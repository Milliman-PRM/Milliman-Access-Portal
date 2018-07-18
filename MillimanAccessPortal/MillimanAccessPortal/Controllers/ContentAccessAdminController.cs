/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using AuditLogLib;
using AuditLogLib.Services;
using MapCommonLib;
using MapCommonLib.ActionFilters;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using MillimanAccessPortal.Models.ContentPublishing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    public class ContentAccessAdminController : Controller
    {
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ApplicationDbContext DbContext;
        private readonly ILogger Logger;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> UserManager;

        public ContentAccessAdminController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ApplicationDbContext DbContextArg,
            ILoggerFactory LoggerFactoryArg,
            StandardQueries QueriesArg,
            UserManager<ApplicationUser> UserManagerArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DbContext = DbContextArg;
            Logger = LoggerFactoryArg.CreateLogger<ContentAccessAdminController>();
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
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, null));
            if (!RoleInClientResult.Succeeded)
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
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, null));
            if (!RoleInClientResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            ClientTree model = await ClientTree.Build(await Queries.GetCurrentApplicationUser(User), UserManager, DbContext, RoleEnum.ContentAccessAdmin);

            return Json(model);
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
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, ClientId));
            if (!RoleInClientResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            var model = RootContentItemList.Build(DbContext, Client, await Queries.GetCurrentApplicationUser(User), RoleEnum.ContentAccessAdmin);

            return Json(model);
        }

        /// <summary>Returns the selection groups associated with a root content item.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified root content item.</remarks>
        /// <param name="RootContentItemId">The root content item whose selection groups are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> SelectionGroups(long RootContentItemId)
        {
            RootContentItem RootContentItem = DbContext.RootContentItem.Find(RootContentItemId);

            #region Preliminary validation
            if (RootContentItem == null)
            {
                Response.Headers.Add("Warning", "The requested root content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            SelectionGroupList Model = SelectionGroupList.Build(DbContext, RootContentItem);

            return Json(Model);
        }

        /// <summary>Creates a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified root content item.</remarks>
        /// <param name="RootContentItemId">The root content item to be assigned to the new selection group.</param>
        /// <param name="SelectionGroupName">The name of the new selection group.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSelectionGroup(long RootContentItemId, String SelectionGroupName)
        {
            RootContentItem RootContentItem = DbContext.RootContentItem.Find(RootContentItemId);

            #region Preliminary validation
            if (RootContentItem == null)
            {
                Response.Headers.Add("Warning", "The requested root content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to create selection group without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { RootContentItem.ClientId, RootContentItemId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            SelectionGroup SelectionGroup = new SelectionGroup
            {
                RootContentItemId = RootContentItem.Id,
                GroupName = SelectionGroupName,
                SelectedHierarchyFieldValueList = new long[] { },
                ContentInstanceUrl = ""
            };

            try
            {
                DbContext.SelectionGroup.Add(SelectionGroup);
                DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Exception while creating selection group \"{SelectionGroup.Id}\"");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", $"Failed to complete transaction.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            #region Log audit event
            AuditEvent SelectionGroupCreatedEvent = AuditEvent.New(
                $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                "Selection group created",
                AuditEventId.SelectionGroupCreated,
                new { RootContentItem.ClientId, RootContentItemId, SelectionGroupId = SelectionGroup.Id },
                User.Identity.Name,
                HttpContext.Session.Id
                );
            AuditLogger.Log(SelectionGroupCreatedEvent);
            #endregion

            Models.ContentAccessAdmin.SelectionGroupSummary Model = Models.ContentAccessAdmin.SelectionGroupSummary.Build(DbContext, SelectionGroup);

            return Json(Model);
        }

        /// <summary>
        /// Rename a selection group
        /// </summary>
        /// <param name="selectionGroupId">The selection group to be updated.</param>
        /// <param name="name">The new name for the selection group.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameSelectionGroup(long selectionGroupId, string name)
        {
            var selectionGroup = DbContext.SelectionGroup
                .Where(sg => sg.Id == selectionGroupId)
                .SingleOrDefault();

            #region Preliminary validation
            if (selectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, selectionGroup.RootContentItemId));
            if (!roleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to update selection group without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { selectionGroup.RootContentItem.ClientId, selectionGroup.RootContentItemId, selectionGroupId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            selectionGroup.GroupName = name;
            DbContext.SelectionGroup.Update(selectionGroup);
            DbContext.SaveChanges();

            var model = Models.ContentAccessAdmin.SelectionGroupSummary.Build(DbContext, selectionGroup);

            return Json(model);
        }

        /// <summary>
        /// Adds a single user by email to a selection group.
        /// </summary>
        /// <remarks>This is a temporary helper action to be used until content access admin is rewritten in React.</remarks>
        /// <param name="SelectionGroupId">The selection group to be updated.</param>
        /// <param name="email">The email of the user to add.</param>
        /// <returns>Ok, or UnprocessableEntity if a user with the provided email does not exist.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserToSelectionGroup(long SelectionGroupId, string email)
        {
            SelectionGroup selectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rci => rci.Client)
                .SingleOrDefault(sg => sg.Id == SelectionGroupId);

            #region Preliminary Validation
            if (selectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, selectionGroup.RootContentItemId));
            if (!roleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to update selection group without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { selectionGroup.RootContentItem.ClientId, selectionGroup.RootContentItemId, SelectionGroupId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            var user = DbContext.ApplicationUser
                .Where(u => u.Email == email)
                .SingleOrDefault();

            #region Validation
            if (user == null)
            {
                Response.Headers.Add("Warning", "One or more requested users do not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            return await UpdateSelectionGroupUserAssignments(SelectionGroupId, new Dictionary<long, bool> { { user.Id, true } });
        }

        /// <summary>Updates the users assigned to a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group to be updated.</param>
        /// <param name="UserAssignments">A dictionary that maps client IDs to a boolean value indicating whether to add or remove the client.</param>
        /// <returns>JsonResult</returns>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSelectionGroupUserAssignments(long SelectionGroupId, Dictionary<long, Boolean> UserAssignments)
        {
            SelectionGroup SelectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rci => rci.Client)
                .SingleOrDefault(sg => sg.Id == SelectionGroupId);

            #region Preliminary Validation
            if (SelectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to update selection group without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Argument processing
            var CurrentAssignments = DbContext.UserInSelectionGroup
                .Where(usg => usg.SelectionGroupId == SelectionGroup.Id)
                .Select(usg => usg.UserId)
                .ToList();
            var UserAdditions = UserAssignments
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .Except(CurrentAssignments);
            var UserRemovals = UserAssignments
                .Where(kvp => !kvp.Value)
                .Select(kvp => kvp.Key)
                .Intersect(CurrentAssignments);
            #endregion

            #region Validation
            var Existent = DbContext.ApplicationUser
                .Where(u => UserAssignments.Keys.Contains(u.Id));
            if (Existent.Count() < UserAssignments.Count())
            {
                Response.Headers.Add("Warning", "One or more requested users do not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var Permissioned = DbContext.UserRoleInRootContentItem
                .Where(ur => UserAdditions.Contains(ur.UserId))
                .Where(ur => ur.RootContentItemId == SelectionGroup.RootContentItemId)
                .Where(ur => ur.RoleId == ((long) RoleEnum.ContentUser));
            if (Permissioned.Count() < UserAdditions.Count())
            {
                Response.Headers.Add("Warning", "One or more requested users do not have permission to use this content.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var AlreadyInGroup = DbContext.UserInSelectionGroup
                .Where(usg => UserAdditions.Contains(usg.UserId))
                .Where(usg => usg.SelectionGroupId != SelectionGroup.Id)
                .Where(usg => usg.SelectionGroup.RootContentItemId == SelectionGroup.RootContentItemId);
            if (AlreadyInGroup.Any())
            {
                Response.Headers.Add("Warning", "One or more requested users to add are already in a different selection group.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            try
            {
                using (IDbContextTransaction DbTransaction = DbContext.Database.BeginTransaction())
                {
                    DbContext.UserInSelectionGroup.RemoveRange(
                        DbContext.UserInSelectionGroup
                            .Where(usg => UserRemovals.Contains(usg.UserId))
                            .Where(usg => usg.SelectionGroupId == SelectionGroup.Id)
                        );
                    DbContext.SaveChanges();

                    DbContext.UserInSelectionGroup.AddRange(
                        UserAdditions
                            .Select(uid =>
                                new UserInSelectionGroup
                                {
                                    SelectionGroupId = SelectionGroup.Id,
                                    UserId = uid,
                                }
                            )
                        );
                    DbContext.SaveChanges();

                    DbTransaction.Commit();
                }
            }
            catch (Exception ex)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Exception while updating selection group \"{SelectionGroupId}\" user assignments");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", $"Failed to complete transaction.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            #region Log audit event(s)
            foreach (var UserAddition in UserAdditions)
            {
                AuditEvent SelectionGroupUserAssignmentsUpdatedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    "User assigned to selection group",
                    AuditEventId.SelectionGroupUserAssigned,
                    new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId, UserId = UserAddition },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(SelectionGroupUserAssignmentsUpdatedEvent);
            }
            foreach (var UserRemoval in UserRemovals)
            {
                AuditEvent SelectionGroupUserAssignmentsUpdatedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    "User removed from selection group",
                    AuditEventId.SelectionGroupUserRemoved,
                    new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId, UserId = UserRemoval },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(SelectionGroupUserAssignmentsUpdatedEvent);
            }
            #endregion

            var Model = Models.ContentAccessAdmin.SelectionGroupSummary.Build(DbContext, SelectionGroup);

            return Json(Model);
        }

        /// <summary>
        /// Set suspended status for a selection group
        /// </summary>
        /// <param name="selectionGroupId">The selection group to be updated.</param>
        /// <param name="isSuspended">The suspended state to which the selection group is to be set.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetSuspendedSelectionGroup(long selectionGroupId, bool isSuspended)
        {
            SelectionGroup selectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                .SingleOrDefault(sg => sg.Id == selectionGroupId);

            #region Preliminary Validation
            if (selectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, selectionGroup.RootContentItemId));
            if (!roleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent authorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to delete selection group without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { selectionGroup.RootContentItem.ClientId, selectionGroup.RootContentItemId, selectionGroupId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(authorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            selectionGroup.IsSuspended = isSuspended;
            DbContext.SelectionGroup.Update(selectionGroup);
            DbContext.SaveChanges();

            #region Log audit event
            AuditEvent selectionGroupSuspensionEvent = AuditEvent.New(
                $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                "Selection group suspension status updated",
                AuditEventId.SelectionGroupSuspensionUpdate,
                new { selectionGroup.RootContentItem.ClientId, selectionGroup.RootContentItemId, selectionGroupId, isSuspended },
                User.Identity.Name,
                HttpContext.Session.Id
                );
            AuditLogger.Log(selectionGroupSuspensionEvent);
            #endregion

            var model = SelectionsDetail.Build(DbContext, Queries, selectionGroup);

            return Json(model);
        }

        /// <summary>Deletes a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group to be deleted.</param>
        /// <returns>JsonResult</returns>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelectionGroup(long SelectionGroupId)
        {
            SelectionGroup SelectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                .SingleOrDefault(sg => sg.Id == SelectionGroupId);

            #region Preliminary Validation
            if (SelectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to delete selection group without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            List<long> RemovedUsers = new List<long>();

            try
            {
                using (IDbContextTransaction DbTransaction = DbContext.Database.BeginTransaction())
                {
                    List<UserInSelectionGroup> UsersToRemove = DbContext.UserInSelectionGroup
                        .Where(usg => usg.SelectionGroupId == SelectionGroup.Id)
                        .ToList();
                    DbContext.UserInSelectionGroup.RemoveRange(UsersToRemove);
                    DbContext.SaveChanges();

                    DbContext.SelectionGroup.Remove(
                        DbContext.SelectionGroup
                            .Where(sg => sg.Id == SelectionGroup.Id)
                            .Single()
                        );
                    DbContext.SaveChanges();

                    DbTransaction.Commit();

                    RemovedUsers = UsersToRemove
                        .Select(uug => uug.UserId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Exception while deleting selection group \"{SelectionGroupId}\" or removing members: [{string.Join(",", RemovedUsers)}]");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", $"Failed to complete transaction.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            #region Log audit event(s)
            foreach (var UserId in RemovedUsers)
            {
                AuditEvent SelectionGroupUpdatedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    "User removed from selection group",
                    AuditEventId.SelectionGroupUserRemoved,
                    new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId, UserId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(SelectionGroupUpdatedEvent);
            }

            AuditEvent SelectionGroupDeletedEvent = AuditEvent.New(
                $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                "Selection group deleted",
                AuditEventId.SelectionGroupDeleted,
                new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId },
                User.Identity.Name,
                HttpContext.Session.Id
                );
            AuditLogger.Log(SelectionGroupDeletedEvent);
            #endregion

            SelectionGroupList Model = SelectionGroupList.Build(DbContext, SelectionGroup.RootContentItem);

            return Json(Model);
        }

        /// <summary>Returns the selections associated with a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the related root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group whose selections are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> Selections(long SelectionGroupId)
        {
            SelectionGroup SelectionGroup = DbContext.SelectionGroup.Find(SelectionGroupId);

            #region Preliminary validation
            if (SelectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            SelectionsDetail Model = SelectionsDetail.Build(DbContext, Queries, SelectionGroup);

            return Json(Model);
        }

        /// <summary>Submits a new reduction task.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the related root content item.</remarks>
        /// <param name="selectionGroupId">The selection group to reduce.</param>
        /// <param name="selections">A list of selected selection IDs</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSelections(long selectionGroupId, bool isMaster, long[] selections)
        {
            var selectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                .Where(sg => sg.Id == selectionGroupId)
                .SingleOrDefault();

            #region Preliminary validation
            if (selectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, selectionGroup.RootContentItemId));
            if (!roleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to update selections without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { selectionGroup.RootContentItem.ClientId, selectionGroup.RootContentItemId, selectionGroupId, selections },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var currentLivePublication = DbContext.ContentPublicationRequest
                .Where(request => request.RootContentItemId == selectionGroup.RootContentItemId)
                .Where(request => request.RequestStatus == PublicationStatus.Confirmed)
                .SingleOrDefault();
            if (currentLivePublication == null)
            {
                Response.Headers.Add("Warning", "There is no live content for the requested selection group.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // There must be no pending reduction task for this selection group
            var pendingStatus = new List<ReductionStatusEnum>
            {
                ReductionStatusEnum.Queued,
                ReductionStatusEnum.Reducing,
                ReductionStatusEnum.Reduced,
            };
            if (DbContext.ContentReductionTask
                .Where(task => task.SelectionGroupId == selectionGroup.Id)
                .Where(task => task.CreateDateTimeUtc > currentLivePublication.CreateDateTimeUtc)
                .Any(task => pendingStatus.Contains(task.ReductionStatus)))
            {
                Response.Headers.Add("Warning", "An unresolved publication or selection change prevents this action.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // There must be no reduction task with erroneous or unexpected status for this selection group
            var unexpectedStatus = new List<ReductionStatusEnum>
            {
                ReductionStatusEnum.Unspecified,
                ReductionStatusEnum.Replaced,
            };
            if (DbContext.ContentReductionTask
                .Where(task => task.SelectionGroupId == selectionGroup.Id)
                .Where(task => task.CreateDateTimeUtc > currentLivePublication.CreateDateTimeUtc)
                .Any(task => unexpectedStatus.Contains(task.ReductionStatus)))
            {
                Response.Headers.Add("Warning", "An erroneous reduction status prevents this action.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            if (isMaster)
            {
                if (selectionGroup.IsMaster)
                {
                    Response.Headers.Add("Warning", "The specified selection group already has master content access.");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }
            else
            {
                // The requested selections must be valid for the content item
                int validSelectionCount = DbContext.HierarchyFieldValue
                                                   .Where(hfv => hfv.HierarchyField.RootContentItemId == selectionGroup.RootContentItemId)
                                                   .Where(hfv => selections.Contains(hfv.Id))
                                                   .Count();
                if (validSelectionCount < selections.Count())
                {
                    Response.Headers.Add("Warning", "One or more requested selections do not exist or do not belong to the specified content item.");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }

                if (!selectionGroup.IsMaster)
                {
                    // The requested selections must be modified from the live selections for this SelectionGroup
                    if (selections.ToHashSet().SetEquals(selectionGroup.SelectedHierarchyFieldValueList))
                    {
                        Response.Headers.Add("Warning", "The requested selections are not different from the active document.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                }
            }
            #endregion

            if (isMaster)
            {
                selectionGroup.IsMaster = true;
                selectionGroup.SelectedHierarchyFieldValueList = new long[0];
                DbContext.SelectionGroup.Update(selectionGroup);
                DbContext.SaveChanges();

                #region Log audit event
                AuditEvent selectionGroupMasterAccessEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    "Selection group given master access",
                    AuditEventId.SelectionChangeMasterAccessGranted,
                    new { selectionGroup.RootContentItem.ClientId, selectionGroup.RootContentItemId, selectionGroupId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(selectionGroupMasterAccessEvent);
                #endregion
            }
            else
            {
                string selectionCriteriaString = JsonConvert.SerializeObject(ContentReductionHierarchy<ReductionFieldValueSelection>
                    .GetFieldSelectionsForSelectionGroup(DbContext, selectionGroupId, selections), Formatting.Indented);

                var contentReductionTask = new ContentReductionTask
                {
                    ApplicationUser = await Queries.GetCurrentApplicationUser(User),
                    SelectionGroupId = selectionGroup.Id,
                    MasterFilePath = @"\\indy-syn01\prm_test\Sample Data\CCR_0273ZDM_New_Reduction_Script.qvw",  // TODO Fix this
                    ContentPublicationRequest = null,
                    SelectionCriteria = selectionCriteriaString,
                    ReductionStatus = ReductionStatusEnum.Queued,
                    CreateDateTimeUtc = DateTime.UtcNow,
                };
                DbContext.ContentReductionTask.Add(contentReductionTask);
                DbContext.SaveChanges();

                #region Log audit event
                AuditEvent selectionChangeReductionQueuedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    "Selection change reduction task queued",
                    AuditEventId.SelectionChangeReductionQueued,
                    new { selectionGroup.RootContentItem.ClientId, selectionGroup.RootContentItemId, selectionGroupId, selections },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(selectionChangeReductionQueuedEvent);
                #endregion
            }

            SelectionsDetail model = SelectionsDetail.Build(DbContext, Queries, selectionGroup);

            return Json(model);
        }

        /// <summary>Cancel a pending or completed reduction task.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the related root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group associated with the reduction to be canceled.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReduction(long SelectionGroupId)
        {
            SelectionGroup SelectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                .Where(sg => sg.Id == SelectionGroupId)
                .SingleOrDefault();

            #region Preliminary validation
            if (SelectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to cancel reduction without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentAccessAdmin]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var CancelableStatus = new List<ReductionStatusEnum>
            {
                ReductionStatusEnum.Queued,
            };
            var CancelableTasks = DbContext.ContentReductionTask
                .Where(crt => crt.SelectionGroupId == SelectionGroup.Id)
                .Where(crt => CancelableStatus.Contains(crt.ReductionStatus))
                .Where(crt => crt.ContentPublicationRequestId == null);
            if (CancelableTasks.Count() == 0)
            {
                Response.Headers.Add("Warning", "There are no cancelable tasks for this root content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            // There should always be at most one cancelable task
            var UpdatedTasks = new List<ContentReductionTask>();
            foreach (var Task in CancelableTasks)
            {
                Task.ReductionStatus = ReductionStatusEnum.Canceled;
                DbContext.Update(Task);

                UpdatedTasks.Add(Task);
            }
            DbContext.SaveChanges();

            #region Log audit event
            AuditEvent SelectionChangeReductionCanceledEvent = AuditEvent.New(
                $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                "Selection change reduction task canceled",
                AuditEventId.SelectionChangeReductionCanceled,
                new { SelectionGroup.RootContentItem.ClientId, SelectionGroup.RootContentItemId, SelectionGroupId, UpdatedTasks },
                User.Identity.Name,
                HttpContext.Session.Id
                );
            AuditLogger.Log(SelectionChangeReductionCanceledEvent);
            #endregion

            SelectionsDetail Model = SelectionsDetail.Build(DbContext, Queries, SelectionGroup);

            return Json(Model);
        }

        [HttpGet]
        [PreventAuthRefresh]
        public async Task<IActionResult> Status()
        {
            var rootContentItemStatusList = RootContentItemStatus.Build(DbContext, await Queries.GetCurrentApplicationUser(User));
            var selectionGroupStatusList = SelectionGroupStatus.Build(DbContext, await Queries.GetCurrentApplicationUser(User));

            var model = new
            {
                RootContentItemStatusList = rootContentItemStatusList,
                SelectionGroupStatusList = selectionGroupStatusList,
            };

            return new JsonResult(model);
        }
    }
}
