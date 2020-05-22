/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Controller for actions supporting the content publishing page
 * DEVELOPER NOTES:
 */

using AuditLogLib.Event;
using AuditLogLib.Models;
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
using MillimanAccessPortal.Binders;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ContentPublishing;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.Utilities;
using Newtonsoft.Json.Linq;
using PowerBiLib;
using QlikviewLib;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    public class ContentPublishingController : Controller
    {
        private const RoleEnum requiredRole = RoleEnum.ContentPublisher;

        private readonly IAuditLogger AuditLogger;
        private readonly IConfiguration ApplicationConfig;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ApplicationDbContext _dbContext;
        private readonly FileSystemTasks _fileSystemTasks;
        private readonly IGoLiveTaskQueue _goLiveTaskQueue;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PowerBiConfig _powerBiConfig;
        private readonly QlikviewConfig _qlikviewConfig;
        private readonly IPublicationPostProcessingTaskQueue _PostProcessingTaskQueue;
        private readonly ContentPublishingAdminQueries _publishingQueries;

        /// <summary>
        /// Constructor, stores local references to injected service instances
        /// </summary>
        /// <param name="AuditLoggerArg"></param>
        /// <param name="AuthorizationServiceArg"></param>
        /// <param name="ContextArg"></param>
        /// <param name="fileSystemTasks"></param>
        /// <param name="goLiveTaskQueue"></param>
        /// <param name="UserManagerArg"></param>
        /// <param name="ApplicationConfigArg"></param>
        /// <param name="PowerBiOptionsAccessorArg"></param>
        /// <param name="QlikviewOptionsAccessorArg"></param>
        /// <param name="postProcessingTaskQueue"></param>
        /// <param name="publishingQueriesArg"></param>
        public ContentPublishingController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ApplicationDbContext ContextArg,
            FileSystemTasks fileSystemTasks,
            IGoLiveTaskQueue goLiveTaskQueue,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration ApplicationConfigArg,
            IOptions<PowerBiConfig> PowerBiOptionsAccessorArg,
            IOptions<QlikviewConfig> QlikviewOptionsAccessorArg,
            IPublicationPostProcessingTaskQueue postProcessingTaskQueue,
            ContentPublishingAdminQueries publishingQueriesArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            _dbContext = ContextArg;
            _fileSystemTasks = fileSystemTasks;
            _goLiveTaskQueue = goLiveTaskQueue;
            _userManager = UserManagerArg;
            ApplicationConfig = ApplicationConfigArg;
            _powerBiConfig = PowerBiOptionsAccessorArg.Value;
            _qlikviewConfig = QlikviewOptionsAccessorArg.Value;
            _PostProcessingTaskQueue = postProcessingTaskQueue;
            _publishingQueries = publishingQueriesArg;
        }

        /// <summary>
        /// View page in which publication UI is presented
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
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
                Log.Debug($"In ContentPublishingController.PageGlobalData action: authorization failure, user {User.Identity.Name}, role {requiredRole.ToString()}");
                Response.Headers.Add("Warning", "You are not authorized to publish content.");
                return Unauthorized();
            }
            #endregion

            PublishingPageGlobalModel model = await _publishingQueries.BuildPublishingPageGlobalModelAsync();

            return Json(model);
        }
        
        [HttpGet]
        async public Task<IActionResult> Clients()
        {
            Log.Verbose("Entered ContentPublishingController.Clients action");

            #region Authorization
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(requiredRole));
            if (!RoleInClientResult.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.Clients action: authorization failure, user {User.Identity.Name}, role {requiredRole.ToString()}");
                Response.Headers.Add("Warning", "You are not authorized to publish content.");
                return Unauthorized();
            }
            #endregion

            ClientsResponseModel responseModel = new ClientsResponseModel
            {
                Clients = await _publishingQueries.GetAuthorizedClientsModelAsync(await _userManager.GetUserAsync(User)),
            };

            return new JsonResult(responseModel);
        }

        /// <summary>
        /// GET content items for a client authorized to the current user
        /// </summary>
        /// <param name="clientId">Client to which content items must belong</param>
        [HttpGet]
        public async Task<IActionResult> ContentItems([EmitBeforeAfterLog] Guid clientId)
        {
            Log.Verbose($"Entered ContentPublishingController.ContentItems action with client id {clientId}");
            Client client = await _dbContext.Client.FindAsync(clientId);

            #region Preliminary validation
            if (client == null)
            {
                Log.Debug($"In ContentPublishingController.ContentItems action: client {clientId} not found, aborting");
                Response.Headers.Add("Warning", "The requested client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            var roleResult = await AuthorizationService
                .AuthorizeAsync(User, null, new RoleInClientRequirement(requiredRole, clientId));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to publish for this client.");
                return Unauthorized();
            }
            #endregion

            var currentUser = await _userManager.GetUserAsync(User);
            var contentItems = await _publishingQueries.BuildRootContentItemsModelAsync(client, currentUser);

            return Json(contentItems);
        }

        [HttpGet]
        public async Task<IActionResult> RootContentItemDetail(Guid rootContentItemId)
        {
            Log.Verbose($"Entered ContentPublishingController.RootContentItemDetail action with root content item id {rootContentItemId}");

            RootContentItem rootContentItem = await _dbContext.RootContentItem.FindAsync(rootContentItemId);

            #region Preliminary validation
            if (rootContentItem == null)
            {
                Log.Debug($"In ContentPublishingController.RootContentItemDetail action: content item not found, aborting");
                Response.Headers.Add("Warning", "The requested content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInClientResult = await AuthorizationService.AuthorizeAsync(
                User, null, new RoleInRootContentItemRequirement(requiredRole, rootContentItemId));
            if (!roleInClientResult.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.RootContentItemDetail action: authorization failure, user {User.Identity.Name}, root content item {rootContentItemId}, role {requiredRole.ToString()}, aborting");
                Response.Headers.Add("Warning", "You are not authorized to publish content to the specified content item.");
                return Unauthorized();
            }
            #endregion

            //RootContentItemDetail model = Models.ContentPublishing.RootContentItemDetail.Build(_dbContext, rootContentItem);
            RootContentItemDetail model = await _publishingQueries.BuildContentItemDetailModelAsync(rootContentItem, Request);

            return Json(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRootContentItem([FromBody] JObject rootContentItemJobject)
        {
            RootContentItem rootContentItem = await JsonToRootContentItemAsync(rootContentItemJobject);
            if (rootContentItem == null)
            {
                Log.Information($"In {ControllerContext.ActionDescriptor.DisplayName} action: failed to bind adequate root content item parameter, aborting");
                Response.Headers.Add("Warning", "Error interpreting the requested new content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action with root content item {{@RootContentItem}}", rootContentItem);

            #region Preliminary validation
            var client = await _dbContext.Client.SingleOrDefaultAsync(c => c.Id == rootContentItem.ClientId);
            if (client == null)
            {
                Log.Information($"In {ControllerContext.ActionDescriptor.DisplayName} action: client {rootContentItem.ClientId} not found, aborting");
                Response.Headers.Add("Warning", "The associated client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(requiredRole, rootContentItem.ClientId));
            if (!roleInClientResult.Succeeded)
            {
                Log.Information($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failure, user {User.Identity.Name}, client {rootContentItem.ClientId}, role {requiredRole.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(requiredRole));
                Response.Headers.Add("Warning", "You are not authorized to create content items for the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            if (!await _dbContext.ContentType.AnyAsync(c => c.Id == rootContentItem.ContentTypeId))
            {
                Log.Information($"In {ControllerContext.ActionDescriptor.DisplayName} action: content type for content item {rootContentItem.Id} not found, aborting");
                Response.Headers.Add("Warning", "The associated content type does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (!await _dbContext.Client.AnyAsync(c => c.Id == rootContentItem.ClientId))
            {
                Log.Information($"In {ControllerContext.ActionDescriptor.DisplayName} action: client for content item {rootContentItem.Id} not found, aborting");
                Response.Headers.Add("Warning", "The associated client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (string.IsNullOrWhiteSpace(rootContentItem.ContentName))
            {
                Log.Information($"In {ControllerContext.ActionDescriptor.DisplayName} action: content name is required and not provided, aborting");
                Response.Headers.Add("Warning", "You must supply a name for the content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            using (IDbContextTransaction DbTransaction = await _dbContext.Database.BeginTransactionAsync())
            {
                await _dbContext.ContentType.Where(ct => ct.Id == rootContentItem.ContentTypeId).LoadAsync();
                // Commit the new root content item
                _dbContext.RootContentItem.Add(rootContentItem);
                await _dbContext.SaveChangesAsync();

                // Copy user roles for the new root content item from its client.
                // In the future, root content item management and publishing roles may
                // be separated in which case this automatic role copy should be removed.
                List<RoleEnum> RolesToInheritFromClient = new List<RoleEnum> { RoleEnum.ContentAccessAdmin, requiredRole };

                foreach (RoleEnum role in RolesToInheritFromClient)
                {
                    var inheritedRoles = _dbContext.UserRoleInClient
                        .Where(r => r.ClientId == rootContentItem.ClientId)
                        .Where(r => r.Role.RoleEnum == role)
                        .Select(r => new UserRoleInRootContentItem
                        {
                            UserId = r.UserId,
                            RootContentItemId = rootContentItem.Id,
                            RoleId = ApplicationRole.RoleIds[role],
                        });
                    _dbContext.UserRoleInRootContentItem.AddRange(inheritedRoles);
                }
                await _dbContext.SaveChangesAsync();
                await DbTransaction.CommitAsync();
            }

            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: success");
            AuditLogger.Log(AuditEventType.RootContentItemCreated.ToEvent(rootContentItem, client));

            RootContentItemSummary summary = await RootContentItemSummary.BuildAsync(_dbContext, rootContentItem);
            RootContentItemDetail detail = await _publishingQueries.BuildContentItemDetailModelAsync(rootContentItem, Request);

            return Json(new { summary, detail });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRootContentItem([FromBody] JObject rootContentItemJobject)
        {
            RootContentItem rootContentItem = await JsonToRootContentItemAsync(rootContentItemJobject);
            if (rootContentItem == null)
            {
                Log.Information($"In {ControllerContext.ActionDescriptor.DisplayName} action: failed to bind adequate root content item parameter, aborting");
                Response.Headers.Add("Warning", "Error interpreting the requested new content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            Log.Verbose($"Entered ContentPublishingController.UpdateRootContentItem action with root content item {{@RootContentItem}}", rootContentItem);

            #region Preliminary validation
            var currentRootContentItem = await _dbContext.RootContentItem
                                                         .Include(c => c.ContentType)
                                                         .SingleOrDefaultAsync(i => i.Id == rootContentItem.Id);
            if (currentRootContentItem == null)
            {
                Log.Debug($"In ContentPublishingController.UpdateRootContentItem action: content item {rootContentItem.Id} not found, aborting");
                Response.Headers.Add("Warning", "The specified content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, rootContentItem.Id));
            if (!roleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.UpdateRootContentItem action: authorization failure, user {User.Identity.Name}, content item {rootContentItem.Id}, role {requiredRole.ToString()}");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(requiredRole));
                Response.Headers.Add("Warning", "You are not authorized to update this content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            if (currentRootContentItem.ContentTypeId != rootContentItem.ContentTypeId)
            {
                Log.Debug($"In ContentPublishingController.UpdateRootContentItem action: change of content type was requested, aborting");
                Response.Headers.Add("Warning", "The content type can not be modified.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (currentRootContentItem.ContentType == null)
            {
                Log.Debug($"In ContentPublishingController.UpdateRootContentItem action: content type not found for content item {rootContentItem.Id}, aborting");
                Response.Headers.Add("Warning", "The associated content type does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (rootContentItem.ContentName == null)
            {
                Log.Debug($"In ContentPublishingController.UpdateRootContentItem action: content name is required but not provided, aborting");
                Response.Headers.Add("Warning", "You must supply a name for the content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            // Update the current item so that it can be updated
            currentRootContentItem.ContentName = rootContentItem.ContentName;
            currentRootContentItem.Description = rootContentItem.Description;
            currentRootContentItem.Notes = rootContentItem.Notes;
            switch (currentRootContentItem.ContentType.TypeEnum)
            {
                case ContentTypeEnum.PowerBi:
                    rootContentItem.ContentType = await _dbContext.ContentType.FindAsync(rootContentItem.ContentTypeId);
                    PowerBiContentItemProperties newProps = rootContentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;
                    PowerBiContentItemProperties currentProps = currentRootContentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;

                    currentProps.FilterPaneEnabled = newProps.FilterPaneEnabled;
                    currentProps.NavigationPaneEnabled = newProps.NavigationPaneEnabled;
                    currentProps.BookmarksPaneEnabled = newProps.BookmarksPaneEnabled;

                    currentRootContentItem.TypeSpecificDetailObject = currentProps;
                    break;
            }

            List<UserInSelectionGroup> usersInGroup = null;
            if (currentRootContentItem.ContentDisclaimer != rootContentItem.ContentDisclaimer)
            {
                // Reset disclaimer acceptance
                usersInGroup = await _dbContext.UserInSelectionGroup
                                               .Include(usg => usg.User)
                                               .Include(usg => usg.SelectionGroup)
                                               .Where(u => u.SelectionGroup.RootContentItemId == currentRootContentItem.Id)
                                               .ToListAsync();
                usersInGroup.ForEach(u => u.DisclaimerAccepted = false);
            }
            currentRootContentItem.ContentDisclaimer = rootContentItem.ContentDisclaimer;

            await _dbContext.SaveChangesAsync();

            var logClient = await _dbContext.Client.FindAsync(rootContentItem.ClientId);

            Log.Verbose($"In ContentPublishingController.UpdateRootContentItem action: success");
            AuditLogger.Log(AuditEventType.RootContentItemUpdated.ToEvent(currentRootContentItem, logClient));
            if (usersInGroup != null)
            {
                AuditLogger.Log(AuditEventType.ContentDisclaimerAcceptanceReset
                    .ToEvent(usersInGroup, currentRootContentItem, logClient, ContentDisclaimerResetReason.DisclaimerTextModified));
            }

            RootContentItemSummary summary = await RootContentItemSummary.BuildAsync(_dbContext, currentRootContentItem);
            RootContentItemDetail detail = await _publishingQueries.BuildContentItemDetailModelAsync(currentRootContentItem, Request);

            return Json(new { summary, detail });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRootContentItem([FromBody] Guid rootContentItemId)
        {
            Log.Verbose($"Entered ContentPublishingController.DeleteRootContentItem action with root content item id {rootContentItemId} and password");

            var rootContentItem = await  _dbContext.RootContentItem
                                                   .Include(x => x.Client)
                                                   .Include(x => x.ContentType)
                                                   .SingleOrDefaultAsync(x => x.Id == rootContentItemId);

            #region Preliminary Validation
            if (rootContentItem == null)
            {
                Log.Debug($"In ContentPublishingController.DeleteRootContentItem action: content item {rootContentItemId} not found, aborting");
                Response.Headers.Add("Warning", "The requested content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, rootContentItem.Id));
            if (!roleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.DeleteRootContentItem action: authorization failure, user {User.Identity.Name}, content item {rootContentItemId}, role {requiredRole.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(requiredRole));
                Response.Headers.Add("Warning", "You are not authorized to administer the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var blocked = await _dbContext.ContentPublicationRequest
                                          .Where(r => r.RootContentItemId == rootContentItemId)
                                          .AnyAsync(r => PublicationStatusExtensions.ActiveStatuses.Contains(r.RequestStatus));
            if (blocked)
            {
                Log.Debug($"In ContentPublishingController.DeleteRootContentItem action: the operation is blocked due to pending publication, aborting");
                Response.Headers.Add("Warning", "The specified content item cannot be deleted at this time.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            RootContentItemDetail model = await _publishingQueries.BuildContentItemDetailModelAsync(rootContentItem, Request);

            _dbContext.RootContentItem.Remove(rootContentItem);
            await _dbContext.SaveChangesAsync();

            // ContentType specific handling after database operation completes
            switch (rootContentItem.ContentType.TypeEnum)
            {
                case ContentTypeEnum.Qlikview:
                    string ContentFolderFullPath = Path.Combine(ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath"), rootContentItem.Id.ToString());

                    if (Directory.Exists(ContentFolderFullPath))  // unlikely but it might not exist if e.g. nothing was ever published
                    {
                        List<string> AllQvwFiles = Directory.GetFiles(ContentFolderFullPath, "*.qvw").ToList();
                        AllQvwFiles.ForEach(async f =>
                        {
                            string FileFullPath = Path.Combine(ContentFolderFullPath, f);
                            string FileRelativePath = Path.GetRelativePath(ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath"), FileFullPath);
                            await new QlikviewLibApi(_qlikviewConfig).ReclaimAllDocCalsForFile(FileRelativePath);
                        });
                    }
                    break;

                case ContentTypeEnum.PowerBi:
                    PowerBiContentItemProperties props = rootContentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;

                    PowerBiLibApi powerBiApi = await new PowerBiLibApi(_powerBiConfig).InitializeAsync();
                    await powerBiApi.DeleteReportAsync(props.LiveReportId);
                    break;

                case ContentTypeEnum.Html:
                case ContentTypeEnum.Pdf:
                case ContentTypeEnum.FileDownload:
                default:
                    // no content specific action
                    break;
            }

            string ContentFolderPath = Path.Combine(ApplicationConfig.GetSection("Storage")["ContentItemRootPath"], rootContentItem.Id.ToString());
            try
            {
                Directory.Delete(ContentFolderPath, true);
            }
            catch (DirectoryNotFoundException)
            {
                // The root content item doesn't have any publications, this is fine so continue
            }
            catch (Exception e)
            {
                if (! (new StackTrace()).GetFrames().Any(f => f.GetMethod().DeclaringType.Namespace == "MapTests"))
                {
                    Log.Debug($"In ContentPublishingController.DeleteRootContentItem action: error while deleting folder for content {rootContentItem.Id}, aborting");
                    throw new ApplicationException($"Failed to delete content folder {ContentFolderPath}", e);
                }
            }

            Log.Verbose($"In ContentPublishingController.DeleteRootContentItem action: success, aborting");
            AuditLogger.Log(AuditEventType.RootContentItemDeleted.ToEvent(rootContentItem, rootContentItem.Client));

            return Json(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish([FromBody] PublishRequest request)
        {
            Log.Verbose($"Entered ContentPublishingController.Publish action with {{@PublishRequest}}", request);

            ApplicationUser currentApplicationUser = await _userManager.GetUserAsync(User);

            #region Preliminary Validation
            if (currentApplicationUser == null)
            {
                Log.Error($"In ContentPublishingController.Publish action: Current user {User.Identity.Name} not found, aborting");
                Response.Headers.Add("Warning", "Your user identity is unknown.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, request.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.Publish action: authorization failure, user {currentApplicationUser.UserName}, content item {request.RootContentItemId}, role {requiredRole}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(requiredRole));
                Response.Headers.Add("Warning", $"You are not authorized to publish this content");
                return Unauthorized();
            }

            #endregion

            RootContentItem ContentItem = await _dbContext.RootContentItem
                                                          .Include(rc => rc.ContentType)
                                                          .Include(rc => rc.Client)
                                                          .SingleOrDefaultAsync(rc => rc.Id == request.RootContentItemId);

            #region Validation
            // The requested RootContentItem must exist
            if (ContentItem == null)
            {
                Log.Debug($"In ContentPublishingController.Publish action: content item {request.RootContentItemId} not found, aborting");
                Response.Headers.Add("Warning", "Requested content item not found.");
                return BadRequest();
            }

            // All the provided references to related files must be found in the FileUpload entity.  
            if (request.NewRelatedFiles.Any(f => _dbContext.FileUpload.Count(fu => fu.Id == f.FileUploadId) != 1))
            {
                Log.Debug($"In ContentPublishingController.Publish action: one or more new files to be published not found in FileUpload table, aborting");
                Response.Headers.Add("Warning", "A specified uploaded file was not found.");
                return BadRequest();
            }

            bool Blocked;

            // There must be no unresolved ContentPublicationRequest.
            Blocked = await _dbContext.ContentPublicationRequest
                                      .Where(r => r.RootContentItemId == request.RootContentItemId)
                                      .AnyAsync(r => PublicationStatusExtensions.ActiveStatuses.Contains(r.RequestStatus));
            if (Blocked)
            {
                Log.Debug($"In ContentPublishingController.Publish action: blocked due to unresolved ContentPublicationRequest for content item {request.RootContentItemId}, aborting");
                Response.Headers.Add("Warning", "A previous publication is pending for this content.");
                return BadRequest();
            }

            // There must be no unresolved ContentReductionTask.
            Blocked = await _dbContext.ContentReductionTask
                                      .Where(t => t.ContentPublicationRequestId == null)
                                      .Where(t => t.SelectionGroup.RootContentItemId == request.RootContentItemId)
                                      .AnyAsync(t => ReductionStatusExtensions.activeStatusList.Contains(t.ReductionStatus));
            if (Blocked)
            {
                Log.Debug($"In ContentPublishingController.Publish action: blocked due to unresolved ContentReductionTask for content item {request.RootContentItemId}, aborting");
                Response.Headers.Add("Warning", "A previous reduction task is pending for this content.");
                return BadRequest();
            }

            // There must be new files or files to delete
            if (!request.NewRelatedFiles.Any() && !request.DeleteFilePurposes.Any())
            {
                Log.Debug($"In ContentPublishingController.Publish action: no files provided, aborting");
                Response.Headers.Add("Warning", "No files provided.");
                return BadRequest();
            }

            // There must be a master content file either in this request or from previous go-live
            if (!ContentItem.ContentFilesList.Any(f => f.FilePurpose.ToLower() == "mastercontent") && !request.NewRelatedFiles.Any(f => f.FilePurpose.ToLower() == "mastercontent"))
            {
                Log.Debug($"In ContentPublishingController.Publish action: content item has no master file and publication request does not contain one, aborting");
                Response.Headers.Add("Warning", "New publications must include a master content file");
                return BadRequest();
            }
            #endregion

            if (request.DeleteFilePurposes.Any())
            {
                var filesToDelete = ContentItem.ContentFilesList
                    .Where(f => request.DeleteFilePurposes.Contains(f.FilePurpose)).ToList();

                _fileSystemTasks.DeleteRelatedFiles(ContentItem, filesToDelete);

                await _dbContext.SaveChangesAsync();
            }

            if (request.NewRelatedFiles.Any())
            {
                // Insert the initial publication request (not queued yet)
                ContentPublicationRequest NewContentPublicationRequest = new ContentPublicationRequest
                {
                    ApplicationUserId = currentApplicationUser.Id,
                    RequestStatus = PublicationStatus.Validating,
                    CreateDateTimeUtc = DateTime.UtcNow,
                    RootContentItemId = ContentItem.Id,
                    LiveReadyFilesObj = new List<ContentRelatedFile>(),
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles>(),
                    UploadedRelatedFilesObj = request.NewRelatedFiles,
                    RequestedAssociatedFileList = request.AssociatedFiles,
                };
                _dbContext.ContentPublicationRequest.Add(NewContentPublicationRequest);

                try
                {
                    await _dbContext.SaveChangesAsync();
                }
                catch
                {
                    Log.Error($"In ContentPublishingController.Publish action: failed to save database changes, aborting");
                    Response.Headers.Add("Warning", "Failed to save database changes");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                string rootPath = ApplicationConfig.GetSection("Storage")["ContentItemRootPath"];
                string exchangePath = ApplicationConfig.GetSection("Storage")["MapPublishingServerExchangePath"];
                string CxnString = ApplicationConfig.GetConnectionString("DefaultConnection");  // key string must match that used in startup.cs
                ContentPublishSupport.AddPublicationMonitor(
                    ContentPublishSupport.MonitorPublicationRequestForQueueingAsync(NewContentPublicationRequest.Id, CxnString, rootPath, exchangePath, _PostProcessingTaskQueue));

                Log.Verbose($"In ContentPublishingController.Publish action: publication request successfully submitted for validation");
                AuditLogger.Log(AuditEventType.PublicationRequestInitiated.ToEvent(ContentItem, ContentItem.Client, NewContentPublicationRequest));
            }

            var rootContentItemDetail = await _publishingQueries.BuildContentItemDetailModelAsync(ContentItem, Request);
            return Json(rootContentItemDetail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelContentPublicationRequest([FromBody] Guid rootContentItemId)
        {
            Log.Verbose($"Entered ContentPublishingController.CancelContentPublicationRequest action with content item {rootContentItemId}");

            var rootContentItem = await _dbContext.RootContentItem
                                                  .Include(c => c.Client)
                                                  .SingleOrDefaultAsync(c => c.Id == rootContentItemId);

            #region Preliminary validation
            if (rootContentItem == null)
            {
                Log.Debug($"In ContentPublishingController.CancelContentPublicationRequest action: content item {rootContentItemId} not found, aborting");
                Response.Headers.Add("Warning", "The specified content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItem = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, rootContentItem.Id));
            if (!roleInRootContentItem.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.CancelContentPublicationRequest action: authorization failure, user {User.Identity.Name}, content item {rootContentItemId}, role {requiredRole.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(requiredRole));
                Response.Headers.Add("Warning", "You are not authorized to cancel content publication requests for this content item.");
                return Unauthorized();
            }
            #endregion

            DateTime onlyCancelRequestCreatedAfter = (await _dbContext.ContentPublicationRequest
                                                                      .Where(r => r.RootContentItemId == rootContentItem.Id)
                                                                      .Where(r => PublicationStatusExtensions.CancelOnlyAfterLastOfStatusList.Contains(r.RequestStatus))
                                                                      .OrderByDescending(r => r.CreateDateTimeUtc)
                                                                      .FirstOrDefaultAsync())
                                                        ?.CreateDateTimeUtc 
                                                        ?? DateTime.MinValue;

            var contentPublicationRequest = await _dbContext.ContentPublicationRequest
                                                            .Where(r => r.RootContentItemId == rootContentItem.Id)
                                                            .Where(r => PublicationStatusExtensions.CancelablePublicationStatusList.Contains(r.RequestStatus))
                                                            .Where(r => r.CreateDateTimeUtc > onlyCancelRequestCreatedAfter)
                                                            .OrderByDescending(r => r.CreateDateTimeUtc)
                                                            .FirstOrDefaultAsync();

            #region Validation
            if (contentPublicationRequest == null)
            {
                Log.Debug($"In ContentPublishingController.CancelContentPublicationRequest action: there is no cancelable publication request for this content item {rootContentItemId}, aborting");
                Response.Headers.Add("Warning", "No cancelable requests for this content item exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            // Delete uploaded files related to the publication request
            List<Guid> possibleUploadIds = contentPublicationRequest.UploadedRelatedFilesObj.Select(u => u.FileUploadId).Union(
                                           contentPublicationRequest.RequestedAssociatedFileList.Select(u => u.Id)).ToList();
            List<FileUpload> uploadRecords = await _dbContext.FileUpload
                                                             .Where(u => possibleUploadIds.Contains(u.Id))
                                                             .ToListAsync();
            foreach (var uploadRecord in uploadRecords)
            {
                _dbContext.FileUpload.Remove(uploadRecord);
                if (System.IO.File.Exists(uploadRecord.StoragePath))
                {
                    System.IO.File.Delete(uploadRecord.StoragePath);
                }
            }

            // Delete reduction related files
            var ReductionRelatedFiles = contentPublicationRequest.ReductionRelatedFilesObj.SelectMany(rrf => rrf.ReducedContentFileList.Select(rcf => rcf.FullPath).Append(rrf.MasterContentFile?.FullPath));
            foreach (string reductionRelatedFile in ReductionRelatedFiles)
            {
                if (reductionRelatedFile == null)
                {
                    continue;
                }

                if (System.IO.File.Exists(reductionRelatedFile))
                {
                    System.IO.File.Delete(reductionRelatedFile);
                }

                string containingFolder = Path.GetDirectoryName(reductionRelatedFile);
                if (Path.GetFullPath(containingFolder).Contains(Path.GetFullPath(ApplicationConfig.GetValue("Storage:MapPublishingServerExchangePath", "zzzz"))) &&
                    Path.GetFullPath(containingFolder) != Path.GetFullPath(ApplicationConfig.GetValue("Storage:MapPublishingServerExchangePath", "zzz")) &&
                    Directory.Exists(containingFolder) &&
                    Directory.EnumerateFileSystemEntries(containingFolder).Count() == 0)
                {
                    Directory.Delete(containingFolder);
                }
            }

            // Delete live ready files (including associated files)
            var LiveReadyFiles = contentPublicationRequest.LiveReadyFilesObj.Select(f => f.FullPath).Union(
                                 contentPublicationRequest.LiveReadyAssociatedFilesList.Select(f => f.FullPath))
                                 .ToList();
            foreach (string fileToDelete in LiveReadyFiles)
            {
                if (System.IO.File.Exists(fileToDelete))
                {
                    System.IO.File.Delete(fileToDelete);
                }
            }

            // Cancel all realted ContentReductionTask records
            List<ContentReductionTask> relatedTasks = await _dbContext.ContentReductionTask
                                                                      .Where(t => t.ContentPublicationRequestId == contentPublicationRequest.Id)
                                                                      .ToListAsync();
            relatedTasks.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Canceled);

            contentPublicationRequest.RequestStatus = PublicationStatus.Canceled;
            contentPublicationRequest.UploadedRelatedFilesObj = null;
            contentPublicationRequest.RequestedAssociatedFileList = null;
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch
            {
                Log.Error($"In ContentPublishingController.CancelContentPublicationRequest action: failed to save cancelation of publication request for content item {rootContentItemId}, processing may have started, aborting");
                Response.Headers.Add("Warning", "The publication request failed to be canceled.  Processing may have started.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            Log.Verbose($"In ContentPublishingController.CancelContentPublicationRequest action: success");
            AuditLogger.Log(AuditEventType.PublicationCanceled.ToEvent(rootContentItem, rootContentItem.Client, contentPublicationRequest));

            var rootContentItemStatusList = await _publishingQueries.SelectCancelContentPublicationRequestAsync(await _userManager.GetUserAsync(User), rootContentItem, Request);

            return new JsonResult(rootContentItemStatusList);
        }

        [HttpGet]
        [PreventAuthRefresh]
        public async Task<IActionResult> Status(Guid clientId)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            var publishingStatusModel = await _publishingQueries.SelectStatusAsync(user, clientId);

            return new JsonResult(publishingStatusModel);
        }

        /// <summary>
        /// Action to return a summary of publication summary information for user confirmation
        /// </summary>
        /// <param name="RootContentItemId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> PreLiveSummary(Guid RootContentItemId)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action with content item {RootContentItemId}");

            #region Authorization
            AuthorizationResult roleInRootContentItem = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, RootContentItemId));
            if (!roleInRootContentItem.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.PreLiveSummary action: authorization failure, user {User.Identity.Name}, content item {RootContentItemId}, role {requiredRole.ToString()}, aborting");
                Response.Headers.Add("Warning", "You are not authorized to view the publication certification summary for this content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            if (!await _dbContext.RootContentItem.AnyAsync(c => c.Id == RootContentItemId))
            {
                Log.Debug($"In ContentPublishingController.PreLiveSummary action: content item {RootContentItemId} not found, aborting");
                Response.Headers.Add("Warning", "The requested content item was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            PreLiveContentValidationSummary ReturnObj = await PreLiveContentValidationSummary.BuildAsync(_dbContext, RootContentItemId, ApplicationConfig, HttpContext);

            Log.Debug($"{ControllerContext.ActionDescriptor.DisplayName} action: success, returning summary {ReturnObj.ValidationSummaryId}");
            AuditLogger.Log(AuditEventType.PreGoLiveSummary.ToEvent((PreLiveContentValidationSummaryLogModel)ReturnObj));

            return new JsonResult(ReturnObj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoLive([FromBody][Bind("RootContentItemId", "PublicationRequestId", "ValidationSummaryId")] GoLiveViewModel goLiveViewModel)
        {
            Log.Verbose(
                "Entered ContentPublishingController.GoLive action with model {@GoLiveViewModel}",
                goLiveViewModel);

            #region Authorization
            AuthorizationResult authorization = await AuthorizationService.AuthorizeAsync(User, null,
                new RoleInRootContentItemRequirement(requiredRole, goLiveViewModel.RootContentItemId));
            if (!authorization.Succeeded)
            {
                Log.Debug(
                    "In ContentPublishingController.GoLive action: authorization failure, " +
                    $"user {User.Identity.Name}, " + 
                    $"content item {goLiveViewModel.RootContentItemId}, " + 
                    $"role {requiredRole.ToString()}, " + 
                    "aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(requiredRole));
                Response.Headers.Add("Warning", "You are not authorized to publish content for this content item.");
                return Unauthorized();
            }
            #endregion

            goLiveViewModel.UserName = User.Identity.Name;

            #region Validation
            var publicationRequest = await _dbContext.ContentPublicationRequest
                                                     .Include(r => r.RootContentItem)
                                                         .ThenInclude(c => c.ContentType)
                                                     .Include(r => r.ApplicationUser)
                                                     .Where(r => r.Id == goLiveViewModel.PublicationRequestId)
                                                     .Where(r => r.RootContentItemId == goLiveViewModel.RootContentItemId)
                                                     .SingleOrDefaultAsync(r => r.RequestStatus == PublicationStatus.Processed);

            if (publicationRequest?.RootContentItem == null || publicationRequest?.ApplicationUser == null)
            {
                Log.Error(
                    "In ContentPublishingController.GoLive action: " +
                    $"publication request {goLiveViewModel.PublicationRequestId} not found, " + 
                    $"or related user {publicationRequest?.ApplicationUserId} not found, " +
                    $"or related content item {publicationRequest?.RootContentItemId} not found");
                Response.Headers.Add("Warning", "Go-Live request references an invalid publication request.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            bool ReductionIsInvolved = publicationRequest.RootContentItem.DoesReduce
                && publicationRequest.LiveReadyFilesObj.Any(f => f.FilePurpose.ToLower() == "mastercontent");

            var LiveHierarchy = new ContentReductionHierarchy<ReductionFieldValue>
            {
                RootContentItemId = publicationRequest.RootContentItemId
            };
            var NewHierarchy = new ContentReductionHierarchy<ReductionFieldValue>
            {
                RootContentItemId = publicationRequest.RootContentItemId
            };

            var RelatedReductionTasks = await _dbContext.ContentReductionTask
                                                        .Include(t => t.SelectionGroup)
                                                            .ThenInclude(g => g.RootContentItem)
                                                                .ThenInclude(c => c.ContentType)
                                                        .Where(t => t.ContentPublicationRequestId == publicationRequest.Id)
                                                        .ToListAsync();

            if (ReductionIsInvolved)
            {
                // For each reducing SelectionGroup related to the RootContentItem:
                var relatedSelectionGroups = _dbContext.SelectionGroup
                    .Where(g => g.RootContentItemId == goLiveViewModel.RootContentItemId)
                    .Where(g => !g.IsMaster);
                foreach (var relatedSelectionGroup in relatedSelectionGroups)
                {
                    ContentReductionTask ThisTask;

                    // RelatedReductionTasks should have one ContentReductionTask related to the SelectionGroup
                    try
                    {
                        ThisTask = RelatedReductionTasks.Single(t => t.SelectionGroupId == relatedSelectionGroup.Id);
                    }
                    catch (InvalidOperationException)
                    {
                        Log.Error($"" +
                            "In ContentPublishingController.GoLive action: " +
                            "expected one reduction task for each non-master selection group, " +
                            $"failed for selection group {relatedSelectionGroup.Id}, " +
                            "aborting");
                        Response.Headers.Add("Warning",
                            $"Expected 1 reduction task related to SelectionGroup {relatedSelectionGroup.Id}, " +
                            "cannot complete this go-live request.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }

                    // This ContentReductionTask must be a reducing task
                    var reducingTaskAction = new List<TaskActionEnum>
                    {
                        TaskActionEnum.ReductionOnly,
                        TaskActionEnum.HierarchyAndReduction,
                    };
                    if (!reducingTaskAction.Contains(ThisTask.TaskAction))
                    {
                        Log.Error($"In ContentPublishingController.GoLive action: " +
                            $"for selection group {relatedSelectionGroup.Id}, " +
                            $"reduction task {ThisTask.Id} " +
                            $"should have action {TaskActionEnum.HierarchyAndReduction.ToString()} " +
                            $"or {TaskActionEnum.ReductionOnly.ToString()} " +
                            $"but is {ThisTask.TaskAction.ToString()}, " +
                            "aborting");
                        Response.Headers.Add("Warning",
                            $"Go live request failed to verify related content reduction task {ThisTask.Id}.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }

                    // The reduced content file identified in the ContentReductionTask must exist
                    // Reductions that will result in inactive selection groups have no result file
                    bool isInactive = string.IsNullOrWhiteSpace(ThisTask.ResultFilePath);
                    if (!isInactive && !System.IO.File.Exists(ThisTask.ResultFilePath))
                    {
                        Log.Error($"In ContentPublishingController.GoLive action: " +
                            $"for selection group {relatedSelectionGroup.Id}, " +
                            $"reduced content file {ThisTask.ResultFilePath} not found, aborting");
                        Response.Headers.Add("Warning",
                            $"Reduced content file {ThisTask.ResultFilePath} does not exist, " +
                            "cannot complete the go-live request.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                }

                LiveHierarchy = ContentReductionHierarchy<ReductionFieldValue>
                    .GetHierarchyForRootContentItem(_dbContext, publicationRequest.RootContentItemId);
                NewHierarchy = RelatedReductionTasks[0].MasterContentHierarchyObj;

                if (LiveHierarchy.Fields.Count != 0)
                {
                    // No change in field list (e.g. names) should occur
                    var liveFieldSet = LiveHierarchy.Fields.Select(f => f.FieldName).ToHashSet();
                    var newFieldSet = NewHierarchy.Fields.Select(f => f.FieldName).ToHashSet();
                    if (!liveFieldSet.SetEquals(newFieldSet))
                    {
                        Log.Error($"In ContentPublishingController.GoLive action: " +
                            "new hierarchy field names are different from live hierarchy, " +
                            "aborting");
                        Response.Headers.Add("Warning",
                            "New hierarchy field list does not match the live hierarchy");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                }
            }
            #endregion

            publicationRequest.RequestStatus = PublicationStatus.Confirming;
            await _dbContext.SaveChangesAsync();

            _goLiveTaskQueue.QueueGoLive(goLiveViewModel);

            return Json(new { publicationRequestId = goLiveViewModel.PublicationRequestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject([FromBody][Bind("RootContentItemId", "PublicationRequestId", "ValidationSummaryId")] GoLiveViewModel goLiveViewModel)
        {
            Log.Verbose($"Entered ContentPublishingController.Reject action with content item {goLiveViewModel.RootContentItemId}, publication request {goLiveViewModel.PublicationRequestId}");

            #region Authorization
            AuthorizationResult authorization = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(requiredRole, goLiveViewModel.RootContentItemId));
            if (!authorization.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.Reject action, authorization failure, user {User.Identity.Name}, content item {goLiveViewModel.RootContentItemId}, role {requiredRole.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(requiredRole));
                Response.Headers.Add("Warning", "You are not authorized to publish content for this content item.");
                return Unauthorized();
            }
            #endregion

            RootContentItem rootContentItem = await _dbContext.RootContentItem
                                                              .Include(c => c.ContentType)
                                                              .Include(c => c.Client)
                                                              .SingleAsync(c => c.Id == goLiveViewModel.RootContentItemId);
            ContentPublicationRequest pubRequest = await _dbContext.ContentPublicationRequest
                                                                   .FindAsync(goLiveViewModel.PublicationRequestId);

            #region Validation
            // the rootContentItem already exists because the authorization check passed above

            if (pubRequest == null || pubRequest.RootContentItemId != goLiveViewModel.RootContentItemId)
            {
                Log.Debug($"In ContentPublishingController.Reject action, publication request {goLiveViewModel.PublicationRequestId} not found, or associated content item, aborting");
                Response.Headers.Add("Warning", "The requested publication request does not exist.");
                return BadRequest();
            }

            if (pubRequest.RequestStatus != PublicationStatus.Processed)
            {
                Log.Debug($"In ContentPublishingController.Reject action, publication request {goLiveViewModel.PublicationRequestId} is not ready to go live, status = {pubRequest.RequestStatus.ToString()}, aborting");
                Response.Headers.Add("Warning", "The specified publication request is not currently processed.");
                return BadRequest();
            }
            #endregion

            // Update status of request and all associated reduction tasks
            using (var Txn = await _dbContext.Database.BeginTransactionAsync())
            {
                pubRequest.RequestStatus = PublicationStatus.Rejected;

                List<ContentReductionTask> RelatedTasks = await _dbContext.ContentReductionTask
                                                                          .Where(t => t.ContentPublicationRequestId == goLiveViewModel.PublicationRequestId)
                                                                          .ToListAsync();
                foreach (ContentReductionTask relatedTask in RelatedTasks)
                {
                    relatedTask.ReductionStatus = ReductionStatusEnum.Rejected;
                }

                string configuredContentRootFolder = ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath");

                // Prepare each pre-live file/resource for delete
                foreach (ContentRelatedFile PreliveFile in pubRequest.LiveReadyFilesObj)
                {
                    // if the filename extension matches any of those stored in the Content item's matching contentType record
                    if (rootContentItem.ContentType.FileExtensions.Any(e => string.Equals($".{e}", Path.GetExtension(PreliveFile.FullPath), StringComparison.OrdinalIgnoreCase)))
                    {
                        switch (rootContentItem.ContentType.TypeEnum)
                        {
                            case ContentTypeEnum.Qlikview:
                                string qvwFileRelativePath = Path.GetRelativePath(configuredContentRootFolder, PreliveFile.FullPath);
                                try
                                {
                                    await new QlikviewLibApi(_qlikviewConfig).ReclaimAllDocCalsForFile(qvwFileRelativePath);
                                }
                                catch (Exception ex)
                                {
                                    Log.Warning(ex, $"Failed to reclaim Qlikview document CAL for file {PreliveFile.FullPath}, relative path {qvwFileRelativePath}");
                                }
                                break;

                            case ContentTypeEnum.PowerBi:
                                PowerBiContentItemProperties props = rootContentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;

                                PowerBiLibApi powerBiApi = await new PowerBiLibApi(_powerBiConfig).InitializeAsync();
                                await powerBiApi.DeleteReportAsync(props.PreviewReportId);

                                props.PreviewEmbedUrl = null;
                                props.PreviewReportId = null;
                                props.PreviewWorkspaceId = null;
                                rootContentItem.TypeSpecificDetailObject = props;
                                break;

                            default:
                                break;
                        }
                    }
                }

                await _dbContext.SaveChangesAsync();
                await Txn.CommitAsync();
            }

            // Delete pre-live folder
            string PreviewFolder = Path.Combine(ApplicationConfig.GetSection("Storage")["ContentItemRootPath"],
                                                goLiveViewModel.RootContentItemId.ToString(),
                                                goLiveViewModel.PublicationRequestId.ToString());
            if (Directory.Exists(PreviewFolder))
            {
                try
                {
                    FileSystemUtil.DeleteDirectoryWithRetry(PreviewFolder, attempts: 4, baseIntervalMs: 2000);  // 4, 2000 is max 20 sec delay
                }
                catch (IOException ex)
                {
                    Log.Error(ex, $"In ContentPublishingController.Reject action, failed to delete publication pre-live folder <{PreviewFolder}>");
                }
            }

            Log.Verbose($"In ContentPublishingController.Reject action, success");
            AuditLogger.Log(AuditEventType.ContentPublicationRejected.ToEvent(rootContentItem, rootContentItem.Client, pubRequest));

            return Json(new { publicationRequestId = goLiveViewModel.PublicationRequestId });
        }

        private async Task<RootContentItem> JsonToRootContentItemAsync(JObject jObject)
        {
            RootContentItem model = default;
            try
            {
                model = jObject.ToObject<RootContentItem>();
                model.ContentType = await _dbContext.ContentType.SingleOrDefaultAsync(ct => ct.Id == model.ContentTypeId);
            }
            catch (Exception ex)
            {
                var x = ex;
                return null;
            }

            // Handle type specific properties, if any
            if (jObject.TryGetValue("TypeSpecificDetailObject", StringComparison.InvariantCultureIgnoreCase, out JToken typeSpecificDetailObjectToken))
            {
                model.TypeSpecificDetailObject = (TypeSpecificContentItemProperties)typeSpecificDetailObjectToken.ToObject(model.TypeSpecificDetailObjectType);
            }

            return model;
        }
    }
}
