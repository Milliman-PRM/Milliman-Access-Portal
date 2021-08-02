/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Actions to support administration of user authorization to hosted content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using AuditLogLib.Services;
using MapCommonLib.ActionFilters;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using QlikviewLib;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    [LogActionBeforeAfter]
    public class ContentAccessAdminController : Controller
    {
        private readonly RoleEnum requiredRole = RoleEnum.ContentAccessAdmin;

        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly IConfiguration ApplicationConfig;
        private readonly ApplicationDbContext DbContext;
        private readonly ContentAccessAdminQueries _accessAdminQueries;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly QlikviewConfig QvConfig;

        public ContentAccessAdminController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ApplicationDbContext DbContextArg,
            ContentAccessAdminQueries accessAdminQueriesArg,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration ApplicationConfigArg,
            IOptions<QlikviewConfig> QvConfigArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DbContext = DbContextArg;
            _accessAdminQueries = accessAdminQueriesArg;
            UserManager = UserManagerArg;
            ApplicationConfig = ApplicationConfigArg;
            QvConfig = QvConfigArg.Value;
        }

        /// <summary>
        /// GET content access admin view
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            #region Authorization
            AuthorizationResult roleResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(requiredRole));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access.");
                return Unauthorized();
            }
            #endregion

            return View();
        }

        /// <summary>
        /// Global constant data that is relevant at the page level
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> PageGlobalData()
        {
            #region Authorization
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(requiredRole));
            if (!RoleInClientResult.Succeeded)
            {
                Log.Debug($"In ContentAccessAdminController.PageGlobalData action: authorization failure, user {User.Identity.Name}, role {requiredRole.ToString()}");
                Response.Headers.Add("Warning", "You are not authorized to manage content access.");
                return Unauthorized();
            }
            #endregion

            ContentAccessAdminPageGlobalModel model = await _accessAdminQueries.BuildAccessAdminPageGlobalModelAsync();

            return Json(model);
        }

        /// <summary>
        /// GET clients authorized to the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            #region Authorization
            var roleResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(requiredRole));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access.");
                return Unauthorized();
            }
            #endregion

            var currentUser = await UserManager.GetUserAsync(User);
            var clients = await _accessAdminQueries.GetAuthorizedClientsModelAsync(currentUser);

            return Json(clients);
        }

        /// <summary>
        /// GET content items for a client authorized to the current user
        /// </summary>
        /// <param name="clientId">Client to whom content items must belong</param>
        [HttpGet]
        public async Task<IActionResult> ContentItems([EmitBeforeAfterLog] Guid clientId)
        {
            #region Authorization
            var roleResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(requiredRole, clientId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access for this client.");
                return Unauthorized();
            }
            #endregion

            var currentUser = await UserManager.GetUserAsync(User);
            var contentItems = await _accessAdminQueries.SelectContentItemsAsync(currentUser, clientId);

            return Json(contentItems);
        }

        /// <summary>
        /// GET selection groups for a content item authorized to the current user
        /// </summary>
        /// <param name="contentItemId">Content item to whom the selection groups must belong</param>
        [HttpGet]
        public async Task<IActionResult> SelectionGroups([EmitBeforeAfterLog] Guid contentItemId)
        {
            #region Authorization
            var roleResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, contentItemId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access for this content item.");
                return Unauthorized();
            }
            #endregion

            var selectionGroups = await _accessAdminQueries.SelectSelectionGroupsAsync(contentItemId);

            return Json(selectionGroups);
        }

        /// <summary>
        /// GET selections for a selection group authorized to the current user
        /// </summary>
        /// <param name="groupId">Selection group to whom the selections must belong</param>
        [HttpGet]
        public async Task<IActionResult> Selections([EmitBeforeAfterLog] Guid groupId)
        {
            Guid contentItemId = await DbContext.SelectionGroup
                                                .Where(g => g.Id == groupId)
                                                .Select(g => g.RootContentItemId)
                                                .SingleOrDefaultAsync();

            #region Authorization
            var roleResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, contentItemId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            SelectionsResponseModel selections = await _accessAdminQueries.SelectSelectionsAsync(groupId);

            return Json(selections);
        }

        /// <summary>
        /// GET publication and reduction status
        /// </summary>
        /// <param name="clientId">Client to whom publications should belong</param>
        /// <param name="contentItemId">Content item to whom reductions should belong</param>
        [HttpGet]
        [PreventAuthRefresh]
        public async Task<IActionResult> Status([EmitBeforeAfterLog] Guid clientId, [EmitBeforeAfterLog] Guid contentItemId)
        {
            var currentUser = await UserManager.GetUserAsync(User);
            var status = await _accessAdminQueries.SelectStatusAsync(currentUser, clientId, contentItemId);

            return Json(status);
        }

        /// <summary>
        /// POST a selection group to create
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequestModel model)
        {
            var contentItem = await DbContext.RootContentItem
                                             .Include(i => i.Client)
                                             .Include(i => i.ContentType)
                                             .SingleOrDefaultAsync(i => i.Id == model.ContentItemId);

            #region Authorization
            var roleResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, model.ContentItemId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            // reject this request if the RootContentItem has a pending publication request
            bool blockedByPendingPublication = await DbContext.ContentPublicationRequest
                                                              .Where(r => r.RootContentItemId == contentItem.Id)
                                                              .Where(r => PublicationStatusExtensions.ActiveStatuses.Contains(r.RequestStatus))
                                                              .AnyAsync();
            if (blockedByPendingPublication)
            {
                Response.Headers.Add("Warning",
                    "A new selection group may not be created while this content item has a pending publication.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (!contentItem.DoesReduce && contentItem.ContentType.TypeEnum.LiveContentFileStoredInMap())
            {
                ContentRelatedFile liveMasterFile = contentItem.ContentFilesList
                    .SingleOrDefault(f => f.FilePurpose.ToLower() == "mastercontent");
                if (liveMasterFile == null || !System.IO.File.Exists(liveMasterFile.FullPath))
                {
                    Response.Headers.Add("Warning",
                        "A master content file does not exist for the requested content item.");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }
            #endregion

            var selectionGroups = contentItem.DoesReduce
                ? await _accessAdminQueries.CreateReducingGroupAsync(model.ContentItemId, model.Name)
                : await _accessAdminQueries.CreateMasterGroupAsync(model.ContentItemId, model.Name);

            return Json(selectionGroups);
        }

        private static object updateGroupLockObj = new object();
        private static SemaphoreSlim updateGroupSemaphore = new SemaphoreSlim(1, 1);
        /// <summary>
        /// POST an update to a selection group
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGroup([FromBody] UpdateGroupRequestModel model)
        {
            (Guid contentItemId, Guid clientId) = (await DbContext.SelectionGroup
                                                                  .Where(g => g.Id == model.GroupId)
                                                                  .Select(g => g.RootContentItem)
                                                                  .ToListAsync())
                                                .ConvertAll(r => (r.Id, r.ClientId))
                                                .SingleOrDefault();

            #region Authorization
            var roleResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, contentItemId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            UpdateGroupResponseModel responseModel = default;

            bool semaphoreAcquired = await updateGroupSemaphore.WaitAsync(TimeSpan.FromSeconds(15));
            if (semaphoreAcquired)
            {
                try
                {
                    #region Validation
                    int accountsExistCount = DbContext.ApplicationUser.Count(u => model.Users.Contains(u.Id));
                    if (accountsExistCount < model.Users.Count)
                    {
                        Response.Headers.Add("Warning", "One or more requested users do not exist in the system.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }

                    int hasClientContentRoleCount = DbContext.UserRoleInClient
                        .Where(r => model.Users.Contains(r.UserId))
                        .Where(r => r.ClientId == clientId)
                        .Where(r => r.Role.RoleEnum == RoleEnum.ContentUser)
                        .Count();
                    if (hasClientContentRoleCount < model.Users.Count)
                    {
                        Response.Headers.Add("Warning",
                            "One or more requested users do not have permission to view content of this client.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }

                    bool anyAlreadyInAnotherGroup = DbContext.UserInSelectionGroup
                        .Where(u => model.Users.Contains(u.UserId))
                        .Where(u => u.SelectionGroup.RootContentItemId == contentItemId)
                        .Where(u => u.SelectionGroupId != model.GroupId)
                        .Any();
                    if (anyAlreadyInAnotherGroup)
                    {
                        Response.Headers.Add("Warning",
                            "One or more requested users are curently in a different selection group for the same content.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                    #endregion

                    responseModel = await _accessAdminQueries.UpdateGroupAsync(model.GroupId, model.Name, model.Users);
                }
                finally
                {
                    updateGroupSemaphore.Release();
                }
            }

            return Json(responseModel);
        }

        /// <summary>
        /// POST a selection group to suspend
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendGroup([FromBody] SuspendGroupRequestModel model)
        {
            SelectionGroup sg = await DbContext.SelectionGroup.SingleOrDefaultAsync(g => g.Id == model.GroupId);
            if (sg == null)
            {
                Response.Headers.Add("Warning", "The requested selection group was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            Guid contentItemId = sg.RootContentItemId;

            #region Authorization
            var roleResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, contentItemId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            var group = await _accessAdminQueries.SetGroupSuspendedAsync(model.GroupId, model.IsSuspended);

            return Json(group);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetGroupPowerBiEditability([FromBody] SetPowerBiEditabilityRequestModel model)
        {
            SelectionGroup selectionGroup = await DbContext.SelectionGroup
                                                           .Include(sg => sg.RootContentItem)
                                                               .ThenInclude(rci => rci.ContentType)
                                                           .SingleOrDefaultAsync(sg => sg.Id == model.GroupId);

            #region Authorization
            var roleResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, selectionGroup.RootContentItemId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            if (selectionGroup == null)
            {
                Response.Headers.Add("Warning", "The requested selection group was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (selectionGroup.RootContentItem.ContentType.TypeEnum != ContentTypeEnum.PowerBi)
            {
                Response.Headers.Add("Warning", "Cannot toggle attribute 'Editable' for this content type.");
                Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName}: Failed to toggle 'Editable' attribute of Selection Group {model.GroupId} due to improper content type.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (!selectionGroup.IsEditablePowerBiEligible)
            {
                Response.Headers.Add("Warning", "Content item is not eligible for editability.");
                Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName}: Failed to toggle 'Editable' attribute of Selection Group {model.GroupId} due to Content Item ineligibility.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            selectionGroup.TypeSpecificDetailObject = new PowerBiSelectionGroupProperties()
            {
                Editable = model.Editable
            };
            await DbContext.SaveChangesAsync();

            return Json(selectionGroup);
        }

        /// <summary>
        /// POST a selection group to delete
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroup([FromBody] DeleteGroupRequestModel model)
        {
            var selectionGroup = await DbContext.SelectionGroup
                                                .Include(sg => sg.RootContentItem)
                                                    .ThenInclude(rci => rci.ContentType)
                                                .SingleOrDefaultAsync(sg => sg.Id == model.GroupId);

            #region Authorization
            var roleResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, selectionGroup?.RootContentItemId));
            if (!roleResult.Succeeded)
            {
                Log.Information($"Action {ControllerContext.ActionDescriptor.DisplayName} user {User.Identity.Name} is not authorized to content item {selectionGroup?.RootContentItemId}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            // reject this request if the SelectionGroup has a pending reduction
            DateTime lastLiveReductionDateTime = await DbContext.ContentReductionTask
                                                                .Where(t => t.SelectionGroupId == selectionGroup.Id)
                                                                .Where(t => new[] { ReductionStatusEnum.Live, ReductionStatusEnum.Replaced }.Contains(t.ReductionStatus))
                                                                .OrderByDescending(t => t.CreateDateTimeUtc)
                                                                .Take(1)
                                                                .Select(t => t.CreateDateTimeUtc)
                                                                .SingleOrDefaultAsync();

            bool blockedByPendingReduction = await DbContext.ContentReductionTask
                                                            .Where(r => r.SelectionGroupId == selectionGroup.Id)
                                                            .Where(r => r.CreateDateTimeUtc > lastLiveReductionDateTime)
                                                            .AnyAsync(r => ReductionStatusExtensions.activeStatusList.Contains(r.ReductionStatus));
            if (blockedByPendingReduction)
            {
                Log.Information($"Action {ControllerContext.ActionDescriptor.DisplayName} aborting because a pending reduction exists for this selection group {selectionGroup.Id}");
                Response.Headers.Add("Warning", "A selection group may not be deleted while it has a pending reduction.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // reject this request if the related RootContentItem has a pending publication request
            bool blockedByPendingPublication = await DbContext.ContentPublicationRequest
                                                              .Where(r => r.RootContentItemId == selectionGroup.RootContentItemId)
                                                              .AnyAsync(r => PublicationStatusExtensions.ActiveStatuses.Contains(r.RequestStatus));
            if (blockedByPendingPublication)
            {
                Log.Information($"Action {ControllerContext.ActionDescriptor.DisplayName} aborting for selection group {selectionGroup.Id} because an active publication request exists for related content itme {selectionGroup.RootContentItemId}");
                Response.Headers.Add("Warning", "A selection group may not be deleted while this content item has a pending publication.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            var selectionGroupModel = await _accessAdminQueries.DeleteGroupAsync(model.GroupId);

            #region file cleanup
            // ContentType specific handling after successful transaction
            switch (selectionGroup.RootContentItem.ContentType.TypeEnum)
            {
                case ContentTypeEnum.Qlikview:
                    if (!selectionGroup.IsMaster && !string.IsNullOrWhiteSpace(selectionGroup.ContentInstanceUrl))
                    {
                        string ContentFileFullPath = Path.Combine(
                            ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath"),
                            selectionGroup.ContentInstanceUrl);

                        try
                        {
                            await new QlikviewLibApi(QvConfig).ReclaimAllDocCalsForFile(selectionGroup.ContentInstanceUrl);

                            System.IO.File.Delete(ContentFileFullPath);

                            if (System.IO.File.Exists($"{ContentFileFullPath}.TShared"))
                            {
                                System.IO.File.Delete($"{ContentFileFullPath}.TShared");
                            }

                            if (System.IO.File.Exists($"{ContentFileFullPath}.Meta"))
                            {
                                System.IO.File.Delete($"{ContentFileFullPath}.Meta");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"In action {ControllerContext.ActionDescriptor.DisplayName}, failed while reclaiming Qlikview document CAL or deleting file");
                        }
                    }
                    break;

                case ContentTypeEnum.Html:
                case ContentTypeEnum.Pdf:
                case ContentTypeEnum.FileDownload:
                case ContentTypeEnum.PowerBi:
                default:
                    // for all non-reducible content types, do nothing.
                    break;
            }
            #endregion

            return Json(selectionGroupModel);
        }

        /// <summary>
        /// POST a change to selected values
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSelections([FromBody] UpdateSelectionsRequestModel model)
        {
            return await UpdateSelectionsAsync(model.GroupId, model.IsMaster, model.Selections.ToArray());
        }

        /// <summary>
        /// POST a request to cancel a reduction
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReduction([FromBody] CancelReductionRequestModel model)
        {
            return await CancelReductionAsync(model.GroupId);
        }

        [NonAction]
        internal async Task<IActionResult> UpdateSelectionsAsync(Guid selectionGroupId, bool isMaster, IEnumerable<Guid> selections)
        {
            var selectionGroup = await DbContext.SelectionGroup
                                                .Include(sg => sg.RootContentItem)
                                                    .ThenInclude(c => c.ContentType)
                                                .Include(sg => sg.RootContentItem)
                                                    .ThenInclude(c => c.Client)
                                                .SingleOrDefaultAsync(sg => sg.Id == selectionGroupId);

            ApplicationUser currentUser = await UserManager.GetUserAsync(User);

            #region Preliminary validation
            if (selectionGroup == null)
            {
                Log.Warning($"In ContentAccessAdminController.UpdateSelections: selection group {selectionGroupId} not found, aborting");
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, selectionGroup.RootContentItemId));
            if (!roleInRootContentItemResult.Succeeded)
            {
                Log.Information($"In ContentAccessAdminController.UpdateSelections: authorization failure, user {User.Identity.Name}, content item {selectionGroup.RootContentItemId}, role {RoleEnum.ContentAccessAdmin.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentAccessAdmin), currentUser.UserName, currentUser.Id);
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            ContentReductionTask liveTask = await DbContext.ContentReductionTask
                                                           .Where(t => t.SelectionGroup.RootContentItemId == selectionGroup.RootContentItemId)
                                                           .Where(t => t.MasterContentHierarchy != null)
                                                           .FirstOrDefaultAsync(t => t.ReductionStatus == ReductionStatusEnum.Live);

            #region Validation
            // reject this request if the RootContentItem has a pending publication request
            bool blockedByPendingPublication = await DbContext.ContentPublicationRequest
                                                              .Where(pr => pr.RootContentItemId == selectionGroup.RootContentItem.Id)
                                                              .AnyAsync(pr => PublicationStatusExtensions.ActiveStatuses.Contains(pr.RequestStatus));
            if (blockedByPendingPublication)
            {
                Log.Information($"In ContentAccessAdminController.UpdateSelections: selection group {selectionGroupId} blocked by pending publication, aborting");
                Response.Headers.Add("Warning", "Selections may not be updated while this content item has a pending publication.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (!selectionGroup.RootContentItem.DoesReduce)
            {
                Log.Information($"In ContentAccessAdminController.UpdateSelections: invoked for selection group {selectionGroupId} with non-reducing content item {selectionGroup.RootContentItemId}, aborting");
                Response.Headers.Add("Warning", "The requested selection group belongs to a content item that cannot be reduced.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // There should always be 1 live publication but this query is designed to tolerate error conditions
            var currentLivePublication = await DbContext.ContentPublicationRequest
                                                        .Where(request => request.RootContentItemId == selectionGroup.RootContentItemId)
                                                        .Where(request => request.RequestStatus == PublicationStatus.Confirmed)
                                                        .OrderBy(request => request.CreateDateTimeUtc)
                                                        .Take(1)
                                                        .SingleOrDefaultAsync();
            if (currentLivePublication == null)
            {
                Log.Information($"In ContentAccessAdminController.UpdateSelections: invoked for selection group {selectionGroupId} with non-live content item {selectionGroup.RootContentItemId}, aborting");
                Response.Headers.Add("Warning", "There is no live content for the requested selection group.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // There must be no pending reduction task for this selection group
            var pendingReductions = DbContext.ContentReductionTask
                .Where(task => task.SelectionGroupId == selectionGroup.Id)
                .Where(task => task.CreateDateTimeUtc > currentLivePublication.CreateDateTimeUtc)
                .Where(task => ReductionStatusExtensions.activeStatusList.Contains(task.ReductionStatus));
            if (await pendingReductions.AnyAsync())
            {
                Log.Information($"In ContentAccessAdminController.UpdateSelections: selection group {selectionGroupId} blocked due to pending reduction (id {string.Join(",", pendingReductions.Select(t => t.Id))}), aborting");
                Response.Headers.Add("Warning", "An unresolved publication or selection change prevents this action.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (isMaster)
            {
                // Invalid to request update to unrestricted if the group already is unrestricted
                if (selectionGroup.IsMaster)
                {
                    Log.Information($"In ContentAccessAdminController.UpdateSelections: request to make selection group {selectionGroup.Id} master but it is already master, aborting");
                    Response.Headers.Add("Warning", "The specified selection group already has master content access.");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }

                // Invalid to convert a group to unrestricted if no previous task has live status and contains a master hierarchy
                if (liveTask == default)
                {
                    Log.Information($"In ContentAccessAdminController.UpdateSelections: Unable to find a live reduction task record with master hierarchy information");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }
            else
            {
                // The requested selections must be valid for the content item
                int validSelectionCount = await DbContext.HierarchyFieldValue
                                                         .Where(hfv => hfv.HierarchyField.RootContentItemId == selectionGroup.RootContentItemId)
                                                         .Where(hfv => selections.Contains(hfv.Id))
                                                         .CountAsync();
                if (validSelectionCount < selections.Count())
                {
                    Log.Information($"In ContentAccessAdminController.UpdateSelections: request to update selection group {selectionGroup.Id} using invalid selection value(s), aborting");
                    Response.Headers.Add("Warning", "One or more requested selections do not exist or do not belong to the specified content item.");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }

                if (!selectionGroup.IsMaster)
                {
                    // The requested selections must be modified from the live selections for this SelectionGroup
                    if (selections.ToHashSet().SetEquals(selectionGroup.SelectedHierarchyFieldValueList))
                    {
                        Log.Information($"In ContentAccessAdminController.UpdateSelections: request to update selection group {selectionGroup.Id} with unchanged selections, aborting");
                        Response.Headers.Add("Warning", "The requested selections are not different from the active document.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                }
            }

            // Require that the live master file path is stored in the RootContentItem and the file exists
            ContentRelatedFile LiveMasterFile = selectionGroup.RootContentItem.ContentFilesList.SingleOrDefault(f => f.FilePurpose.ToLower() == "mastercontent");
            if (selectionGroup.RootContentItem.ContentType.TypeEnum.LiveContentFileStoredInMap() &&
             (LiveMasterFile == null || !System.IO.File.Exists(LiveMasterFile.FullPath)))
            {
                Log.Information($"In ContentAccessAdminController.UpdateSelections: request to update selection group {selectionGroup.Id} but master content file {LiveMasterFile?.FullPath ?? "<unspecified>"} for the content item {selectionGroup.RootContentItemId} is not found");
                Response.Headers.Add("Warning", "A master content file does not exist for the requested content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            Guid NewTaskGuid = Guid.NewGuid();

            if (isMaster)
            {
                selectionGroup.IsMaster = true;
                selectionGroup.SelectedHierarchyFieldValueList = new List<Guid>();
                selectionGroup.SetContentUrl(Path.GetFileName(LiveMasterFile.FullPath));

                // Reset disclaimer acceptance
                var usersInGroup = await DbContext.UserInSelectionGroup
                                                  .Include(usg => usg.User)
                                                  .Include(usg => usg.SelectionGroup)
                                                  .Where(u => u.SelectionGroupId == selectionGroupId)
                                                  .ToListAsync();
                usersInGroup.ForEach(u => u.DisclaimerAccepted = false);

                DateTime taskTime = DateTime.UtcNow;
                // Record with a new ContentReductionTask record
                DbContext.ContentReductionTask.Add(new ContentReductionTask
                {
                    ApplicationUserId = currentUser.Id,
                    CreateDateTimeUtc = taskTime,
                    ProcessingStartDateTimeUtc = taskTime,
                    Id = NewTaskGuid,
                    MasterFilePath = "",
                    OutcomeMetadataObj = new ReductionTaskOutcomeMetadata
                    {
                        OutcomeReason = MapDbReductionTaskOutcomeReason.MasterHierarchyAssigned,
                        ReductionTaskId = NewTaskGuid,
                        UserMessage = MapDbReductionTaskOutcomeReason.MasterHierarchyAssigned.GetDisplayDescriptionString(),
                        SupportMessage = $"Selection Group {selectionGroup.Id} has been updated with unrestricted access",
                        SelectionGroupName = selectionGroup.GroupName,
                    },
                    ReductionStatus = ReductionStatusEnum.Live,
                    ReductionStatusMessage = $"Selection Group {selectionGroup.Id} has been updated with unrestricted access",
                    SelectionGroupId = selectionGroup.Id,
                    TaskAction = TaskActionEnum.Unspecified,
                    MasterContentChecksum = selectionGroup.RootContentItem
                                                          .ContentFilesList
                                                          .Single(f => f.FilePurpose.Equals("MasterContent", StringComparison.InvariantCultureIgnoreCase))
                                                          .Checksum,
                    MasterContentHierarchyObj = liveTask.MasterContentHierarchyObj,
                });

                // set live reduction to replaced
                var liveReductionTasks = await DbContext.ContentReductionTask
                                                        .Where(t => t.SelectionGroupId == selectionGroup.Id)
                                                        .Where(t => t.ReductionStatus == ReductionStatusEnum.Live)
                                                        .ToListAsync();
                liveReductionTasks.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Replaced);

                await DbContext.SaveChangesAsync();

                AuditLogger.Log(AuditEventType.SelectionChangeMasterAccessGranted.ToEvent(selectionGroup, selectionGroup.RootContentItem, selectionGroup.RootContentItem.Client), currentUser.UserName, currentUser.Id);
                AuditLogger.Log(AuditEventType.ContentDisclaimerAcceptanceReset
                    .ToEvent(usersInGroup, selectionGroup.RootContentItem, selectionGroup.RootContentItem.Client, ContentDisclaimerResetReason.ContentSelectionsModified), currentUser.UserName, currentUser.Id);
            }
            else
            {
                if (selections.Count() == 0)
                {
                    // Insert a new reduction task to record the unprocessable condition
                    DbContext.ContentReductionTask.Add(new ContentReductionTask
                    {
                        ApplicationUserId = currentUser.Id,
                        CreateDateTimeUtc = DateTime.UtcNow,
                        Id = NewTaskGuid,
                        MasterFilePath = "",  // required field - consider removing non null requirement
                        OutcomeMetadataObj = new ReductionTaskOutcomeMetadata
                        {
                            OutcomeReason = MapDbReductionTaskOutcomeReason.NoSelectedFieldValues,
                            ReductionTaskId = NewTaskGuid,
                            UserMessage = MapDbReductionTaskOutcomeReason.NoSelectedFieldValues.GetDisplayDescriptionString(),
                            SelectionGroupName = selectionGroup.GroupName,
                        },
                        ReductionStatus = ReductionStatusEnum.Live,
                        ReductionStatusMessage = "In ContentAccessAdminController.UpdateSelections, no selections, reduction task not queued",
                        SelectionGroupId = selectionGroup.Id,
                        TaskAction = TaskActionEnum.Unspecified,
                    });

                    // Update selection group fields
                    selectionGroup.IsMaster = false;
                    selectionGroup.SelectedHierarchyFieldValueList = new List<Guid>();
                    selectionGroup.ContentInstanceUrl = null;

                    // set live reduction to replaced
                    var liveReductionTasks = await DbContext.ContentReductionTask
                                                            .Where(t => t.SelectionGroupId == selectionGroup.Id)
                                                            .Where(t => t.ReductionStatus == ReductionStatusEnum.Live)
                                                            .ToListAsync();
                    liveReductionTasks.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Replaced);

                    await DbContext.SaveChangesAsync();

                    Log.Information($"In ContentAccessAdminController.UpdateSelections: request to update selection group {selectionGroup.Id} with no selections, cannot be processed, aborting");
                }
                else
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                    // The master file will be copied to a task folder in the file exchange share
                    string TaskFolderPath = Path.Combine(ApplicationConfig.GetValue<string>("Storage:MapPublishingServerExchangePath"), NewTaskGuid.ToString("D"));
                    Directory.CreateDirectory(TaskFolderPath);
                    string MasterFileCopyTarget = Path.Combine(TaskFolderPath, Path.GetFileName(LiveMasterFile.FullPath));

                    var contentReductionTask = new ContentReductionTask
                    {
                        Id = NewTaskGuid,
                        ApplicationUser = await UserManager.GetUserAsync(User),
                        SelectionGroupId = selectionGroup.Id,
                        MasterFilePath = MasterFileCopyTarget,
                        MasterContentChecksum = LiveMasterFile.Checksum,
                        ContentPublicationRequest = null,
                        SelectionCriteriaObj = await ContentReductionHierarchy<ReductionFieldValueSelection>.GetFieldSelectionsForSelectionGroupAsync(DbContext, selectionGroupId, selections),
                        ReductionStatus = ReductionStatusEnum.Validating,
                        CreateDateTimeUtc = DateTime.UtcNow,
                        TaskAction = TaskActionEnum.HierarchyAndReduction,
                    };
                    DbContext.ContentReductionTask.Add(contentReductionTask);
                    await DbContext.SaveChangesAsync();

                    string CxnString = ApplicationConfig.GetConnectionString("DefaultConnection");  // key string must match that used in startup.cs

                    Task DontAwaitThisTask = ContentAccessSupport.LongRunningUpdateSelectionCodeAsync(CxnString, LiveMasterFile.FullPath, MasterFileCopyTarget, contentReductionTask, cancellationTokenSource);

                    Log.Information($"In ContentAccessAdminController.UpdateSelections: reduction task {contentReductionTask.Id} submitted with status {contentReductionTask.ReductionStatus.ToString()}.  Background processing will continue.");

                    object ContentTypeConfigObj = null;
                    switch (selectionGroup.RootContentItem.ContentType.TypeEnum)
                    {
                        case ContentTypeEnum.Qlikview:
                            ContentTypeConfigObj = QvConfig;
                            break;

                        case ContentTypeEnum.Html:
                        case ContentTypeEnum.Pdf:
                        case ContentTypeEnum.FileDownload:
                        case ContentTypeEnum.PowerBi:
                        default:
                            // should never get here because non-reducible content types are blocked in validation above
                            break;
                    }
                    string ContentItemRootPath = ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath");
                    ContentAccessSupport.AddReductionMonitor(Task.Run(() => ContentAccessSupport.MonitorReductionTaskForGoLive(NewTaskGuid, CxnString, ContentItemRootPath, ContentTypeConfigObj, cancellationTokenSource.Token), cancellationTokenSource.Token));

                    AuditLogger.Log(AuditEventType.SelectionChangeReductionQueued.ToEvent(selectionGroup, selectionGroup.RootContentItem, selectionGroup.RootContentItem.Client, contentReductionTask), currentUser.UserName, currentUser.Id);
                }
            }

            var model = await _accessAdminQueries.GetUpdateSelectionsModelAsync(selectionGroupId, isMaster, selections.ToList());

            Log.Debug($"ContentAccessAdminController.UpdateSelections: succeeded for selection group {selectionGroupId}");

            return Json(model);
        }

        [NonAction]
        private async Task<IActionResult> CancelReductionAsync(Guid SelectionGroupId)
        {
            SelectionGroup SelectionGroup = await DbContext.SelectionGroup
                                                           .Include(sg => sg.RootContentItem)
                                                               .ThenInclude(c => c.Client)
                                                           .SingleOrDefaultAsync(sg => sg.Id == SelectionGroupId);

            ApplicationUser currentUser = await UserManager.GetUserAsync(User);

            #region Preliminary validation
            if (SelectionGroup == null)
            {
                Log.Warning($"In ContentAccessAdminController.CancelReduction: selection group {SelectionGroupId} not found, aborting");
                Response.Headers.Add("Warning", "The requested selection group does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, SelectionGroup.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Log.Information($"In ContentAccessAdminController.CancelReduction: authorization failure, user {User.Identity.Name}, content item {SelectionGroup.RootContentItemId}, role {RoleEnum.ContentAccessAdmin.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentAccessAdmin), currentUser.UserName, currentUser.Id);
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var CancelableTasks = DbContext.ContentReductionTask
                                           .Where(crt => crt.SelectionGroupId == SelectionGroup.Id)
                                           .Where(crt => ReductionStatusExtensions.cancelableStatusList.Contains(crt.ReductionStatus))
                                           .Where(crt => crt.ContentPublicationRequestId == null);
            if (await CancelableTasks.CountAsync() == 0)
            {
                Log.Warning($"In ContentAccessAdminController.CancelReduction: no cancelable tasks for requested selection group {SelectionGroupId}, aborting");
                Response.Headers.Add("Warning", "There are no cancelable tasks for this content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            // There should always be at most one cancelable task
            var UpdatedTasks = new List<ContentReductionTask>();
            foreach (var Task in CancelableTasks)
            {
                Task.ReductionStatus = ReductionStatusEnum.Canceled;
                UpdatedTasks.Add(Task);
            }
            await DbContext.SaveChangesAsync();

            Log.Information($"In ContentAccessAdminController.CancelReduction: reduction task(s) cancelled: {string.Join(", ", UpdatedTasks.Select(t => t.Id.ToString()))}");
            foreach (var Task in UpdatedTasks)
            {
                AuditLogger.Log(AuditEventType.SelectionChangeReductionCanceled.ToEvent(SelectionGroup, SelectionGroup.RootContentItem, SelectionGroup.RootContentItem.Client, Task), currentUser.UserName, currentUser.Id);
            }

            var model = await _accessAdminQueries.GetCanceledSingleReductionModelAsync(SelectionGroupId);

            return Json(model);
        }
    }
}
