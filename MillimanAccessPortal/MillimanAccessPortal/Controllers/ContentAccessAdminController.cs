/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using AuditLogLib.Event;
using AuditLogLib.Services;
using MapCommonLib.ActionFilters;
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
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    [LogVerbose]
    public class ContentAccessAdminController : Controller
    {
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly IConfiguration ApplicationConfig;
        private readonly ApplicationDbContext DbContext;
        private readonly StandardQueries _standardQueries;
        private readonly ContentAccessAdminQueries _queries;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly QlikviewConfig QvConfig;

        public ContentAccessAdminController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ApplicationDbContext DbContextArg,
            StandardQueries QueriesArg,
            ContentAccessAdminQueries queries,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration ApplicationConfigArg,
            IOptions<QlikviewConfig> QvConfigArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DbContext = DbContextArg;
            _standardQueries = QueriesArg;
            _queries = queries;
            UserManager = UserManagerArg;
            ApplicationConfig = ApplicationConfigArg;
            QvConfig = QvConfigArg.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            #region Authorization
            AuthorizationResult roleResult = await AuthorizationService
                .AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Authorizing action {ControllerContext.ActionDescriptor} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access.");
                return Unauthorized();
            }
            #endregion

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            #region Authorization
            var roleResult = await AuthorizationService
                .AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Authorizing action {ControllerContext.ActionDescriptor} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to administer content access.");
                return Unauthorized();
            }
            #endregion

            var currentUser = await _standardQueries.GetCurrentApplicationUser(User);
            var clients = _queries.SelectClients(currentUser);

            return Json(clients);
        }

        [HttpGet]
        public async Task<IActionResult> ContentItems(Guid clientId)
        {
            #region Authorization
            var roleResult = await AuthorizationService
                .AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentAccessAdmin, clientId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Authorizing action {ControllerContext.ActionDescriptor} for user {User.Identity.Name}");
                Response.Headers.Add("Warning",
                    "You are not authorized to administer content access for this client.");
                return Unauthorized();
            }
            #endregion

            var currentUser = await _standardQueries.GetCurrentApplicationUser(User);
            var contentItems = _queries.SelectContentItems(currentUser, clientId);

            return Json(contentItems);
        }

        [HttpGet]
        public async Task<IActionResult> SelectionGroups(Guid itemId)
        {
            #region Authorization
            var roleResult = await AuthorizationService
                .AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, itemId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Authorizing action {ControllerContext.ActionDescriptor} for user {User.Identity.Name}");
                Response.Headers.Add("Warning",
                    "You are not authorized to administer content access for this content item.");
                return Unauthorized();
            }
            #endregion

            var selectionGroups = _queries.SelectSelectionGroups(itemId);

            return Json(selectionGroups);
        }

        [HttpGet]
        public async Task<IActionResult> Selections(Guid groupId)
        {
            Guid itemId = DbContext.SelectionGroup
                .Where(g => g.Id == groupId)
                .Select(g => g.RootContentItemId)
                .SingleOrDefault();

            #region Authorization
            var roleResult = await AuthorizationService
                .AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, itemId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Authorizing action {ControllerContext.ActionDescriptor} for user {User.Identity.Name}");
                Response.Headers.Add("Warning",
                    "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            var selections = _queries.SelectSelections(groupId);

            return Json(selections);
        }

        [HttpGet]
        [PreventAuthRefresh]
        public async Task<IActionResult> Status(Guid clientId, Guid itemId)
        {
            var currentUser = await _standardQueries.GetCurrentApplicationUser(User);
            var status = _queries.SelectStatus(currentUser, clientId, itemId);

            return Json(status);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequestModel model)
        {
            var contentItem = DbContext.RootContentItem
                .Where(i => i.Id == model.ItemId)
                .Include(item => item.Client)
                .Include(item => item.ContentType)
                .SingleOrDefault();

            #region Authorization
            var roleResult = await AuthorizationService
                .AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(
                    RoleEnum.ContentAccessAdmin, model.ItemId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Authorizing action {ControllerContext.ActionDescriptor} for user {User.Identity.Name}");
                Response.Headers.Add("Warning",
                    "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            // reject this request if the RootContentItem has a pending publication request
            bool blockedByPendingPublication = DbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == contentItem.Id)
                .Where(r => r.RequestStatus.IsActive())
                .Any();
            if (blockedByPendingPublication)
            {
                Response.Headers.Add("Warning",
                    "A new selection group may not be created while this content item has a pending publication.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (!contentItem.DoesReduce)
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
                ? _queries.CreateReducingGroup(model.ItemId, model.Name)
                : _queries.CreateMasterGroup(model.ItemId, model.Name);

            return Json(selectionGroups);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGroup([FromBody] UpdateGroupRequestModel model)
        {
            (Guid itemId, Guid clientId) = DbContext.SelectionGroup
                .Where(g => g.Id == model.GroupId)
                .Select(g => g.RootContentItem)
                .ToList()
                .ConvertAll(r => (r.Id, r.ClientId))
                .SingleOrDefault();

            #region Authorization
            var roleResult = await AuthorizationService
                .AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, itemId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Authorizing action {ControllerContext.ActionDescriptor} for user {User.Identity.Name}");
                Response.Headers.Add("Warning",
                    "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var existsCount = DbContext.ApplicationUser
                .Where(u => model.Users.Contains(u.Id))
                .Count();
            if (existsCount < model.Users.Count)
            {
                Response.Headers.Add("Warning", "One or more requested users do not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var hasRoleCount = DbContext.UserRoleInClient
                .Where(r => model.Users.Contains(r.UserId))
                .Where(r => r.ClientId == clientId)
                .Where(r => r.Role.RoleEnum == RoleEnum.ContentUser)
                .Count();
            if (hasRoleCount < model.Users.Count)
            {
                Response.Headers.Add("Warning",
                    "One or more requested users do not have permission to use this content.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var anyInOtherGroup = DbContext.UserInSelectionGroup
                .Where(u => model.Users.Contains(u.UserId))
                .Where(u => u.SelectionGroup.RootContentItemId == itemId)
                .Where(u => u.SelectionGroupId != model.GroupId)
                .Any();
            if (anyInOtherGroup)
            {
                Response.Headers.Add("Warning",
                    "One or more requested users are already in a different selection group.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            var group = _queries.UpdateGroup(model.GroupId, model.Name, model.Users.ToList());

            return Json(group);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendGroup([FromBody] SuspendGroupRequestModel model)
        {
            Guid itemId = DbContext.SelectionGroup
                .Where(g => g.Id == model.GroupId)
                .Select(g => g.RootContentItemId)
                .SingleOrDefault();

            #region Authorization
            var roleResult = await AuthorizationService
                .AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentAccessAdmin, itemId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Authorizing action {ControllerContext.ActionDescriptor} for user {User.Identity.Name}");
                Response.Headers.Add("Warning",
                    "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            var group = _queries.SetGroupSuspended(model.GroupId, model.IsSuspended);

            return Json(group);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroup([FromBody] DeleteGroupRequestModel model)
        {
            var selectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rci => rci.ContentType)
                .SingleOrDefault(sg => sg.Id == model.GroupId);

            #region Authorization
            var roleResult = await AuthorizationService
                .AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(
                    RoleEnum.ContentAccessAdmin, selectionGroup?.RootContentItemId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Authorizing action {ControllerContext.ActionDescriptor} for user {User.Identity.Name}");
                Response.Headers.Add("Warning",
                    "You are not authorized to administer content access to the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            // reject this request if the RootContentItem has a pending publication request
            bool blockedByPendingPublication = DbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == selectionGroup.RootContentItemId)
                .Where(r => r.RequestStatus.IsActive())
                .Any();
            if (blockedByPendingPublication)
            {
                Response.Headers.Add("Warning",
                    "A new selection group may not be created while this content item has a pending publication.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            var selectionGroups = _queries.DeleteGroup(model.GroupId);

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

                case ContentTypeEnum.Html:
                case ContentTypeEnum.Pdf:
                case ContentTypeEnum.FileDownload:
                default:
                    // for all non-reducible content types, do nothing.
                    break;
            }
            #endregion

            return Json(selectionGroups);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSelections([FromBody] UpdateSelectionsRequestModel model)
        {
            return await _updateSelections(model.GroupId, model.IsMaster, model.Selections.ToArray());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReduction([FromBody] CancelReductionRequestModel model)
        {
            return await _cancelReduction(model.GroupId);
        }

        [NonAction]
        private async Task<IActionResult> _updateSelections(Guid selectionGroupId, bool isMaster, Guid[] selections)
        {
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
                Guid NewTaskGuid = Guid.NewGuid();

                if (selections.Count() == 0)
                {
                    // Insert a new reduction task to record the unprocessable condition
                    DbContext.ContentReductionTask.Add(new ContentReductionTask
                    {
                        ApplicationUserId = (await _standardQueries.GetCurrentApplicationUser(User)).Id,
                        CreateDateTimeUtc = DateTime.UtcNow,
                        Id = NewTaskGuid,
                        MasterFilePath = "",  // required field - consider removing non null requirement
                        OutcomeMetadataObj = new ReductionTaskOutcomeMetadata
                        {
                            OutcomeReason = MapDbReductionTaskOutcomeReason.NoSelectedFieldValues,
                            ReductionTaskId = NewTaskGuid,
                        },
                        ReductionStatus = ReductionStatusEnum.Live,
                        ReductionStatusMessage = "In ContentAccessAdminController.UpdateSelections, no selections, reduction task not queued",
                        SelectionGroupId = selectionGroup.Id,
                        TaskAction = TaskActionEnum.Unspecified,
                    });

                    // Update selection group fields
                    selectionGroup.IsMaster = false;
                    selectionGroup.SelectedHierarchyFieldValueList = new Guid[0];
                    selectionGroup.ContentInstanceUrl = null;

                    // set live reduction to replaced
                    var liveReductionTasks = DbContext.ContentReductionTask
                        .Where(t => t.SelectionGroupId == selectionGroup.Id)
                        .Where(t => t.ReductionStatus == ReductionStatusEnum.Live)
                        .ToList();
                    liveReductionTasks.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Replaced);

                    DbContext.SaveChanges();

                    Log.Debug($"In ContentAccessAdminController.UpdateSelections action: request to update selection group {selectionGroup.Id} with no selections, cannot be processed, aborting");
                }
                else
                {
                    // Stage the master file in a task folder in the file exchange share
                    string TaskFolderPath = Path.Combine(ApplicationConfig.GetValue<string>("Storage:MapPublishingServerExchangePath"), NewTaskGuid.ToString("D"));
                    Directory.CreateDirectory(TaskFolderPath);
                    string MasterFileCopyTarget = Path.Combine(TaskFolderPath, Path.GetFileName(LiveMasterFile.FullPath));
                    System.IO.File.Copy(LiveMasterFile.FullPath, MasterFileCopyTarget);

                    var contentReductionTask = new ContentReductionTask
                    {
                        Id = NewTaskGuid,
                        ApplicationUser = await _standardQueries.GetCurrentApplicationUser(User),
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

                        case ContentTypeEnum.Html:
                        case ContentTypeEnum.Pdf:
                        case ContentTypeEnum.FileDownload:
                        default:
                            // should never get here because non-reducible content types are blocked in validation above
                            break;
                    }
                    ContentAccessSupport.AddReductionMonitor(Task.Run(() => ContentAccessSupport.MonitorReductionTaskForGoLive(NewTaskGuid, CxnString, ContentItemRootPath, ContentTypeConfigObj)));

                    AuditLogger.Log(AuditEventType.SelectionChangeReductionQueued.ToEvent(selectionGroup, contentReductionTask));
                }
            }
            SelectionsDetail model = SelectionsDetail.Build(DbContext, _standardQueries, selectionGroup);

            return Json(model);
        }

        [NonAction]
        public async Task<IActionResult> _cancelReduction(Guid SelectionGroupId)
        {
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

            SelectionsDetail Model = SelectionsDetail.Build(DbContext, _standardQueries, SelectionGroup);

            return Json(Model);
        }
    }
}
