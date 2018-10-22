/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using AuditLogLib.Event;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using MillimanAccessPortal.Models.ContentPublishing;
using QlikviewLib;
using Serilog;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    public class ContentAccessAdminController : Controller
    {
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly IConfiguration ApplicationConfig;
        private readonly ApplicationDbContext DbContext;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly QlikviewConfig QvConfig;

        public ContentAccessAdminController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ApplicationDbContext DbContextArg,
            StandardQueries QueriesArg,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration ApplicationConfigArg,
            IOptions<QlikviewConfig> QvConfigArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DbContext = DbContextArg;
            Queries = QueriesArg;
            UserManager = UserManagerArg;
            ApplicationConfig = ApplicationConfigArg;
            QvConfig = QvConfigArg.Value;
        }

        /// <summary>Action for content access administration index.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in at least one client.</remarks>
        /// <returns>ViewResult</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            Log.Verbose($"Entered ContentAccessAdminController.Index");

            #region Authorization
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, null));
            if (!RoleInClientResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.Index action: authorization failure for user {User.Identity.Name}, role {RoleEnum.ContentAccessAdmin.ToString()}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access.");
                return Unauthorized();
            }
            #endregion

            return View();
        }

        /// <summary>Returns the list of client families visible to the user.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in at least one client.</remarks>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> ClientFamilyList()
        {
            Log.Verbose($"Entered ContentAccessAdminController.ClientFamilyList action");

            #region Authorization
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, null));
            if (!RoleInClientResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.ClientFamilyList action: authorization failure for user {User.Identity.Name}, role {RoleEnum.ContentAccessAdmin.ToString()}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access.");
                return Unauthorized();
            }
            #endregion

            ClientTree model = await ClientTree.Build(await Queries.GetCurrentApplicationUser(User), UserManager, DbContext, RoleEnum.ContentAccessAdmin);

            return Json(model);
        }

        /// <summary>Returns the root content items available to a client.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified client.</remarks>
        /// <param name="ClientId">The client whose root content items are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> RootContentItems(Guid ClientId)
        {
            Log.Verbose($"Entered ContentAccessAdminController.RootContentItems action");

            Client Client = DbContext.Client.Find(ClientId);

            #region Preliminary validation
            if (Client == null)
            {
                Log.Debug($"In ContentAccessAdminController.RootContentItems action: client with ID {ClientId} not found");
                Response.Headers.Add("Warning", "The requested client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, ClientId));
            if (!RoleInClientResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.RootContentItems action: authorization failure, user {User.Identity.Name}, role {RoleEnum.ContentAccessAdmin.ToString()}, client ID {ClientId}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified client.");
                return Unauthorized();
            }
            #endregion

            var model = Models.ContentAccessAdmin.RootContentItemList.Build(DbContext, Client, await Queries.GetCurrentApplicationUser(User), RoleEnum.ContentAccessAdmin);

            return Json(model);
        }

        /// <summary>Returns the selection groups associated with a root content item.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified root content item.</remarks>
        /// <param name="RootContentItemId">The root content item whose selection groups are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> SelectionGroups(Guid RootContentItemId)
        {
            Log.Verbose($"Entered ContentAccessAdminController.SelectionGroups action: with RootContentItem ID {RootContentItemId}");

            RootContentItem RootContentItem = DbContext.RootContentItem.Find(RootContentItemId);

            #region Preliminary validation
            if (RootContentItem == null)
            {
                Log.Debug($"In ContentAccessAdminController.SelectionGroups action: RootContentItem with ID {RootContentItemId} not found");
                Response.Headers.Add("Warning", "The requested content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.SelectionGroups action: authorization failure, user {User.Identity.Name}, RootContentItemId {RootContentItemId}, role {RoleEnum.ContentAccessAdmin.ToString()}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
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
        public async Task<IActionResult> CreateSelectionGroup(Guid RootContentItemId, String SelectionGroupName)
        {
            Log.Verbose($"Entered ContentAccessAdminController.CreateSelectionGroup action: with RootContentItem ID {RootContentItemId}, SelectionGroupName {SelectionGroupName}");

            RootContentItem rootContentItem = DbContext.RootContentItem
                .Where(item => item.Id == RootContentItemId)
                .Include(item => item.Client)
                .Include(item => item.ContentType)
                .SingleOrDefault();

            #region Preliminary validation
            if (rootContentItem == null)
            {
                Log.Debug($"In ContentAccessAdminController.CreateSelectionGroup action: RootContentItem with ID {RootContentItemId} not found");
                Response.Headers.Add("Warning", "The requested content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.CreateSelectionGroup action: authorization failure, user {User.Identity.Name}, RootContentItem ID {RootContentItemId}, role {RoleEnum.ContentAccessAdmin.ToString()}");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentAccessAdmin));
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            // reject this request if the RootContentItem has a pending publication request
            bool blockedByPendingPublication = DbContext.ContentPublicationRequest
                .Where(pr => pr.RootContentItemId == rootContentItem.Id)
                .Any(pr => pr.RequestStatus.IsActive());
            if (blockedByPendingPublication)
            {
                Log.Debug($"In ContentAccessAdminController.CreateSelectionGroup action: managing of selection groups for content item {rootContentItem.Id} is blocked due to pending publication, aborting");
                Response.Headers.Add("Warning", "A new selection group may not be created while this content item has a pending publication.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            SelectionGroup newSelectionGroup = new SelectionGroup
            {
                RootContentItemId = rootContentItem.Id,
                GroupName = SelectionGroupName,
                SelectedHierarchyFieldValueList = new Guid[] { },
                ContentInstanceUrl = "",
                IsMaster = false,
            };

            if (!rootContentItem.DoesReduce)
            {
                ContentRelatedFile liveMasterFile = rootContentItem.ContentFilesList.SingleOrDefault(f => f.FilePurpose.ToLower() == "mastercontent");
                if (liveMasterFile == null || !System.IO.File.Exists(liveMasterFile.FullPath))
                {
                    Log.Debug($"In ContentAccessAdminController.CreateSelectionGroup action; failed to locate master content file {liveMasterFile?.FullPath ?? ""}");
                    Response.Headers.Add("Warning", "A master content file does not exist for the requested content item.");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }

                newSelectionGroup.IsMaster = true;
                newSelectionGroup.RootContentItem = rootContentItem;
                newSelectionGroup.SetContentUrl(Path.GetFileName(liveMasterFile.FullPath));
            }

            try
            {
                DbContext.SelectionGroup.Add(newSelectionGroup);
                DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Exception while creating selection group \"{newSelectionGroup.Id}\"");
                Log.Error(ErrMsg);
                Response.Headers.Add("Warning", $"Failed to complete transaction.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            Log.Verbose($"In ContentAccessAdminController.CreateSelectionGroup action; success");
            AuditLogger.Log(AuditEventType.SelectionGroupCreated.ToEvent(newSelectionGroup));

            Models.ContentAccessAdmin.SelectionGroupSummary Model = Models.ContentAccessAdmin.SelectionGroupSummary.Build(DbContext, newSelectionGroup);

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
        public async Task<IActionResult> RenameSelectionGroup(Guid selectionGroupId, string name)
        {
            Log.Verbose($"Entered ContentAccessAdminController.RenameSelectionGroup action: with SelectionGroup ID {selectionGroupId}, new name {name}");

            var selectionGroup = DbContext.SelectionGroup
                .Where(sg => sg.Id == selectionGroupId)
                .SingleOrDefault();

            #region Preliminary validation
            if (selectionGroup == null)
            {
                Log.Debug($"In ContentAccessAdminController.RenameSelectionGroup action: selection roup with ID {selectionGroupId} not found");
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, selectionGroup.RootContentItemId));
            if (!roleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.RenameSelectionGroup action: authorization failure, user {User.Identity.Name}, selection group ID {selectionGroupId}, role {RoleEnum.ContentAccessAdmin.ToString()}");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentAccessAdmin));
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            selectionGroup.GroupName = name;
            DbContext.SelectionGroup.Update(selectionGroup);
            DbContext.SaveChanges();
            Log.Verbose($"In ContentAccessAdminController.RenameSelectionGroup action: success");

            var model = Models.ContentAccessAdmin.SelectionGroupSummary.Build(DbContext, selectionGroup);

            return Json(model);
        }

        /// <summary>
        /// Adds a single user by email to a selection group.
        /// </summary>
        /// <remarks>This is a temporary helper action to be used until content access admin is rewritten in React.</remarks>
        /// <param name="SelectionGroupId">The selection group to be updated.</param>
        /// <param name="username">The username of the user to add.</param>
        /// <returns>Ok, or UnprocessableEntity if a user with the provided email does not exist.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserToSelectionGroup(Guid SelectionGroupId, string username)
        {
            Log.Verbose($"Entered ContentAccessAdminController.AddUserToSelectionGroup action: with SelectionGroup ID {SelectionGroupId}, user name {username}");

            SelectionGroup selectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rci => rci.Client)
                .SingleOrDefault(sg => sg.Id == SelectionGroupId);

            #region Preliminary Validation
            if (selectionGroup == null)
            {
                Log.Debug($"In ContentAccessAdminController.AddUserToSelectionGroup action: selection group {SelectionGroupId} not found");
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, selectionGroup.RootContentItemId));
            if (!roleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.AddUserToSelectionGroup action: authorization failure, user {User.Identity.Name}, content item {selectionGroup.RootContentItemId}, role {RoleEnum.ContentAccessAdmin.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentAccessAdmin));
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            var user = DbContext.ApplicationUser
                .Where(u => u.UserName == username)
                .SingleOrDefault();

            #region Validation
            if (user == null)
            {
                Log.Debug($"In ContentAccessAdminController.AddUserToSelectionGroup action: user {User.Identity.Name} not found, aborting");
                Response.Headers.Add("Warning", "One or more requested users do not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            return await UpdateSelectionGroupUserAssignments(SelectionGroupId, new Dictionary<Guid, bool> { { user.Id, true } });
        }

        /// <summary>Updates the users assigned to a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group to be updated.</param>
        /// <param name="UserAssignments">A dictionary that maps client IDs to a boolean value indicating whether to add or remove the client.</param>
        /// <returns>JsonResult</returns>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSelectionGroupUserAssignments(Guid SelectionGroupId, Dictionary<Guid, Boolean> UserAssignments)
        {
            Log.Verbose($"Entered ContentAccessAdminController.UpdateSelectionGroupUserAssignments action: with SelectionGroup ID {SelectionGroupId}, {{@UserAssignments}}", UserAssignments);

            SelectionGroup SelectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rci => rci.Client)
                .SingleOrDefault(sg => sg.Id == SelectionGroupId);

            #region Preliminary Validation
            if (SelectionGroup == null)
            {
                Log.Debug($"In ContentAccessAdminController.UpdateSelectionGroupUserAssignments action: selection group {SelectionGroupId} not found");
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.UpdateSelectionGroupUserAssignments action: authorization failure, user {User.Identity.Name}, content item {SelectionGroup.RootContentItemId}, role {RoleEnum.ContentAccessAdmin.ToString()}");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentAccessAdmin));
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
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
                Log.Debug($"In ContentAccessAdminController.UpdateSelectionGroupUserAssignments action: one or more requested users do not exist, aborting");
                Response.Headers.Add("Warning", "One or more requested users do not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var Permissioned = DbContext.UserRoleInClient
                .Where(ur => UserAdditions.Contains(ur.UserId))
                .Where(ur => ur.ClientId == SelectionGroup.RootContentItem.ClientId)
                .Where(ur => ur.RoleId == (ApplicationRole.RoleIds[RoleEnum.ContentUser]));
            if (Permissioned.Count() < UserAdditions.Count())
            {
                Log.Debug($"In ContentAccessAdminController.UpdateSelectionGroupUserAssignments action: one or more requested users are not authorized to the content item, aborting");
                Response.Headers.Add("Warning", "One or more requested users do not have permission to use this content.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var AlreadyInAnotherGroup = DbContext.UserInSelectionGroup
                .Where(usg => UserAdditions.Contains(usg.UserId))
                .Where(usg => usg.SelectionGroupId != SelectionGroup.Id)
                .Where(usg => usg.SelectionGroup.RootContentItemId == SelectionGroup.RootContentItemId);
            if (AlreadyInAnotherGroup.Any())
            {
                Log.Debug($"In ContentAccessAdminController.UpdateSelectionGroupUserAssignments action: one or more requested users are already authorized to another selection group for the same content item, aborting");
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
                Log.Error(ErrMsg);
                Response.Headers.Add("Warning", $"Failed to complete transaction.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            #region Log
            Log.Verbose($"In ContentAccessAdminController.UpdateSelectionGroupUserAssignments action: success");
            foreach (var UserAddition in UserAdditions)
            {
                AuditLogger.Log(AuditEventType.SelectionGroupUserAssigned.ToEvent(SelectionGroup, UserAddition));
            }
            foreach (var UserRemoval in UserRemovals)
            {
                AuditLogger.Log(AuditEventType.SelectionGroupUserRemoved.ToEvent(SelectionGroup, UserRemoval));
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
        public async Task<IActionResult> SetSuspendedSelectionGroup(Guid selectionGroupId, bool isSuspended)
        {
            Log.Verbose($"Entered ContentAccessAdminController.SetSuspendedSelectionGroup action: with SelectionGroup ID {selectionGroupId}, suspend {isSuspended}");

            SelectionGroup selectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                .SingleOrDefault(sg => sg.Id == selectionGroupId);

            #region Preliminary Validation
            if (selectionGroup == null)
            {
                Log.Debug($"In ContentAccessAdminController.SetSuspendedSelectionGroup action: selection group {selectionGroupId} not found");
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, selectionGroup.RootContentItemId));
            if (!roleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.SetSuspendedSelectionGroup action: authorization failure, user {User.Identity.Name}, content item {selectionGroup.RootContentItemId}, role {RoleEnum.ContentAccessAdmin.ToString()}");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentAccessAdmin));
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            selectionGroup.IsSuspended = isSuspended;
            DbContext.SelectionGroup.Update(selectionGroup);
            DbContext.SaveChanges();

            Log.Verbose($"In ContentAccessAdminController.SetSuspendedSelectionGroup action: success");
            AuditLogger.Log(AuditEventType.SelectionGroupSuspensionUpdate.ToEvent(selectionGroup, isSuspended, ""));

            var model = SelectionsDetail.Build(DbContext, Queries, selectionGroup);

            return Json(model);
        }

        /// <summary>Deletes a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the specified root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group to be deleted.</param>
        /// <returns>JsonResult</returns>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelectionGroup(Guid SelectionGroupId)
        {
            Log.Verbose($"Entered ContentAccessAdminController.DeleteSelectionGroup action with SelectionGroup ID {SelectionGroupId}");

            SelectionGroup selectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rci => rci.Client)
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rci => rci.ContentType)
                .SingleOrDefault(sg => sg.Id == SelectionGroupId);

            #region Preliminary Validation
            if (selectionGroup == null)
            {
                Log.Debug($"In ContentAccessAdminController.DeleteSelectionGroup action: selection group {SelectionGroupId} not found");
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, selectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.DeleteSelectionGroup action: authorization failure: user {User.Identity.Name}, content item {selectionGroup.RootContentItemId}, role {RoleEnum.ContentAccessAdmin.ToString()}");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentAccessAdmin));
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            // reject this request if the RootContentItem has a pending publication request
            bool blockedByPendingPublication = DbContext.ContentPublicationRequest
                .Where(pr => pr.RootContentItemId == selectionGroup.RootContentItem.Id)
                .Any(pr => pr.RequestStatus.IsActive());
            if (blockedByPendingPublication)
            {
                Log.Debug($"In ContentAccessAdminController.DeleteSelectionGroup action: action blocked by pending publication, aborting");
                Response.Headers.Add("Warning", "A selection group may not be deleted while this content item has a pending publication.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            List<Guid> RemovedUsers = new List<Guid>();

            try
            {
                using (IDbContextTransaction DbTransaction = DbContext.Database.BeginTransaction())
                {
                    List<UserInSelectionGroup> UsersToRemove = DbContext.UserInSelectionGroup
                        .Where(usg => usg.SelectionGroupId == selectionGroup.Id)
                        .ToList();
                    DbContext.UserInSelectionGroup.RemoveRange(UsersToRemove);
                    DbContext.SaveChanges();

                    DbContext.SelectionGroup.Remove(
                        DbContext.SelectionGroup
                            .Where(sg => sg.Id == selectionGroup.Id)
                            .Single()
                        );
                    DbContext.SaveChanges();

                    DbTransaction.Commit();

                    RemovedUsers = UsersToRemove
                        .Select(uug => uug.UserId)
                        .ToList();
                }

                // ContentType specific handling after successful transaction
                switch (selectionGroup.RootContentItem.ContentType.TypeEnum)
                {
                    case ContentTypeEnum.Qlikview:
                        if (!selectionGroup.IsMaster && !string.IsNullOrWhiteSpace(selectionGroup.ContentInstanceUrl))
                        {
                            string ContentFileFullPath = Path.Combine(ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath"), selectionGroup.ContentInstanceUrl);

                            await new QlikviewLibApi().ReclaimAllDocCalsForFile(selectionGroup.ContentInstanceUrl, QvConfig);

                            if (System.IO.File.Exists(ContentFileFullPath))
                            {
                                System.IO.File.Delete(ContentFileFullPath);
                            }
                            if (System.IO.File.Exists(ContentFileFullPath + ".Shared"))
                            {
                                System.IO.File.Delete(ContentFileFullPath + ".Shared");
                            }
                            if (System.IO.File.Exists(ContentFileFullPath + ".Meta"))
                            {
                                System.IO.File.Delete(ContentFileFullPath + ".Meta");
                            }
                        }
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Exception while deleting selection group \"{SelectionGroupId}\" or removing members: [{string.Join(",", RemovedUsers)}]");
                Log.Error(ErrMsg);
                Response.Headers.Add("Warning", $"Failed to complete transaction.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            Log.Verbose($"In ContentAccessAdminController.DeleteSelectionGroup action: success");
            AuditLogger.Log(AuditEventType.SelectionGroupDeleted.ToEvent(selectionGroup));

            SelectionGroupList Model = SelectionGroupList.Build(DbContext, selectionGroup.RootContentItem);

            return Json(Model);
        }

        /// <summary>Returns the selections associated with a selection group.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the related root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group whose selections are to be returned.</param>
        /// <returns>JsonResult</returns>
        [HttpGet]
        public async Task<IActionResult> Selections(Guid SelectionGroupId)
        {
            Log.Verbose($"Entered ContentAccessAdminController.Selections action with SelectionGroup ID {SelectionGroupId}");

            SelectionGroup SelectionGroup = DbContext.SelectionGroup.Find(SelectionGroupId);

            #region Preliminary validation
            if (SelectionGroup == null)
            {
                Log.Debug($"In ContentAccessAdminController.Selections action: selection group {SelectionGroupId} not found");
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.Selections action: authorization failure: user {User.Identity.Name}, content item {SelectionGroup.RootContentItemId}, role {RoleEnum.ContentAccessAdmin.ToString()}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
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
        public async Task<IActionResult> UpdateSelections(Guid selectionGroupId, bool isMaster, Guid[] selections)
        {
            Log.Verbose($"Entered ContentAccessAdminController.UpdateSelections action with SelectionGroup ID {selectionGroupId}, is master {isMaster}, {{@selections}}", selections);

            var selectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(c => c.ContentType)
                .Where(sg => sg.Id == selectionGroupId)
                .SingleOrDefault();

            #region Preliminary validation
            if (selectionGroup == null)
            {
                Log.Debug($"In ContentAccessAdminController.UpdateSelections action: selection group not found, aborting");
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, selectionGroup.RootContentItemId));
            if (!roleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.UpdateSelections action: authorization failure, user {User.Identity.Name}, content item {selectionGroup.RootContentItemId}, role {RoleEnum.ContentAccessAdmin.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentAccessAdmin));
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            // reject this request if the RootContentItem has a pending publication request
            bool blockedByPendingPublication = DbContext.ContentPublicationRequest
                .Where(pr => pr.RootContentItemId == selectionGroup.RootContentItem.Id)
                .Any(pr => pr.RequestStatus.IsActive());
            if (blockedByPendingPublication)
            {
                Log.Debug($"In ContentAccessAdminController.UpdateSelections action: action blocked by pending publication, aborting");
                Response.Headers.Add("Warning", "Selections may not be updated while this content item has a pending publication.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (!selectionGroup.RootContentItem.DoesReduce)
            {
                Log.Debug($"In ContentAccessAdminController.UpdateSelections action: action invoked for non-reducing content item, aborting");
                Response.Headers.Add("Warning", "The requested selection group belongs to a content item that cannot be reduced.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var currentLivePublication = DbContext.ContentPublicationRequest
                .Where(request => request.RootContentItemId == selectionGroup.RootContentItemId)
                .Where(request => request.RequestStatus == PublicationStatus.Confirmed)
                .SingleOrDefault();
            if (currentLivePublication == null)
            {
                Log.Debug($"In ContentAccessAdminController.UpdateSelections action: action invoked for non-live content item, aborting");
                Response.Headers.Add("Warning", "There is no live content for the requested selection group.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // There must be no pending reduction task for this selection group
            if (DbContext.ContentReductionTask
                .Where(task => task.SelectionGroupId == selectionGroup.Id)
                .Where(task => task.CreateDateTimeUtc > currentLivePublication.CreateDateTimeUtc)
                .Any(task => task.ReductionStatus.IsActive()))
            {
                Log.Debug($"In ContentAccessAdminController.UpdateSelections action: action blocked due to a pending reduction task, aborting");
                Response.Headers.Add("Warning", "An unresolved publication or selection change prevents this action.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (isMaster)
            {
                if (selectionGroup.IsMaster)
                {
                    Log.Debug($"In ContentAccessAdminController.UpdateSelections action: request to make selection group {selectionGroup.Id} master but it is already master, aborting");
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
                    Log.Debug($"In ContentAccessAdminController.UpdateSelections action: request to update selection group {selectionGroup.Id} using invalid selection value(s), aborting");
                    Response.Headers.Add("Warning", "One or more requested selections do not exist or do not belong to the specified content item.");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }

                if (!selectionGroup.IsMaster)
                {
                    // The requested selections must be modified from the live selections for this SelectionGroup
                    if (selections.ToHashSet().SetEquals(selectionGroup.SelectedHierarchyFieldValueList))
                    {
                        Log.Debug($"In ContentAccessAdminController.UpdateSelections action: request to update selection group {selectionGroup.Id} with unchanged selections, aborting");
                        Response.Headers.Add("Warning", "The requested selections are not different from the active document.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                }
            }

            // Require that the live master file path is stored in the RootContentItem and the file exists
            ContentRelatedFile LiveMasterFile = selectionGroup.RootContentItem.ContentFilesList.SingleOrDefault(f => f.FilePurpose.ToLower() == "mastercontent");
            if (LiveMasterFile == null 
             || !System.IO.File.Exists(LiveMasterFile.FullPath))
            {
                Log.Debug($"In ContentAccessAdminController.UpdateSelections action: request to update selection group {selectionGroup.Id} but master content file for the content item {selectionGroup.RootContentItemId} is not found");
                Response.Headers.Add("Warning", "A master content file does not exist for the requested content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            if (isMaster)
            {
                selectionGroup.IsMaster = true;
                selectionGroup.SelectedHierarchyFieldValueList = new Guid[0];
                selectionGroup.SetContentUrl(Path.GetFileName(LiveMasterFile.FullPath));
                DbContext.SelectionGroup.Update(selectionGroup);
                DbContext.SaveChanges();

                AuditLogger.Log(AuditEventType.SelectionChangeMasterAccessGranted.ToEvent(selectionGroup));
            }
            else
            {
                // Stage the master file in a task folder in the file exchange share
                Guid NewTaskGuid = Guid.NewGuid();
                string TaskFolderPath = Path.Combine(ApplicationConfig.GetValue<string>("Storage:MapPublishingServerExchangePath"), NewTaskGuid.ToString("D"));
                Directory.CreateDirectory(TaskFolderPath);
                string MasterFileCopyTarget = Path.Combine(TaskFolderPath, Path.GetFileName(LiveMasterFile.FullPath));
                System.IO.File.Copy(LiveMasterFile.FullPath, MasterFileCopyTarget);

                var contentReductionTask = new ContentReductionTask
                {
                    Id = NewTaskGuid,
                    ApplicationUser = await Queries.GetCurrentApplicationUser(User),
                    SelectionGroupId = selectionGroup.Id,
                    MasterFilePath = MasterFileCopyTarget,
                    MasterContentChecksum = LiveMasterFile.Checksum,
                    ContentPublicationRequest = null,
                    SelectionCriteriaObj = ContentReductionHierarchy<ReductionFieldValueSelection>.GetFieldSelectionsForSelectionGroup(DbContext, selectionGroupId, selections),
                    ReductionStatus = ReductionStatusEnum.Queued,
                    CreateDateTimeUtc = DateTime.UtcNow,
                    TaskAction = TaskActionEnum.HierarchyAndReduction,
                };
                DbContext.ContentReductionTask.Add(contentReductionTask);
                DbContext.SaveChanges();

                string CxnString = ApplicationConfig.GetConnectionString("DefaultConnection");  // key string must match that used in startup.cs
                string ContentItemRootPath = ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath");

                object ContentTypeConfigObj = null;
                switch (selectionGroup.RootContentItem.ContentType.TypeEnum)
                {
                    case ContentTypeEnum.Qlikview:
                        ContentTypeConfigObj = QvConfig;
                        break;

                    case ContentTypeEnum.Unknown:
                    default:
                        break;
                }
                ContentAccessSupport.AddReductionMonitor(Task.Run(() => ContentAccessSupport.MonitorReductionTaskForGoLive(NewTaskGuid, CxnString, ContentItemRootPath, ContentTypeConfigObj)));

                AuditLogger.Log(AuditEventType.SelectionChangeReductionQueued.ToEvent(selectionGroup, contentReductionTask));
            }
            Log.Verbose("In ContentAccessAdminController.UpdateSelections action: success");
            SelectionsDetail model = SelectionsDetail.Build(DbContext, Queries, selectionGroup);

            return Json(model);
        }

        /// <summary>Cancel a pending or completed reduction task.</summary>
        /// <remarks>This action is only authorized to users with ContentAccessAdmin role in the related root content item.</remarks>
        /// <param name="SelectionGroupId">The selection group associated with the reduction to be canceled.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReduction(Guid SelectionGroupId)
        {
            Log.Verbose($"Entered ContentAccessAdminController.CancelReduction action with SelectionGroup ID {SelectionGroupId}");

            SelectionGroup SelectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                .Where(sg => sg.Id == SelectionGroupId)
                .SingleOrDefault();

            #region Preliminary validation
            if (SelectionGroup == null)
            {
                Log.Debug($"In ContentAccessAdminController.CancelReduction action: selection group {SelectionGroupId} not found, aborting");
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, SelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.CancelReduction action: authorization failure, user {User.Identity.Name}, content item {SelectionGroup.RootContentItemId}, role {RoleEnum.ContentAccessAdmin.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentAccessAdmin));
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var CancelableTasks = DbContext.ContentReductionTask
                .Where(crt => crt.SelectionGroupId == SelectionGroup.Id)
                .Where(crt => crt.ReductionStatus.IsCancelable())
                .Where(crt => crt.ContentPublicationRequestId == null);
            if (CancelableTasks.Count() == 0)
            {
                Log.Debug($"In ContentAccessAdminController.CancelReduction action: no cancelable tasks for this content item, aborting");
                Response.Headers.Add("Warning", "There are no cancelable tasks for this content item.");
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

            Log.Debug($"In ContentAccessAdminController.CancelReduction action: tasks cancelled: {string.Join(", ", UpdatedTasks.Select(t=>t.Id.ToString()))}");
            foreach (var Task in UpdatedTasks)
            {
                AuditLogger.Log(AuditEventType.SelectionChangeReductionCanceled.ToEvent(SelectionGroup, Task));
            }

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
