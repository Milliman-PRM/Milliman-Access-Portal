/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES: 
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in at least one client.</remarks>
        /// <returns>ViewResult</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            #region Authorization
            AuthorizationResult ContentAccessAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, null));
            if (!ContentAccessAdminResult.Succeeded)
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
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in at least one client.</remarks>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> ClientFamilyList()
        {
            #region Authorization
            AuthorizationResult ContentAccessAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, null));
            if (!ContentAccessAdminResult.Succeeded)
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
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified client.</remarks>
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
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult ContentAccessAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, ClientId));
            if (!ContentAccessAdminResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            ContentAccessAdminRootContentItemListViewModel Model = ContentAccessAdminRootContentItemListViewModel.Build(DbContext, await Queries.GetCurrentApplicationUser(User), Client);

            return Json(Model);
        }

        /// <summary>Returns the selection groups associated with a root content item.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified client.</remarks>
        /// <param name="ClientId">The client associated with the root content item.</param>
        /// <param name="RootContentItemId">The root content item whose selection groups are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> SelectionGroups(long ClientId, long RootContentItemId)
        {
            Client Client = DbContext.Client.Find(ClientId);

            #region Preliminary validation
            if (Client == null)
            {
                Response.Headers.Add("Warning", "The requested client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult ContentAccessAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, ClientId));
            if (!ContentAccessAdminResult.Succeeded)
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
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            ContentAccessAdminSelectionGroupListViewModel Model = ContentAccessAdminSelectionGroupListViewModel.Build(DbContext, Client, RootContentItem);

            return Json(Model);
        }

        /// <summary>Creates a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified client.</remarks>
        /// <param name="ClientId">The client to be assigned to the new selection group.</param>
        /// <param name="RootContentItemId">The root content item to be assigned to the new selection group.</param>
        /// <param name="SelectionGroupName">The name of the new selection group.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSelectionGroup(long ClientId, long RootContentItemId, String SelectionGroupName)
        {
            Client Client = DbContext.Client.Find(ClientId);

            #region Preliminary validation
            if (Client == null)
            {
                Response.Headers.Add("Warning", "The requested client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult ContentAccessAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, ClientId));
            if (!ContentAccessAdminResult.Succeeded)
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
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            ContentItemUserGroup SelectionGroup = new ContentItemUserGroup
            {
                ClientId = Client.Id,
                RootContentItemId = RootContentItem.Id,
                GroupName = SelectionGroupName,
                SelectedHierarchyFieldValueList = new long[] { },
                ContentInstanceUrl = ""
            };

            DbContext.ContentItemUserGroup.Add(SelectionGroup);
            DbContext.SaveChanges();

            ContentAccessAdminSelectionGroupDetailViewModel Model = ContentAccessAdminSelectionGroupDetailViewModel.Build(DbContext, SelectionGroup);

            return Json(Model);
        }

        /// <summary>Updates the users assigned to a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified client.</remarks>
        /// <param name="SelectionGroupId">The selection group to be updated.</param>
        /// <param name="UserAssignments">A dictionary that maps client IDs to a boolean value indicating whether to add or remove the client.</param>
        /// <returns>JsonResult</returns>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSelectionGroupUserAssignments(long SelectionGroupId, Dictionary<long, Boolean> UserAssignments)
        {
            ContentItemUserGroup SelectionGroup = DbContext.ContentItemUserGroup
                .Include(rg => rg.Client)
                .Include(rg => rg.RootContentItem)
                .SingleOrDefault(rg => rg.Id == SelectionGroupId);

            #region Preliminary Validation
            if (SelectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult ContentAccessAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.ClientId));
            if (!ContentAccessAdminResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var Nonexistant = UserAssignments
                .Select(kvp => DbContext.ApplicationUser.Find(kvp.Key))
                .Where(m => m == null);
            if (Nonexistant.Any())
            {
                Response.Headers.Add("Warning", "One or more requested users do not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var Nonpermissioned = UserAssignments
                .Where(kvp => kvp.Value)
                .Where(kvp => DbContext.UserRoleInRootContentItem
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserId == kvp.Key)
                    .Where(ur => ur.RootContentItemId == SelectionGroup.RootContentItemId)
                    .Where(ur => ur.Role.RoleEnum == RoleEnum.ContentUser)
                    .SingleOrDefault() == null
                    );
            if (Nonpermissioned.Any())
            {
                Response.Headers.Add("Warning", "One or more requested users do not have permission to use this content.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var AlreadyInGroup = UserAssignments
                .Where(kvp => kvp.Value)
                .Where(kvp => DbContext.UserInContentItemUserGroup
                    .Include(uug => uug.ContentItemUserGroup)
                    .Where(uug => uug.UserId == kvp.Key)
                    .Where(uug => uug.ContentItemUserGroupId != SelectionGroup.Id)
                    .Where(uug => uug.ContentItemUserGroup.RootContentItemId == SelectionGroup.RootContentItemId
                        )
                    .Any()
                    );
            if (AlreadyInGroup.Any())
            {
                Response.Headers.Add("Warning", "One or more requested users to add are already in a different selection group.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            using (IDbContextTransaction DbTransaction = DbContext.Database.BeginTransaction())
            {
                DbContext.UserInContentItemUserGroup.RemoveRange(
                    UserAssignments
                        .Where(kvp => !kvp.Value)
                        .Select(kvp => DbContext.UserInContentItemUserGroup
                            .Where(uug => uug.ContentItemUserGroupId == SelectionGroup.Id)
                            .Where(uug => uug.UserId == kvp.Key)
                            .SingleOrDefault()
                            )
                        .Where(uug => uug != null)
                    );
                DbContext.SaveChanges();

                DbContext.UserInContentItemUserGroup.AddRange(
                    UserAssignments
                        .Where(kvp => kvp.Value)
                        .Where(kvp => DbContext.UserInContentItemUserGroup
                            .Where(uug => uug.ContentItemUserGroupId == SelectionGroup.Id)
                            .Where(uug => uug.UserId == kvp.Key)
                            .SingleOrDefault() == null
                            )
                        .Select(kvp =>
                            new UserInContentItemUserGroup
                            {
                                ContentItemUserGroupId = SelectionGroup.Id,
                                UserId = kvp.Key,
                            }
                        )
                    );
                DbContext.SaveChanges();

                DbTransaction.Commit();
            }

            ContentAccessAdminSelectionGroupListViewModel Model = ContentAccessAdminSelectionGroupListViewModel.Build(DbContext, SelectionGroup.Client, SelectionGroup.RootContentItem);

            return Json(Model);
        }

        /// <summary>Deletes a selection group.</summary>
        /// <param name="SelectionGroupId">The selection group to be deleted.</param>
        /// <returns>JsonResult</returns>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelectionGroup(long SelectionGroupId)
        {
            ContentItemUserGroup SelectionGroup = DbContext.ContentItemUserGroup.Find(SelectionGroupId);

            #region Preliminary Validation
            if (SelectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult ContentAccessAdminResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.ClientId));
            if (!ContentAccessAdminResult.Succeeded)
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
                        .Where(uug => uug.ContentItemUserGroupId == SelectionGroup.Id)
                        .ToList()
                    );
                DbContext.SaveChanges();

                DbContext.ContentItemUserGroup.Remove(
                    DbContext.ContentItemUserGroup
                        .Where(g => g.Id == SelectionGroup.Id)
                        .Single()
                    );
                DbContext.SaveChanges();

                DbTransaction.Commit();
            }

            ContentAccessAdminSelectionGroupListViewModel Model = ContentAccessAdminSelectionGroupListViewModel.Build(DbContext, SelectionGroup.Client, SelectionGroup.RootContentItem);

            return Json(Model);
        }

        /// <summary>Returns the selections associated with a selection group.</summary>
        /// <param name="SelectionGroupId">The selection group whose selections are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public IActionResult Selections(long SelectionGroupId)
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return Json(new { });
        }

        /// <summary>Updates a selection group with new selections.</summary>
        /// <param name="SelectionGroupId">The selection group whose selections are to be updated.</param>
        /// <param name="Selections">The selections to be applied to the selection group.</param>
        /// <returns>JsonResult</returns>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateSelections(long SelectionGroupId, object Selections)
        {
            #region Authorization
            #endregion

            #region Validation
            #endregion

            return Json(new { });
        }
    }
}
