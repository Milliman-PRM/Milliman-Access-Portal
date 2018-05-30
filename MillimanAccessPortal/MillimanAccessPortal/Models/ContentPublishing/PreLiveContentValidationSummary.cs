using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Models;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PreLiveContentValidationSummary
    {
        public string RootContentName { get; set; }
        public string ContentTypeName { get; set; }
        public string ContentDescription { get; set; }
        public bool DoesReduce { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string AttestationLanguage { get; set; }
        public string MasterContentLink { get; set; }
        public string UserGuideLink { get; set; }
        public string ReleaseNotesLink { get; set; }
        public string ThumbnailLink { get; set; }
        public ContentReductionHierarchy<ReductionFieldValue> LiveHierarchy { get; set; }
        public ContentReductionHierarchy<ReductionFieldValue> NewHierarchy { get; set; }
        public List<SelectionGroupSummary> SelectionGroups { get; set; }

        public static PreLiveContentValidationSummary Build(ApplicationDbContext Db, long RootContentItemId)
        {
            ContentPublicationRequest PubRequest = Db.ContentPublicationRequest
                                                     .Include(r => r.RootContentItem).ThenInclude(c => c.ContentType)
                                                     .Include(r => r.RootContentItem).ThenInclude(c => c.Client)
                                                     .Where(r => r.RequestStatus == PublicationStatus.Processed)
                                                     .SingleOrDefault(r => r.RootContentItemId == RootContentItemId);
            #region Validation of PubRequest and related nav properties from db
            if (PubRequest == null
             || PubRequest.RootContentItem == null
             || PubRequest.RootContentItem.ContentType == null
             || PubRequest.RootContentItem.Client == null
             )  // if any of this happens it probably means db corruption or connection failed
            {
                throw new ApplicationException($"While building content validation summary, publication request query failed");
            }
            #endregion

            PreLiveContentValidationSummary ReturnObj = new PreLiveContentValidationSummary
            {
                RootContentName = PubRequest.RootContentItem.ContentName,
                ContentTypeName = PubRequest.RootContentItem.ContentType.Name,
                ContentDescription = PubRequest.RootContentItem.Description,
                DoesReduce = PubRequest.RootContentItem.DoesReduce,
                ClientName = PubRequest.RootContentItem.Client.Name,
                ClientCode = PubRequest.RootContentItem.Client.ClientCode,
                AttestationLanguage = null,
                MasterContentLink = null,
                UserGuideLink = null,
                ReleaseNotesLink = null,
                ThumbnailLink = null,
            };

            if (PubRequest.RootContentItem.DoesReduce)
            {
                List<ContentReductionTask> AllTasks = Db.ContentReductionTask
                                                        .Include(t => t.SelectionGroup)
                                                        .Where(t => t.ContentPublicationRequestId == PubRequest.Id)
                                                        .ToList();
                #region Validation of reduction tasks and related nav properties from db
                if (AllTasks.Count == 0
                 || AllTasks.Any(t => t.SelectionGroup == null)
                 || AllTasks.Any(t => t.SelectionGroup.RootContentItemId != PubRequest.RootContentItemId)
                 )  // if any of this happens it probably means db corruption or connection failed
                {
                    throw new ApplicationException($"While building content validation summary, reduction task query failed");
                }
                #endregion

                ReturnObj.LiveHierarchy = ContentReductionHierarchy<ReductionFieldValue>.GetHierarchyForRootContentItem(Db, RootContentItemId);
                ReturnObj.NewHierarchy = ContentReductionHierarchy<ReductionFieldValue>.DeserializeJson(AllTasks[0].MasterContentHierarchy);
                ReturnObj.SelectionGroups = AllTasks.Select(t => new SelectionGroupSummary
                    {
                        Name = t.SelectionGroup.GroupName,
                        IsMaster = t.SelectionGroup.IsMaster,
                        UserCount = Db.UserInSelectionGroup.Count(usg => usg.SelectionGroupId == t.SelectionGroup.Id),
                    }
                ).ToList();
            }
            else
            {
                ReturnObj.LiveHierarchy = null;
                ReturnObj.NewHierarchy = null;
                ReturnObj.SelectionGroups = null;
            }

            // TODO populate the links

            return ReturnObj;
        }
    }

    public class SelectionGroupSummary
    {
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; } = 0;
        public bool IsMaster { get; set; }
    }
}
