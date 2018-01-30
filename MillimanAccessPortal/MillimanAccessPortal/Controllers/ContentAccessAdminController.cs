/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Provide all application logic for user administration
 * DEVELOPER NOTES: 
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ContentAccessAdminViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    public class ContentAccessAdminController : Controller
    {
        private readonly IAuthorizationService AuthorizationService;
        private readonly ApplicationDbContext DbContext;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> UserManager;

        public ContentAccessAdminController(
            IAuthorizationService AuthorizationServiceArg,
            ApplicationDbContext DbContextArg,
            StandardQueries QueriesArg,
            UserManager<ApplicationUser> UserManagerArg
            )
        {
            AuthorizationService = AuthorizationServiceArg;
            DbContext = DbContextArg;
            Queries = QueriesArg;
            UserManager = UserManagerArg;
        }

        /// <summary>Action for content access administration index.</summary>
        /// <remarks>This action is only authorized to users with ContentAdmin role in at least one client.</remarks>
        /// <returns>ViewResult</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            #region Authorization
            AuthorizationResult ContentAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAdmin, null));
            if (!ContentAdminResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            return View();
        }

        /// <summary>Returns the list of client families visible to the user.</summary>
        /// <remarks>This action is only authorized to users with ContentAdmin role in at least one client.</remarks>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> ClientFamilyList()
        {
            #region Authorization
            AuthorizationResult ContentAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAdmin, null));
            if (!ContentAdminResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            ContentAccessAdminClientListViewModel Model = await ContentAccessAdminClientListViewModel.Build(await Queries.GetCurrentApplicationUser(User), UserManager, DbContext);

            return Json(Model);
        }

        /// <summary>Returns the root content items available to a client.</summary>
        /// <remarks>This action is only authorized to users with ContentAdmin role in the specified client.</remarks>
        /// <param name="ClientId">The client whose root content items are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> RootContentItems(long ClientId)
        {
            Client Client = DbContext.Client.Find(ClientId);

            #region Preliminary validation
            if (Client == null)
            {
                Response.Headers.Add("Warning", "The requested client does not exist.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            AuthorizationResult ContentAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAdmin, ClientId));
            if (!ContentAdminResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            ContentAccessAdminRootContentItemListViewModel Model = ContentAccessAdminRootContentItemListViewModel.Build(DbContext, Client);

            return Json(Model);
        }

        /// <summary>Returns the report groups associated with a root content item.</summary>
        /// <remarks>This action is only authorized to users with ContentAdmin role in the specified client.</remarks>
        /// <param name="ClientId">The client associated with the root content item.</param>
        /// <param name="RootContentItemId">The root content item whose report groups are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> ReportGroups(long ClientId, long RootContentItemId)
        {
            Client Client = DbContext.Client.Find(ClientId);

            #region Preliminary validation
            if (Client == null)
            {
                Response.Headers.Add("Warning", "The requested client does not exist.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            AuthorizationResult ContentAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAdmin, ClientId));
            if (!ContentAdminResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            RootContentItem RootContentItem = DbContext.RootContentItem.Find(RootContentItemId);
            if (RootContentItem == null)
            {
                Response.Headers.Add("Warning", "The requested root content item does not exist.");
                return BadRequest();
            }
            #endregion

            ContentAccessAdminReportGroupListViewModel Model = ContentAccessAdminReportGroupListViewModel.Build(DbContext, Client, RootContentItem);

            return Json(Model);
        }

        /// <summary>Creates a report group.</summary>
        /// <remarks>This action is only authorized to users with ContentAdmin role in the specified client.</remarks>
        /// <param name="ClientId">The client to be assigned to the new report group.</param>
        /// <param name="RootContentItemId">The root content item to be assigned to the new report group.</param>
        /// <param name="ReportGroupName">The name of the new report group.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReportGroup(long ClientId, long RootContentItemId, String ReportGroupName)
        {
            Client Client = DbContext.Client.Find(ClientId);

            #region Preliminary validation
            if (Client == null)
            {
                Response.Headers.Add("Warning", "The requested client does not exist.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            AuthorizationResult ContentAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAdmin, ClientId));
            if (!ContentAdminResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            RootContentItem RootContentItem = DbContext.RootContentItem.Find(RootContentItemId);
            if (RootContentItem == null)
            {
                Response.Headers.Add("Warning", "The requested root content item does not exist.");
                return BadRequest();
            }
            #endregion

            DbContext.ContentItemUserGroup.Add(new ContentItemUserGroup {
                ClientId = Client.Id,
                RootContentItemId = RootContentItem.Id,
                GroupName = ReportGroupName,
                SelectedHierarchyFieldValueList = new long[] { },
                ContentInstanceUrl = ""
            });
            DbContext.SaveChanges();

            #region Build response object
            ContentItemUserGroup ReportGroup = DbContext.ContentItemUserGroup
                .Where(ug => ug.ClientId == Client.Id)
                .Where(ug => ug.RootContentItemId == RootContentItem.Id)
                .Last();

            ContentAccessAdminReportGroupDetailViewModel Model = ContentAccessAdminReportGroupDetailViewModel.Build(DbContext, ReportGroup);
            #endregion

            return Json(Model);
        }

        /// <summary>Updates the users assigned to a report group.</summary>
        /// <remarks>This action is only authorized to users with ContentAdmin role in the specified client.</remarks>
        /// <param name="ReportGroupId">The report group to be updated.</param>
        /// <param name="MembershipSet">A dictionary indicating a set of users' group membership status.</param>
        /// <returns>JsonResult</returns>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReportGroup(long ReportGroupId, Dictionary<long, Boolean> MembershipSet)
        {
            ContentItemUserGroup ReportGroup = DbContext.ContentItemUserGroup
                .Include(rg => rg.Client)
                .Include(rg => rg.RootContentItem)
                .SingleOrDefault(rg => rg.Id == ReportGroupId);

            #region Preliminary Validation
            if (ReportGroup == null)
            {
                Response.Headers.Add("Warning", "The requested report group does not exist.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            AuthorizationResult ContentAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAdmin, ReportGroup.ClientId));
            if (!ContentAdminResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var Nonexistant = MembershipSet
                .Select(kvp => DbContext.ApplicationUser.Find(kvp.Key))
                .Where(m => m == null);
            if (Nonexistant.Any())
            {
                Response.Headers.Add("Warning", "One or more requested users do not exist.");
                return BadRequest();
            }

            var Nonpermissioned = MembershipSet
                .Where(kvp => DbContext.UserRoleInRootContentItem
                    .Include(ur => ur.Role)
                    .Where(ur => ur.RootContentItemId == ReportGroup.RootContentItemId)
                    .Where(ur => ur.UserId == kvp.Key)
                    .Where(ur => ur.Role.RoleEnum == RoleEnum.ContentUser)
                    .SingleOrDefault() == null
                    );
            if (Nonpermissioned.Any())
            {
                Response.Headers.Add("Warning", "One or more requested users do not have permission to use this content.");
                return BadRequest();
            }

            var AlreadyInGroup = MembershipSet
                .Where(kvp => kvp.Value)
                .Where(kvp => DbContext.UserInContentItemUserGroup
                    .Where(uug => uug.UserId == kvp.Key)
                    .Where(uug => uug.ContentItemUserGroupId != ReportGroup.Id)
                    .Where(uug => DbContext.ContentItemUserGroup
                        .Where(ug => ug.Id == uug.ContentItemUserGroupId)
                        .Single().RootContentItemId == ReportGroup.RootContentItemId
                        )
                    .ToList()
                    .Any()
                    );
            if (AlreadyInGroup.Any())
            {
                Response.Headers.Add("Warning", "One or more requested users to add are already in a different report group.");
                return BadRequest();
            }
            #endregion

            using (IDbContextTransaction DbTransaction = DbContext.Database.BeginTransaction())
            {
                DbContext.UserInContentItemUserGroup.RemoveRange(
                    MembershipSet
                        .Where(kvp => !kvp.Value)
                        .Select(kvp => DbContext.UserInContentItemUserGroup
                            .Where(uug => uug.ContentItemUserGroupId == ReportGroup.Id)
                            .Where(uug => uug.UserId == kvp.Key)
                            .SingleOrDefault()
                            )
                        .Where(uug => uug != null)
                        .ToList()
                    );
                DbContext.SaveChanges();

                DbContext.UserInContentItemUserGroup.AddRange(
                    MembershipSet
                        .Where(kvp => kvp.Value)
                        .Where(kvp => DbContext.UserInContentItemUserGroup.ToList()
                            .Where(uug => uug.ContentItemUserGroupId == ReportGroup.Id)
                            .Where(uug => uug.UserId == kvp.Key)
                            .SingleOrDefault() == null
                            )
                        .Select(kvp =>
                            new UserInContentItemUserGroup
                            {
                                ContentItemUserGroupId = ReportGroup.Id,
                                UserId = kvp.Key,
                            }
                        )
                        .ToList()
                    );
                DbContext.SaveChanges();

                DbTransaction.Commit();
            }

            ContentAccessAdminReportGroupListViewModel Model = ContentAccessAdminReportGroupListViewModel.Build(DbContext, ReportGroup.Client, ReportGroup.RootContentItem);

            return Json(Model);
        }

        /// <summary>Deletes a report group.</summary>
        /// <param name="ReportGroupId">The report group to be deleted.</param>
        /// <returns>JsonResult</returns>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReportGroup(long ReportGroupId)
        {
            ContentItemUserGroup ReportGroup = DbContext.ContentItemUserGroup.Find(ReportGroupId);

            #region Preliminary Validation
            if (ReportGroup == null)
            {
                Response.Headers.Add("Warning", "The requested report group does not exist.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            AuthorizationResult ContentAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAdmin, ReportGroup.ClientId));
            if (!ContentAdminResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            using (IDbContextTransaction DbTransaction = DbContext.Database.BeginTransaction())
            {
                DbContext.UserInContentItemUserGroup.RemoveRange(
                    DbContext.UserInContentItemUserGroup
                        .Where(uug => uug.ContentItemUserGroupId == ReportGroup.Id)
                        .ToList()
                    );
                DbContext.SaveChanges();

                DbContext.ContentItemUserGroup.Remove(
                    DbContext.ContentItemUserGroup
                        .Where(g => g.Id == ReportGroup.Id)
                        .Single()
                    );
                DbContext.SaveChanges();

                DbTransaction.Commit();
            }

            ContentAccessAdminReportGroupListViewModel Model = ContentAccessAdminReportGroupListViewModel.Build(DbContext, ReportGroup.Client, ReportGroup.RootContentItem);

            return Json(Model);
        }

        /// <summary>Returns the selections associated with a report group.</summary>
        /// <param name="ReportGroupId">The report group whose selections are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> Selections(long ReportGroupId)
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return Json(new { });
        }

        /// <summary>Updates a report group with new selections.</summary>
        /// <param name="ReportGroupId">The report group whose selections are to be updated.</param>
        /// <param name="Selections">The selections to be applied to the report group.</param>
        /// <returns>JsonResult</returns>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSelections(long ReportGroupId, object Selections)
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return Json(new { });
        }
    }
}
