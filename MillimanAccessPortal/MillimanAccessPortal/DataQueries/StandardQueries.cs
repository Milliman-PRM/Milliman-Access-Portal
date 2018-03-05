/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: Wrapper for database queries.  Reusable methods appear in this file, methods for single caller appear in files named for the caller
 * DEVELOPER NOTES: 
 */

using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Models;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.ClientAdminViewModels;

namespace MillimanAccessPortal.DataQueries
{
    public partial class StandardQueries
    {
        private ApplicationDbContext DbContext = null;
        private UserManager<ApplicationUser> UserManager = null;

        /// <summary>
        /// Constructor, stores local copy of the caller's IServiceScope
        /// </summary>
        /// <param name="SvcProvider"></param>
        public StandardQueries(
            ApplicationDbContext ContextArg,
            UserManager<ApplicationUser> UserManagerArg
            )
        {
            DbContext = ContextArg;
            UserManager = UserManagerArg;
        }

        /// <summary>
        /// Returns a list of the Clients to which the user is assigned Admin role
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public List<Client> GetListOfClientsUserIsAuthorizedToManage(string UserName)
        {
            List<Client> ListOfAuthorizedClients = new List<Client>();
            IQueryable<Client> AuthorizedClientsQuery =
                DbContext.UserRoleInClient
                .Where(urc => urc.Role.RoleEnum == RoleEnum.Admin)
                .Where(urc => urc.User.UserName == UserName)
                .Join(DbContext.Client, urc => urc.ClientId, c => c.Id, (urc, c) => c);

            ListOfAuthorizedClients.AddRange(AuthorizedClientsQuery);  // Query executes here

            return ListOfAuthorizedClients;
        }

        public List<Client> GetAllRelatedClients(Client ClientArg)
        {
            List<Client> ReturnList = new List<Client>();

            Client RootClient = GetRootClientOfClient(ClientArg.Id);
            ReturnList.Add(RootClient);
            ReturnList.AddRange(GetChildClients(RootClient, true));

            return ReturnList;
        }

        private List<Client> GetChildClients(Client ClientArg, bool Recurse = false)
        {
            List<Client> ReturnList = new List<Client>();

            List<Client> FoundChildClients = DbContext.Client.Where(c => c.ParentClientId == ClientArg.Id).ToList();

            ReturnList.AddRange(FoundChildClients);
            if (Recurse)
            {
                // Get grandchildren too
                foreach (Client ChildClient in FoundChildClients)
                {
                    ReturnList.AddRange(GetChildClients(ChildClient, Recurse));
                }
            }

            return ReturnList;
        }

        public Client GetRootClientOfClient(long id)
        {
            // Execute query here so there is only one db query and the rest is done locally in memory
            List<Client> AllClients = DbContext.Client.ToList();

            // start with the client id supplied
            Client CandidateResult = AllClients.SingleOrDefault(c => c.Id == id);

            // search up the parent hierarchy
            while (CandidateResult != null && CandidateResult.ParentClientId != null)
            {
                CandidateResult = AllClients.SingleOrDefault(c => c.Id == CandidateResult.ParentClientId);
            }

            return CandidateResult;
        }

        public List<Client> GetAllRootClients()
        {
            return DbContext.Client.Where(c => c.ParentClientId == null).ToList();
        }

        /// <summary>
        /// Returns list of normalized role names authorized to provided Client for provided UserId
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="ClientId"></param>
        /// <returns></returns>
        public List<AssignedRoleInfo> GetUserRolesForClient(long UserId, long ClientId)
        {
            IQueryable<AssignedRoleInfo> Query = DbContext.UserRoleInClient
                                                            .Include(urc => urc.Role)
                                                            .Where(urc => urc.UserId == UserId
                                                                       && urc.ClientId == ClientId)
                                                            .Distinct()
                                                            .Select(urc => 
                                                                new AssignedRoleInfo
                                                                {
                                                                    RoleEnum = urc.Role.RoleEnum,
                                                                    RoleDisplayValue = ApplicationRole.RoleDisplayNames[urc.Role.RoleEnum],
                                                                    IsAssigned = true,
                                                                });

            List<AssignedRoleInfo> ReturnVal = Query.ToList();

            return ReturnVal;
        }

        internal async Task<ApplicationUser> GetCurrentApplicationUser(ClaimsPrincipal User)
        {
            return await UserManager.GetUserAsync(User);
        }

        public ContentReductionHierarchy GetReductionFieldsForRootContent(long ContentId)
        {
            RootContentItem ContentItem = DbContext.RootContentItem
                                                     .Include(rc => rc.ContentType)
                                                     .SingleOrDefault(rc => rc.Id == ContentId);
            if (ContentItem == null)
            {
                return null;
            }

            try
            {
                ContentReductionHierarchy ReturnObject = new ContentReductionHierarchy { RootContentItemId = ContentId };

                foreach (HierarchyField Field in DbContext.HierarchyField
                                                            .Where(hf => hf.RootContentItemId == ContentId)
                                                            .ToList())
                {
                    // There may be different handling required for some future content type. If so, move 
                    // the characteristics specific to Qlikview into a class derived from ReductionFieldBase
                    switch (ContentItem.ContentType.TypeEnum)
                    {
                        case ContentTypeEnum.Qlikview:
                            ReturnObject.Fields.Add(new ReductionField
                            {
                                FieldName = Field.FieldName,
                                DisplayName = Field.FieldDisplayName,
                                ValueDelimiter = Field.FieldDelimiter,
                                StructureType = Field.StructureType,
                                Values = DbContext.HierarchyFieldValue
                                                         .Where(fv => fv.HierarchyFieldId == Field.Id)
                                                         .Select(fv => new ReductionFieldValue { Value = fv.Value })
                                                         .ToArray(),
                            });
                            break;

                        default:
                            break;
                    }
                }

                return ReturnObject;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Build hierarchy of selections for a selection group</summary>
        /// <remarks>This builds a nested data structure to represent a hierarchy from several flat tables.</remarks>
        /// <param name="SelectionGroupId">The selection group whose selections are to be gathered</param>
        /// <param name="SelectionUpdates">Any changes to the current selections to effect in the returned hierarchy</param>
        /// <returns>ContentReductionHierarchy</returns>
        public ContentReductionHierarchy GetFieldSelectionsForSelectionGroup(long SelectionGroupId, Dictionary<long, bool> SelectionUpdates = null)
        {
            SelectionGroup SelectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rci => rci.ContentType)
                .SingleOrDefault(sg => sg.Id == SelectionGroupId);
            if (SelectionGroup == null)
            {
                return null;
            }

            // Apply selection updates if provided
            var SelectedHierarchyFieldValues = (SelectionUpdates == null)
                ? SelectionGroup.SelectedHierarchyFieldValueList
                : SelectionGroup.SelectedHierarchyFieldValueList
                    .Union(SelectionUpdates.Keys.Where(k => SelectionUpdates[k]))
                    .Except(SelectionUpdates.Keys.Where(k => !SelectionUpdates[k]));

            ContentReductionHierarchy ContentReductionHierarchy = new ContentReductionHierarchy
            {
                RootContentItemId = SelectionGroup.RootContentItemId,
            };

            var RelatedHierarchyFields = DbContext.HierarchyField
                .Where(hf => hf.RootContentItemId == SelectionGroup.RootContentItemId);
            foreach (var HierarchyField in RelatedHierarchyFields)
            {
                var HierarchyFieldValues = DbContext.HierarchyFieldValue
                    .Where(hfv => hfv.HierarchyFieldId == HierarchyField.Id)
                    .Select(hfv => new ReductionFieldValueSelection
                    {
                        Value = hfv.Value,
                        SelectionStatus = SelectedHierarchyFieldValues.Contains(hfv.Id),
                    })
                    .ToArray();

                // TODO: Create ReductionFieldBase and extend for each content type
                ReductionField ReductionField;
                switch (SelectionGroup.RootContentItem.ContentType.TypeEnum)
                {
                    case ContentTypeEnum.Qlikview:
                        ReductionField = new ReductionField
                        {
                            FieldName = HierarchyField.FieldName,
                            DisplayName = HierarchyField.FieldDisplayName,
                            ValueDelimiter = HierarchyField.FieldDelimiter,
                            StructureType = HierarchyField.StructureType,
                            Values = HierarchyFieldValues,
                        };
                        break;
                    default:
                        ReductionField = null;
                        break;
                }

                ContentReductionHierarchy.Fields.Add(ReductionField);
            }

            return ContentReductionHierarchy;
        }
    }
}
