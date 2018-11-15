/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Controller for actions supporting the content publishing page
 * DEVELOPER NOTES:
 */

using AuditLogLib;
using AuditLogLib.Services;
using MapCommonLib;
using MapCommonLib.ActionFilters;
using MapCommonLib.ContentTypeSpecific;
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
using MillimanAccessPortal.Models.ContentPublishing;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using AuditLogLib.Event;
using QlikviewLib;

namespace MillimanAccessPortal.Controllers
{
    public class ContentPublishingController : Controller
    {
        private readonly IAuditLogger AuditLogger;
        private readonly IConfiguration ApplicationConfig;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ApplicationDbContext DbContext;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly QlikviewConfig QlikviewConfig;


        /// <summary>
        /// Constructor, stores local references to injected service instances
        /// </summary>
        /// <param name="ContextArg"></param>
        /// <param name="QueriesArg"></param>
        /// <param name="LoggerFactoryArg"></param>
        /// <param name="AuditLoggerArg"></param>
        /// <param name="AuthorizationServiceArg"></param>
        public ContentPublishingController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ApplicationDbContext ContextArg,
            StandardQueries QueriesArg,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration ApplicationConfigArg,
            IOptions<QlikviewConfig> QlikviewOptionsAccessorArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DbContext = ContextArg;
            Queries = QueriesArg;
            UserManager = UserManagerArg;
            ApplicationConfig = ApplicationConfigArg;
            QlikviewConfig = QlikviewOptionsAccessorArg.Value;
        }

        /// <summary>
        /// View page in which publication UI is presented
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AvailableContentTypes()
        {
            var model = DbContext.ContentType.ToList();

            return new JsonResult(model);
        }

        [HttpGet]
        async public Task<IActionResult> Clients()
        {
            Log.Verbose("Entered ContentPublishingController.Clients action");

            #region Authorization
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentPublisher, null));
            if (!RoleInClientResult.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.Clients action: authorization failure, user {User.Identity.Name}, global role {RoleEnum.ContentPublisher.ToString()}");
                Response.Headers.Add("Warning", "You are not authorized to publish content.");
                return Unauthorized();
            }
            #endregion

            var model = await ClientTree.Build(await Queries.GetCurrentApplicationUser(User), UserManager, DbContext, RoleEnum.ContentPublisher);

            return new JsonResult(model);
        }

        [HttpGet]
        public async Task<IActionResult> RootContentItems(Guid clientId)
        {
            Log.Verbose($"Entered ContentPublishingController.RootContentItems action with client id {clientId}");

            Client client = DbContext.Client.Find(clientId);

            #region Preliminary validation
            if (client == null)
            {
                Log.Debug($"In ContentPublishingController.RootContentItems action: client {clientId} not found, aborting");
                Response.Headers.Add("Warning", "The requested client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentPublisher, clientId));
            if (!roleInClientResult.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.RootContentItems action: authorization failure, user {User.Identity.Name}, client {clientId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                Response.Headers.Add("Warning", "You are not authorized to publish content for the specified client.");
                return Unauthorized();
            }
            #endregion

            RootContentItemList model = RootContentItemList.Build(DbContext, client, await Queries.GetCurrentApplicationUser(User), RoleEnum.ContentPublisher);

            return Json(model);
        }

        [HttpGet]
        public async Task<IActionResult> RootContentItemDetail(Guid rootContentItemId)
        {
            Log.Verbose($"Entered ContentPublishingController.RootContentItemDetail action with root content item id {rootContentItemId}");

            RootContentItem rootContentItem = DbContext.RootContentItem.Find(rootContentItemId);

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
                User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, rootContentItemId));
            if (!roleInClientResult.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.RootContentItemDetail action: authorization failure, user {User.Identity.Name}, root content item {rootContentItemId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                Response.Headers.Add("Warning", "You are not authorized to publish content to the specified content item.");
                return Unauthorized();
            }
            #endregion

            RootContentItemDetail model = Models.ContentPublishing.RootContentItemDetail.Build(DbContext, rootContentItem);

            return Json(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRootContentItem(RootContentItem rootContentItem)
        {
            Log.Verbose($"Entered ContentPublishingController.CreateRootContentItem action with root content item {{@RootContentItem}}", rootContentItem);

            #region Preliminary validation
            var client = DbContext.Client
                .Where(c => c.Id == rootContentItem.ClientId)
                .SingleOrDefault();
            if (client == null)
            {
                Log.Debug($"In ContentPublishingController.CreateRootContentItem action: client {rootContentItem.ClientId} not found, aborting");
                Response.Headers.Add("Warning", "The associated client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentPublisher, rootContentItem.ClientId));
            if (!roleInClientResult.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.CreateRootContentItem action: authorization failure, user {User.Identity.Name}, client {rootContentItem.ClientId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", "You are not authorized to create content items for the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var contentType = DbContext.ContentType
                .Where(c => c.Id == rootContentItem.ContentTypeId)
                .SingleOrDefault();
            if (contentType == null)
            {
                Log.Debug($"In ContentPublishingController.CreateRootContentItem action: content type for content item {rootContentItem.Id} not found, aborting");
                Response.Headers.Add("Warning", "The associated content type does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (rootContentItem.ContentName == null)
            {
                Log.Debug($"In ContentPublishingController.CreateRootContentItem action: content name is required and not provided, aborting");
                Response.Headers.Add("Warning", "You must supply a name for the content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            using (IDbContextTransaction DbTransaction = DbContext.Database.BeginTransaction())
            {
                // Commit the new root content item
                DbContext.RootContentItem.Add(rootContentItem);
                DbContext.SaveChanges();

                // Copy user roles for the new root content item from its client.
                // In the future, root content item management and publishing roles may
                // be separated in which case this automatic role copy should be removed.
                List<RoleEnum> RolesToInheritFromClient = new List<RoleEnum> { RoleEnum.ContentAccessAdmin, RoleEnum.ContentPublisher };

                foreach (RoleEnum role in RolesToInheritFromClient)
                {
                    var inheritedRoles = DbContext.UserRoleInClient
                        .Where(r => r.ClientId == rootContentItem.ClientId)
                        .Where(r => r.Role.RoleEnum == role)
                        .Select(r => new UserRoleInRootContentItem
                        {
                            UserId = r.UserId,
                            RootContentItemId = rootContentItem.Id,
                            RoleId = ApplicationRole.RoleIds[role],
                        });
                    DbContext.UserRoleInRootContentItem.AddRange(inheritedRoles);
                }
                DbContext.SaveChanges();
                DbTransaction.Commit();
            }

            Log.Verbose($"In ContentPublishingController.CreateRootContentItem action: success");
            AuditLogger.Log(AuditEventType.RootContentItemCreated.ToEvent(rootContentItem));

            RootContentItemSummary summary = RootContentItemSummary.Build(DbContext, rootContentItem);
            RootContentItemDetail detail = Models.ContentPublishing.RootContentItemDetail.Build(DbContext, rootContentItem);

            return Json(new { summary, detail });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRootContentItem(RootContentItem rootContentItem)
        {
            Log.Verbose($"Entered ContentPublishingController.UpdateRootContentItem action with root content item {{@RootContentItem}}", rootContentItem);

            #region Preliminary validation
            var currentRootContentItem = DbContext.RootContentItem
                .Include(c => c.ContentType)
                .Where(i => i.Id == rootContentItem.Id)
                .SingleOrDefault();
            if (currentRootContentItem == null)
            {
                Log.Debug($"In ContentPublishingController.UpdateRootContentItem action: content item {rootContentItem.Id} not found, aborting");
                Response.Headers.Add("Warning", "The specified content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, rootContentItem.Id));
            if (!roleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.UpdateRootContentItem action: authorization failure, user {User.Identity.Name}, content item {rootContentItem.Id}, role {RoleEnum.ContentPublisher.ToString()}");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", "You are not authorized to update this content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
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
            // See ClientAdminController.cs
            currentRootContentItem.ContentName = rootContentItem.ContentName;
            currentRootContentItem.Description = rootContentItem.Description;
            currentRootContentItem.Notes = rootContentItem.Notes;
            currentRootContentItem.TypeSpecificDetail = rootContentItem.TypeSpecificDetail;

            DbContext.RootContentItem.Update(currentRootContentItem);
            DbContext.SaveChanges();

            Log.Verbose($"In ContentPublishingController.UpdateRootContentItem action: success");
            AuditLogger.Log(AuditEventType.RootContentItemUpdated.ToEvent(rootContentItem));

            RootContentItemSummary summary = RootContentItemSummary.Build(DbContext, currentRootContentItem);
            RootContentItemDetail detail = Models.ContentPublishing.RootContentItemDetail.Build(DbContext, currentRootContentItem);

            return Json(new { summary, detail });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRootContentItem(Guid rootContentItemId, string password)
        {
            Log.Verbose($"Entered ContentPublishingController.DeleteRootContentItem action with root content item id {rootContentItemId} and password");

            var rootContentItem = DbContext.RootContentItem
                .Include(x => x.Client)
                .Include(x => x.ContentType)
                .SingleOrDefault(x => x.Id == rootContentItemId);

            #region Preliminary Validation
            if (!await UserManager.CheckPasswordAsync(await Queries.GetCurrentApplicationUser(User), password))
            {
                Log.Debug($"In ContentPublishingController.DeleteRootContentItem action: user password incorrect, aborting");
                Response.Headers.Add("Warning", "Incorrect password");
                return Unauthorized();
            }

            if (rootContentItem == null)
            {
                Log.Debug($"In ContentPublishingController.DeleteRootContentItem action: content item {rootContentItemId} not found, aborting");
                Response.Headers.Add("Warning", "The requested content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, rootContentItem.Id));
            if (!roleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.DeleteRootContentItem action: authorization failure, user {User.Identity.Name}, content item {rootContentItemId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", "You are not authorized to administer the specified content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var blocked = DbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == rootContentItemId)
                .Any(r => r.RequestStatus.IsActive());
            if (blocked)
            {
                Log.Debug($"In ContentPublishingController.DeleteRootContentItem action: the operation is blocked due to pending publication, aborting");
                Response.Headers.Add("Warning", "The specified content item cannot be deleted at this time.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            RootContentItemDetail model = Models.ContentPublishing.RootContentItemDetail.Build(DbContext, rootContentItem);

            DbContext.RootContentItem.Remove(rootContentItem);
            DbContext.SaveChanges();

            // ContentType specific handling after database operation completes
            switch (rootContentItem.ContentType.TypeEnum)
            {
                case ContentTypeEnum.Qlikview:
                    string ContentFolderFullPath = Path.Combine(ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath"), rootContentItem.Id.ToString());

                    if (Directory.Exists(ContentFolderFullPath))  // unlikely but could happen if nothing was ever published
                    {
                        List<string> AllQvwFiles = Directory.GetFiles(ContentFolderFullPath, "*.qvw").ToList();
                        AllQvwFiles.ForEach(async f =>
                        {
                            string FileFullPath = Path.Combine(ContentFolderFullPath, f);
                            string FileRelativePath = Path.GetRelativePath(ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath"), FileFullPath);
                            await new QlikviewLibApi().ReclaimAllDocCalsForFile(FileRelativePath, QlikviewConfig);
                        });
                    }
                    break;

                default:
                    break;
            }

            try
            {
                string ContentFolderPath = Path.Combine(ApplicationConfig.GetSection("Storage")["ContentItemRootPath"], rootContentItem.Id.ToString());
                Directory.Delete(ContentFolderPath, true);
            }
            catch (DirectoryNotFoundException)
            {
                // The root content item doesn't have any publications, this is fine so continue
            }
            catch
            {
                if (! (new StackTrace()).GetFrames().Any(f => f.GetMethod().DeclaringType.Namespace == "MapTests"))
                {
                    Log.Debug($"In ContentPublishingController.DeleteRootContentItem action: error while deleting folder for content {rootContentItem.Id}, aborting");
                    throw;  // maybe this does not have to be done
                }
            }

            Log.Verbose($"In ContentPublishingController.DeleteRootContentItem action: success, aborting");
            AuditLogger.Log(AuditEventType.RootContentItemDeleted.ToEvent(rootContentItem));

            return Json(model);
        }

        [HttpPost]
        public async Task<IActionResult> Publish(PublishRequest Arg)
        {
            Log.Verbose($"Entered ContentPublishingController.Publish action with {{@PublishRequest}}", Arg);

            ApplicationUser currentApplicationUser = await Queries.GetCurrentApplicationUser(User);

            #region Preliminary Validation
            if (currentApplicationUser == null)
            {
                Log.Error($"In ContentPublishingController.Publish action: Current user {User.Identity.Name} not found, aborting");
                Response.Headers.Add("Warning", "Your user identity is unknown.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, Arg.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.Publish action: authorization failure, user {currentApplicationUser.UserName}, content item {Arg.RootContentItemId}, role {RoleEnum.ContentPublisher}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", $"You are not authorized to publish this content");
                return Unauthorized();
            }

            #endregion

            RootContentItem ContentItem = DbContext.RootContentItem
                                                   .Include(rc => rc.ContentType)
                                                   .SingleOrDefault(rc => rc.Id == Arg.RootContentItemId);

            #region Validation
            // The requested RootContentItem must exist
            if (ContentItem == null)
            {
                Log.Debug($"In ContentPublishingController.Publish action: content item {Arg.RootContentItemId} not found, aborting");
                Response.Headers.Add("Warning", "Requested content item not found.");
                return BadRequest();
            }

            // All the provided references to related files must be found in the FileUpload entity.  
            if (Arg.RelatedFiles.Any(f => DbContext.FileUpload.Count(fu => fu.Id == f.FileUploadId) != 1))
            {
                Log.Debug($"In ContentPublishingController.Publish action: one or more new files to be published not found in FileUpload table, aborting");
                Response.Headers.Add("Warning", "A specified uploaded file was not found.");
                return BadRequest();
            }

            bool Blocked;

            // There must be no unresolved ContentPublicationRequest.
            Blocked = DbContext.ContentPublicationRequest
                               .Where(r => r.RootContentItemId == Arg.RootContentItemId)
                               .Any(r => r.RequestStatus.IsActive());
            if (Blocked)
            {
                Log.Debug($"In ContentPublishingController.Publish action: blocked due to unresolved ContentPublicationRequest for content item {Arg.RootContentItemId}, aborting");
                Response.Headers.Add("Warning", "A previous publication is pending for this content.");
                return BadRequest();
            }

            // There must be no unresolved ContentReductionTask.
            Blocked = DbContext.ContentReductionTask
                               .Where(t => t.ContentPublicationRequestId == null)
                               .Where(t => t.SelectionGroup.RootContentItemId == Arg.RootContentItemId)
                               .Any(t => t.ReductionStatus.IsActive());
            if (Blocked)
            {
                Log.Debug($"In ContentPublishingController.Publish action: blocked due to unresolved ContentReductionTask for content item {Arg.RootContentItemId}, aborting");
                Response.Headers.Add("Warning", "A previous reduction task is pending for this content.");
                return BadRequest();
            }

            // There must be a master content file either in this request or from previous go-live
            if (!ContentItem.ContentFilesList.Any(f => f.FilePurpose.ToLower() == "mastercontent") && !Arg.RelatedFiles.Any(f => f.FilePurpose.ToLower() == "mastercontent"))
            {
                Log.Debug($"In ContentPublishingController.Publish action: content item has no master file and publication request does not contain one, aborting");
                Response.Headers.Add("Warning", "New publications must include a master content file");
                return BadRequest();
            }
            #endregion

            // Insert the initial publication request (not queued yet)
            ContentPublicationRequest NewContentPublicationRequest = new ContentPublicationRequest
            {
                ApplicationUserId = currentApplicationUser.Id,
                RequestStatus = PublicationStatus.Validating,
                CreateDateTimeUtc = DateTime.UtcNow,
                RootContentItemId = ContentItem.Id,
                LiveReadyFilesObj = new List<ContentRelatedFile>(),
                ReductionRelatedFilesObj = new List<ReductionRelatedFiles>(),
                UploadedRelatedFilesObj = Arg.RelatedFiles.ToList(),
            };
            try
            {
                DbContext.ContentPublicationRequest.Add(NewContentPublicationRequest);
                DbContext.SaveChanges();
            }
            catch
            {
                Log.Error($"In ContentPublishingController.Publish action: failed to store publication request, aborting");
                Response.Headers.Add("Warning", "Failed to store publication request");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            string rootPath = ApplicationConfig.GetSection("Storage")["ContentItemRootPath"];
            string exchangePath = ApplicationConfig.GetSection("Storage")["MapPublishingServerExchangePath"];
            string CxnString = ApplicationConfig.GetConnectionString("DefaultConnection");  // key string must match that used in startup.cs
            ContentPublishSupport.AddPublicationMonitor(Task.Run(() =>
                ContentPublishSupport.MonitorPublicationRequestForQueueing(NewContentPublicationRequest.Id, CxnString, rootPath, exchangePath)));

            Log.Verbose($"In ContentPublishingController.Publish action: publication request queued successfully");
            AuditLogger.Log(AuditEventType.PublicationRequestInitiated.ToEvent(NewContentPublicationRequest.RootContentItem, NewContentPublicationRequest));

            var rootContentItemDetail = Models.ContentPublishing.RootContentItemDetail.Build(DbContext, ContentItem);
            return Json(rootContentItemDetail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelContentPublicationRequest(Guid rootContentItemId)
        {
            Log.Verbose($"Entered ContentPublishingController.CancelContentPublicationRequest action with content item {rootContentItemId}");

            #region Preliminary validation
            var rootContentItem = DbContext.RootContentItem.Find(rootContentItemId);
            if (rootContentItem == null)
            {
                Log.Debug($"In ContentPublishingController.CancelContentPublicationRequest action: content item {rootContentItemId} not found, aborting");
                Response.Headers.Add("Warning", "The specified content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItem = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, rootContentItem.Id));
            if (!roleInRootContentItem.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.CancelContentPublicationRequest action: authorization failure, user {User.Identity.Name}, content item {rootContentItemId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", "You are not authorized to cancel content publication requests for this content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var contentPublicationRequest = DbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == rootContentItem.Id)
                .Where(r => r.RequestStatus.IsCancelable())
                .SingleOrDefault();
            if (contentPublicationRequest == null)
            {
                Log.Debug($"In ContentPublishingController.CancelContentPublicationRequest action: there is no cancelable publication request for this content item {rootContentItemId}, aborting");
                Response.Headers.Add("Warning", "No cancelable requests for this content item exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            contentPublicationRequest.RequestStatus = PublicationStatus.Canceled;
            contentPublicationRequest.UploadedRelatedFilesObj = null;
            DbContext.ContentPublicationRequest.Update(contentPublicationRequest);
            try
            {
                DbContext.SaveChanges();
            }
            catch
            {
                Log.Error($"In ContentPublishingController.CancelContentPublicationRequest action: failed to save cancelation of publication request for content item {rootContentItemId}, processing may have started, aborting");
                Response.Headers.Add("Warning", "The publication request failed to be canceled.  Processing may have started.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            Log.Verbose($"In ContentPublishingController.CancelContentPublicationRequest action: success");

            var rootContentItemStatusList = RootContentItemStatus.Build(DbContext, await Queries.GetCurrentApplicationUser(User));

            return new JsonResult(rootContentItemStatusList);
        }

        [HttpGet]
        [PreventAuthRefresh]
        public async Task<IActionResult> Status()
        {
            var rootContentItemStatusList = RootContentItemStatus.Build(DbContext, await Queries.GetCurrentApplicationUser(User));

            return new JsonResult(rootContentItemStatusList);
        }

        /// <summary>
        /// Action to return a summary of publication summary information for user confirmation
        /// </summary>
        /// <param name="RootContentItemId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> PreLiveSummary(Guid RootContentItemId)
        {
            Log.Verbose($"Entered ContentPublishingController.PreLiveSummary action with content item {RootContentItemId}");

            #region Authorization
            AuthorizationResult roleInRootContentItem = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, RootContentItemId));
            if (!roleInRootContentItem.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.PreLiveSummary action: authorization failure, user {User.Identity.Name}, content item {RootContentItemId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                Response.Headers.Add("Warning", "You are not authorized to view the publication certification summary for this content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            if (!DbContext.RootContentItem.Any(c => c.Id == RootContentItemId))
            {
                Log.Debug($"In ContentPublishingController.PreLiveSummary action: content item {RootContentItemId} not found, aborting");
                Response.Headers.Add("Warning", "The requested content item was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            PreLiveContentValidationSummary ReturnObj = await PreLiveContentValidationSummary.Build(DbContext, RootContentItemId, ApplicationConfig, HttpContext, QlikviewConfig);

            Log.Verbose($"In ContentPublishingController.PreLiveSummary action: success, returning summary {ReturnObj.ValidationSummaryId}");
            var preGoLiveSummaryLog = new
            {
                ReturnObj.ValidationSummaryId,
                ReturnObj.PublicationRequestId,
                ReturnObj.AttestationLanguage,
                ReturnObj.ContentDescription,
                ReturnObj.RootContentName,
                ReturnObj.ContentTypeName,
                ReturnObj.LiveHierarchy,
                ReturnObj.NewHierarchy,
                ReturnObj.DoesReduce,
                ReturnObj.ClientName,
            };
            AuditLogger.Log(AuditEventType.PreGoLiveSummary.ToEvent(preGoLiveSummaryLog));

            return new JsonResult(ReturnObj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoLive(GoLiveViewModel goLiveViewModel)
        {
            Log.Verbose(
                "Entered ContentPublishingController.GoLive action with model {@GoLiveViewModel}",
                goLiveViewModel);

            #region Authorization
            AuthorizationResult authorization = await AuthorizationService.AuthorizeAsync(User, null,
                new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, goLiveViewModel.RootContentItemId));
            if (!authorization.Succeeded)
            {
                Log.Debug(
                    "In ContentPublishingController.GoLive action: authorization failure, " +
                    $"user {User.Identity.Name}, " + 
                    $"content item {goLiveViewModel.RootContentItemId}, " + 
                    $"role {RoleEnum.ContentPublisher.ToString()}, " + 
                    "aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", "You are not authorized to publish content for this content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var publicationRequest = DbContext.ContentPublicationRequest
                .Include(r => r.RootContentItem)
                    .ThenInclude(c => c.ContentType)
                .Include(r => r.ApplicationUser)
                .Where(r => r.Id == goLiveViewModel.PublicationRequestId)
                .Where(r => r.RootContentItemId == goLiveViewModel.PublicationRequestId)
                .SingleOrDefault(r => r.RequestStatus == PublicationStatus.Processed);

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

            var RelatedReductionTasks = DbContext.ContentReductionTask
                .Include(t => t.SelectionGroup)
                    .ThenInclude(g => g.RootContentItem)
                        .ThenInclude(c => c.ContentType)
                .Where(t => t.ContentPublicationRequestId == publicationRequest.Id)
                .ToList();

            if (ReductionIsInvolved)
            {
                // For each reducing SelectionGroup related to the RootContentItem:
                var relatedSelectionGroups = DbContext.SelectionGroup
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
                    if (ThisTask.TaskAction != TaskActionEnum.HierarchyAndReduction)
                    {
                        Log.Error($"In ContentPublishingController.GoLive action: " +
                            $"for selection group {relatedSelectionGroup.Id}, " +
                            $"reduction task {ThisTask.Id} " +
                            $"should have action {TaskActionEnum.HierarchyAndReduction.ToString()} " +
                            $"but is {ThisTask.TaskAction.ToString()}, " +
                            "aborting");
                        Response.Headers.Add("Warning",
                            $"Go live request failed to verify related content reduction task {ThisTask.Id}.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                    // The reduced content file identified in the ContentReductionTask must exist
                    if (!System.IO.File.Exists(ThisTask.ResultFilePath))
                    {
                        Log.Error($"In ContentPublishingController.GoLive action: " +
                            $"for selection group {relatedSelectionGroup.Id}, " +
                            $"reduced content file {ThisTask.ResultFilePath} not found, aborting");
                        Response.Headers.Add("Warning",
                            $"Reduced content file {ThisTask.ResultFilePath} does not exist, " +
                            "cannot complete the go-live request.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                    //// Validate file checksum for reduced content
                    //var currentChecksum = GlobalFunctions.GetFileChecksum(ThisTask.ResultFilePath).ToLower();
                    //if (currentChecksum != ThisTask.ReducedContentChecksum.ToLower())
                    //{
                    //    Log.Error($"In ContentPublishingController.GoLive action: " +
                    //        "for selection group {relatedSelectionGroup.Id}, " +
                    //        "reduced content file {ThisTask.ResultFilePath} failed checksum validation, " +
                    //        "aborting");
                    //    AuditLogger.Log(AuditEventType.GoLiveValidationFailed.ToEvent(
                    //        publicationRequest.RootContentItem, publicationRequest));
                    //    Response.Headers.Add("Warning", $"Reduced content file failed integrity check, " +
                    //        "cannot complete the go-live request.");
                    //    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    //}
                }

                LiveHierarchy = ContentReductionHierarchy<ReductionFieldValue>
                    .GetHierarchyForRootContentItem(DbContext, publicationRequest.RootContentItemId);
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

            // Validate Checksums of LiveReady files
            //foreach (ContentRelatedFile Crf in publicationRequest.LiveReadyFilesObj)
            //{
            //    if (!Crf.ValidateChecksum())
            //    {
            //        Log.Error($"In ContentPublishingController.GoLive action: " +
            //            $"for publication request {publicationRequest.Id}, " +
            //            $"live ready file {Crf.FullPath} failed checksum validation, " +
            //            "aborting");
            //        AuditLogger.Log(AuditEventType.GoLiveValidationFailed.ToEvent(
            //            publicationRequest.RootContentItem, publicationRequest));
            //        Response.Headers.Add("Warning", "File integrity validation failed");
            //        return StatusCode(StatusCodes.Status422UnprocessableEntity);
            //    }
            //}
            #endregion

            /* At this point, available variables include:
             * - PubRequest - validated to be the relevant ContentPublicationRequest instance, with RootContentItem and ApplicationUser navigation properties
             * - RelatedReductionTasks - List of ContentReductionTask instances referring to PubRequest, with SelectionGroup navigation properties
             * - LiveHierarchy - Reflects the hierarchy of the currently live content
             * - NewHierarchy - Reflects the new hierarchy of the content being requested to go live
             */

            //List<string> FilesToDelete = new List<string>();

            //using (IDbContextTransaction Txn = DbContext.Database.BeginTransaction())
            //{
            //    // 1 Move new master content and related files (not reduced content) into live file names, removing any existing copies of previous version
            //    List<ContentRelatedFile> UpdatedContentFilesList = publicationRequest.RootContentItem.ContentFilesList;
            //    foreach (ContentRelatedFile Crf in publicationRequest.LiveReadyFilesObj)
            //    {
            //        // This assignment defines the live file name
            //        string TargetFileName = ContentTypeSpecificApiBase.GenerateContentFileName(Crf.FilePurpose, Path.GetExtension(Crf.FullPath), rootContentItemId);
            //        string TargetFilePath = Path.Combine(Path.GetDirectoryName(Crf.FullPath), TargetFileName);

            //        // Move any existing file to backed up name
            //        if (System.IO.File.Exists(TargetFilePath))
            //        {
            //            string BackupFilePath = TargetFilePath + ".bak";
            //            if (System.IO.File.Exists(BackupFilePath))
            //            {
            //                System.IO.File.Delete(BackupFilePath);
            //            }
            //            System.IO.File.Move(TargetFilePath, BackupFilePath);
            //            FilesToDelete.Add(BackupFilePath);
            //        }

            //        // Deallocate any temporary document licenses for preview file(s)
            //        switch (publicationRequest.RootContentItem.ContentType.TypeEnum)
            //        {
            //            case ContentTypeEnum.Qlikview:
            //                foreach (ContentRelatedFile Qvw in publicationRequest.LiveReadyFilesObj.Where(f => string.Compare(Path.GetExtension(f.FullPath), ".qvw", true) == 0))
            //                {
            //                    string FileRelativePath = Path.GetRelativePath(ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath"), Qvw.FullPath);
            //                    await new QlikviewLibApi().ReclaimAllDocCalsForFile(FileRelativePath, QlikviewConfig);
            //                }
            //                break;

            //            default:
            //                break;
            //        }

            //        // Can't move between different volumes
            //        System.IO.File.Copy(Crf.FullPath, TargetFilePath);
            //        FilesToDelete.Add(Crf.FullPath);

            //        UpdatedContentFilesList.RemoveAll(f => f.FilePurpose.ToLower() == Crf.FilePurpose.ToLower());
            //        UpdatedContentFilesList.Add(new ContentRelatedFile { FilePurpose = Crf.FilePurpose, FullPath = TargetFilePath, Checksum = Crf.Checksum, FileOriginalName = Crf.FileOriginalName });

            //        // Set content URL in each master SelectionGroup
            //        if (Crf.FilePurpose.ToLower() == "mastercontent")
            //        {
            //            IEnumerable<SelectionGroup> MasterSelectionGroupQuery = null;
            //            if (publicationRequest.RootContentItem.DoesReduce)
            //            {
            //                MasterSelectionGroupQuery = RelatedReductionTasks.Select(t => t.SelectionGroup).Where(g => g.IsMaster);
            //            }
            //            else
            //            {
            //                MasterSelectionGroupQuery = DbContext.SelectionGroup.Where(g => g.RootContentItemId == publicationRequest.RootContentItemId).Where(g => g.IsMaster);
            //            }
            //            foreach (SelectionGroup MasterContentGroup in MasterSelectionGroupQuery)
            //            {
            //                MasterContentGroup.SetContentUrl(TargetFileName);
            //                DbContext.SelectionGroup.Update(MasterContentGroup);
            //            }
            //        }
            //    }
            //    publicationRequest.RootContentItem.ContentFilesList = UpdatedContentFilesList;

            //    // 2 Rename reduced content files to live names
            //    foreach (var ThisTask in RelatedReductionTasks.Where(t => !t.SelectionGroup.IsMaster))
            //    {
            //        // This assignment defines the live file name for any reduced content file
            //        string TargetFileName = ContentTypeSpecificApiBase.GenerateReducedContentFileName(ThisTask.SelectionGroupId, publicationRequest.RootContentItemId, Path.GetExtension(ThisTask.ResultFilePath));
            //        string TargetFilePath = Path.Combine(ApplicationConfig.GetSection("Storage")["ContentItemRootPath"], publicationRequest.RootContentItemId.ToString(), TargetFileName);

            //        // Set url in SelectionGroup
            //        ThisTask.SelectionGroup.SetContentUrl(TargetFileName);
            //        ThisTask.SelectionGroup.ReducedContentChecksum = ThisTask.ReducedContentChecksum;
            //        DbContext.SelectionGroup.Update(ThisTask.SelectionGroup);

            //        // Move the existing file to backed up name if exists
            //        if (System.IO.File.Exists(TargetFilePath))
            //        {
            //            string BackupFilePath = TargetFilePath + ".bak";
            //            if (System.IO.File.Exists(BackupFilePath))
            //            {
            //                System.IO.File.Delete(BackupFilePath);
            //            }
            //            System.IO.File.Move(TargetFilePath, BackupFilePath);
            //            FilesToDelete.Add(BackupFilePath);
            //        }

            //        System.IO.File.Copy(ThisTask.ResultFilePath, TargetFilePath);
            //        FilesToDelete.Add(ThisTask.ResultFilePath);
            //    }

            //    //3 Update db:
            //    //3.1  ContentPublicationRequest.Status
            //    foreach (ContentPublicationRequest PreviousLiveRequest in DbContext.ContentPublicationRequest.Where(r => r.RootContentItemId == publicationRequest.RootContentItemId)
            //                                                                                                 .Where(r => r.RequestStatus == PublicationStatus.Confirmed))
            //    {
            //        PreviousLiveRequest.RequestStatus = PublicationStatus.Replaced;
            //        DbContext.ContentPublicationRequest.Update(PreviousLiveRequest);
            //    }
            //    publicationRequest.RequestStatus = PublicationStatus.Confirmed;

            //    //3.2  ContentReductionTask.Status
            //    foreach (ContentReductionTask PreviousLiveTask in DbContext.ContentReductionTask.Where(r => r.SelectionGroup.RootContentItemId == publicationRequest.RootContentItemId)
            //                                                                                    .Where(r => r.ReductionStatus == ReductionStatusEnum.Live))
            //    {
            //        PreviousLiveTask.ReductionStatus = ReductionStatusEnum.Replaced;
            //        DbContext.ContentReductionTask.Update(PreviousLiveTask);
            //    }
            //    RelatedReductionTasks.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Live);

            //    //3.3  HierarchyFieldValue due to hierarchy changes
            //    //3.3.1  If this is first publication for this root content item, add the fields to db and to LiveHierarchy to help identify all values as new
            //    if (LiveHierarchy.Fields.Count == 0)
            //    {  // This must be first time publication, need to insert the fields.  Values are handled below
            //        NewHierarchy.Fields.ForEach(f =>
            //        {
            //            HierarchyField NewField = new HierarchyField
            //            {
            //                FieldName = f.FieldName,
            //                FieldDisplayName = f.DisplayName,
            //                RootContentItemId = publicationRequest.RootContentItemId,
            //                FieldDelimiter = f.ValueDelimiter,
            //                StructureType = f.StructureType,
            //            };
            //            DbContext.HierarchyField.Add(NewField);
            //            DbContext.SaveChanges();

            //            LiveHierarchy.Fields.Add(new ReductionField<ReductionFieldValue>
            //            {
            //                Id = NewField.Id,  // Id is assigned during DbContext.SaveChanges() above
            //                FieldName = NewField.FieldName,
            //                DisplayName = NewField.FieldDisplayName,
            //                StructureType = NewField.StructureType,
            //                ValueDelimiter = NewField.FieldDelimiter,
            //                Values = new List<ReductionFieldValue>(),
            //            });
            //        });
            //    }
            //    //3.3.2  Add/Remove field values based on value list differences between new/old
            //    foreach (var NewHierarchyField in NewHierarchy.Fields)
            //    {
            //        ReductionField<ReductionFieldValue> MatchingLiveField = LiveHierarchy.Fields.Single(f => f.FieldName == NewHierarchyField.FieldName);

            //        List<string> NewHierarchyFieldValueList = NewHierarchyField.Values.Select(v => v.Value).ToList();
            //        List<string> LiveHierarchyFieldValueList = MatchingLiveField.Values.Select(v => v.Value).ToList();

            //        // Insert new values
            //        foreach (string NewValue in NewHierarchyFieldValueList.Except(LiveHierarchyFieldValueList))
            //        {
            //            DbContext.HierarchyFieldValue.Add(new HierarchyFieldValue { HierarchyFieldId = MatchingLiveField.Id, Value = NewValue });
            //        }

            //        // Delete removed values
            //        foreach (string RemovedValue in LiveHierarchyFieldValueList.Except(NewHierarchyFieldValueList))
            //        {
            //            HierarchyFieldValue ObsoleteRecord = DbContext.HierarchyFieldValue.Single(v => v.HierarchyField.RootContentItemId == publicationRequest.RootContentItemId
            //                                                                                        && v.Value == RemovedValue);
            //            DbContext.HierarchyFieldValue.Remove(ObsoleteRecord);
            //        }
            //    }
            //    DbContext.SaveChanges();

            //    //3.4  Update SelectionGroup SelectedHierarchyFieldValueList due to hierarchy changes
            //    List<Guid> AllRemainingFieldValues = DbContext.HierarchyFieldValue.Where(v => v.HierarchyField.RootContentItemId == publicationRequest.RootContentItemId)
            //                                                                      .Select(v => v.Id)
            //                                                                      .ToList();
            //    foreach (SelectionGroup Group in DbContext.SelectionGroup.Where(g => g.RootContentItemId == publicationRequest.RootContentItemId && !g.IsMaster))
            //    {
            //        Group.SelectedHierarchyFieldValueList = Group.SelectedHierarchyFieldValueList.Intersect(AllRemainingFieldValues).ToArray();
            //    }

            //    // Perform any content type dependent follow up processing
            //    switch (publicationRequest.RootContentItem.ContentType.TypeEnum)
            //    {
            //        case ContentTypeEnum.Qlikview:
            //            await new QlikviewLib.QlikviewLibApi().AuthorizeUserDocumentsInFolder(rootContentItemId.ToString(), QlikviewConfig);
            //            break;

            //        case ContentTypeEnum.Unknown:
            //        default:
            //            break;
            //    }

            //    DbContext.SaveChanges();
            //    Txn.Commit();
            //}

            //Log.Verbose($"In ContentPublishingController.GoLive action: publication request {publicationRequest.Id} success");
            //AuditLogger.Log(AuditEventType.ContentPublicationGoLive.ToEvent(publicationRequest.RootContentItem, publicationRequest, validationSummaryId));

            //// 4 Delete all temporary files
            //foreach (string FileToDelete in FilesToDelete)
            //{
            //    try
            //    {
            //        System.IO.File.Delete(FileToDelete);
            //    }
            //    catch (DirectoryNotFoundException)
            //    {
            //        continue;
            //    }
            //}

            //if (ReductionIsInvolved)
            //{
            //    //Delete temporary folder of publication job (contains temporary reduced content files)
            //    string PubJobTempFolder = Path.GetDirectoryName(RelatedReductionTasks[0].MasterFilePath);
            //    Directory.Delete(PubJobTempFolder, true);
            //}

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid rootContentItemId, Guid publicationRequestId)
        {
            Log.Verbose($"Entered ContentPublishingController.Reject action with content item {rootContentItemId}, publication request {publicationRequestId}");

            #region Authorization
            AuthorizationResult authorization = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, rootContentItemId));
            if (!authorization.Succeeded)
            {
                Log.Debug($"In ContentPublishingController.Reject action, authorization failure, user {User.Identity.Name}, content item {rootContentItemId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", "You are not authorized to publish content for this content item.");
                return Unauthorized();
            }
            #endregion

            // the content item exists if the authorization check passes
            RootContentItem rootContentItem = DbContext.RootContentItem.Find(rootContentItemId);

            #region Validation
            ContentPublicationRequest pubRequest = DbContext.ContentPublicationRequest.Find(publicationRequestId);
            if (pubRequest == null || pubRequest.RootContentItemId != rootContentItemId)
            {
                Log.Debug($"In ContentPublishingController.Reject action, publication request {publicationRequestId} not found, or associated content item, aborting");
                Response.Headers.Add("Warning", "The requested publication request does not exist.");
                return BadRequest();
            }

            if (pubRequest.RequestStatus != PublicationStatus.Processed)
            {
                Log.Debug($"In ContentPublishingController.Reject action, publication request {publicationRequestId} is not ready to go live, status = {pubRequest.RequestStatus.ToString()}, aborting");
                Response.Headers.Add("Warning", "The specified publication request is not currently processed.");
                return BadRequest();
            }
            #endregion

            using (var Txn = DbContext.Database.BeginTransaction())
            {
                pubRequest.RequestStatus = PublicationStatus.Rejected;
                DbContext.ContentPublicationRequest.Update(pubRequest);

                List<ContentReductionTask> RelatedTasks = DbContext.ContentReductionTask.Where(t => t.ContentPublicationRequestId == publicationRequestId).ToList();
                foreach (ContentReductionTask relatedTask in RelatedTasks)
                {
                    relatedTask.ReductionStatus = ReductionStatusEnum.Rejected;
                    DbContext.ContentReductionTask.Update(relatedTask);
                }
                DbContext.SaveChanges();

                // Delete each staged prelive file
                foreach (ContentRelatedFile PreliveFile in pubRequest.LiveReadyFilesObj)
                {
                    if (System.IO.File.Exists(PreliveFile.FullPath))
                    {
                        System.IO.File.Delete(PreliveFile.FullPath);
                    }
                }
                // Delete any FileExchange folder
                if (RelatedTasks.Any())
                {
                    string ExchangeFolder = Path.GetDirectoryName(RelatedTasks[0].MasterFilePath);
                    if (Directory.Exists(ExchangeFolder))
                    {
                        Directory.Delete(ExchangeFolder, true);
                    }
                }

                Txn.Commit();
            }

            Log.Verbose($"In ContentPublishingController.Reject action, success");
            AuditLogger.Log(AuditEventType.ContentPublicationRejected.ToEvent(rootContentItem, pubRequest));

            return Ok();
        }
    }
}
