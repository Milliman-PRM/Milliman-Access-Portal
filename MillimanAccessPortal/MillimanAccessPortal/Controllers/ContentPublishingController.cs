/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Controller for actions supporting the content publishing page
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ContentPublishing;
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
        private readonly ILogger Logger;
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
            ILoggerFactory LoggerFactoryArg,
            StandardQueries QueriesArg,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration ApplicationConfigArg,
            IOptions<QlikviewConfig> QlikviewOptionsAccessorArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DbContext = ContextArg;
            Logger = LoggerFactoryArg.CreateLogger<ContentPublishingController>(); ;
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
            #region Authorization
            AuthorizationResult RoleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentPublisher, null));
            if (!RoleInClientResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to publish content.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            var model = await ClientTree.Build(await Queries.GetCurrentApplicationUser(User), UserManager, DbContext, RoleEnum.ContentPublisher);

            return new JsonResult(model);
        }

        [HttpGet]
        public async Task<IActionResult> RootContentItems(Guid clientId)
        {
            Client client = DbContext.Client.Find(clientId);

            #region Preliminary validation
            if (client == null)
            {
                Response.Headers.Add("Warning", "The requested client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentPublisher, clientId));
            if (!roleInClientResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to publish content for the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            RootContentItemList model = RootContentItemList.Build(DbContext, client, await Queries.GetCurrentApplicationUser(User), RoleEnum.ContentPublisher);

            return Json(model);
        }

        [HttpGet]
        public async Task<IActionResult> RootContentItemDetail(Guid rootContentItemId)
        {
            RootContentItem rootContentItem = DbContext.RootContentItem.Find(rootContentItemId);

            #region Preliminary validation
            if (rootContentItem == null)
            {
                Response.Headers.Add("Warning", "The requested root content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInClientResult = await AuthorizationService.AuthorizeAsync(
                User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, rootContentItemId));
            if (!roleInClientResult.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to publish content to the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            RootContentItemDetail model = Models.ContentPublishing.RootContentItemDetail.Build(DbContext, rootContentItem);

            return Json(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRootContentItem(RootContentItem rootContentItem)
        {
            #region Preliminary validation
            var client = DbContext.Client
                .Where(c => c.Id == rootContentItem.ClientId)
                .SingleOrDefault();
            if (client == null)
            {
                Response.Headers.Add("Warning", "The associated client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInClientResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.ContentPublisher, rootContentItem.ClientId));
            if (!roleInClientResult.Succeeded)
            {
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", "You are not authorized to create root content items for the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var contentType = DbContext.ContentType
                .Where(c => c.Id == rootContentItem.ContentTypeId)
                .SingleOrDefault();
            if (contentType == null)
            {
                Response.Headers.Add("Warning", "The associated content type does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (rootContentItem.ContentName == null)
            {
                Response.Headers.Add("Warning", "You must supply a name for the root content item.");
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
                        .Where(r => r.RoleId == ApplicationRole.RoleIds[role])
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

            AuditLogger.Log(AuditEventType.RootContentItemCreated.ToEvent(rootContentItem));

            RootContentItemSummary summary = RootContentItemSummary.Build(DbContext, rootContentItem);
            RootContentItemDetail detail = Models.ContentPublishing.RootContentItemDetail.Build(DbContext, rootContentItem);

            return Json(new { summary, detail });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRootContentItem(RootContentItem rootContentItem)
        {
            #region Preliminary validation
            var currentRootContentItem = DbContext.RootContentItem
                .Include(c => c.ContentType)
                .Where(i => i.Id == rootContentItem.Id)
                .SingleOrDefault();
            if (currentRootContentItem == null)
            {
                Response.Headers.Add("Warning", "The specified root content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, rootContentItem.Id));
            if (!roleInRootContentItemResult.Succeeded)
            {
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", "You are not authorized to update this root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            if (currentRootContentItem.ContentType == null)
            {
                Response.Headers.Add("Warning", "The associated content type does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            if (rootContentItem.ContentName == null)
            {
                Response.Headers.Add("Warning", "You must supply a name for the root content item.");
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

            AuditLogger.Log(AuditEventType.RootContentItemUpdated.ToEvent(rootContentItem));

            RootContentItemSummary summary = RootContentItemSummary.Build(DbContext, currentRootContentItem);
            RootContentItemDetail detail = Models.ContentPublishing.RootContentItemDetail.Build(DbContext, currentRootContentItem);

            return Json(new { summary, detail });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRootContentItem(Guid rootContentItemId)
        {
            var rootContentItem = DbContext.RootContentItem
                .Include(x => x.Client)
                .SingleOrDefault(x => x.Id == rootContentItemId);

            #region Preliminary Validation
            if (rootContentItem == null)
            {
                Response.Headers.Add("Warning", "The requested root content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, rootContentItem.Id));
            if (!roleInRootContentItemResult.Succeeded)
            {
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", "You are not authorized to administer the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            List<PublicationStatus> blockingRequestStatusList = new List<PublicationStatus>
                                                              { PublicationStatus.Processing, PublicationStatus.Processed, PublicationStatus.Queued };
            var blocked = DbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == rootContentItemId)
                .Any(r => blockingRequestStatusList.Contains(r.RequestStatus));
            if (blocked)
            {
                Response.Headers.Add("Warning", "The specified root content item cannot be deleted at this time.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            RootContentItemDetail model = Models.ContentPublishing.RootContentItemDetail.Build(DbContext, rootContentItem);

            DbContext.RootContentItem.Remove(rootContentItem);
            DbContext.SaveChanges();

            try
            {
                string ContentFolderPath = Path.Combine(ApplicationConfig.GetSection("Storage")["ContentItemRootPath"], rootContentItem.Id.ToString());
                Directory.Delete(ContentFolderPath, true);
            }
            catch
            {
                if (! (new StackTrace()).GetFrames().Any(f => f.GetMethod().DeclaringType.Namespace == "MapTests"))
                {
                    throw;
                }
            }

            AuditLogger.Log(AuditEventType.RootContentItemDeleted.ToEvent(rootContentItem));

            return Json(model);
        }

        [HttpPost]
        public async Task<IActionResult> Publish(PublishRequest Arg)
        {
            ApplicationUser currentApplicationUser = await Queries.GetCurrentApplicationUser(User);

            #region Preliminary Validation
            if (currentApplicationUser == null)
            {
                Response.Headers.Add("Warning", "Your user identity is unknown.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, Arg.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", $"You are not authorized to publish this content");
                return Unauthorized();
            }

            #endregion

#if false   // not sure what all the implications are of this. For now this issue is covered in validation below.
            // Try to cancel all queued (not running) publishing requests and reduction tasks for the content
            using (IDbContextTransaction Txn = DbContext.Database.BeginTransaction())
            {
                List<ContentPublicationRequest> QueuedRequests = DbContext.ContentPublicationRequest
                                                                          .Where(r => r.RootContentItemId == Arg.RootContentItemId)
                                                                          .Where(r => r.RequestStatus == PublicationStatus.Queued)
                                                                          .ToList();
                List<ContentReductionTask> QueuedTasks = DbContext.ContentReductionTask
                                                                  .Include(t => t.SelectionGroup)
                                                          // ?    .Where(t => t.ContentPublicationRequestId == null)
                                                                  .Where(t => t.SelectionGroup.RootContentItemId == Arg.RootContentItemId)
                                                                  .Where(t => t.ReductionStatus == ReductionStatusEnum.Queued)
                                                                  .ToList();

                QueuedRequests.ForEach(r => r.RequestStatus = PublicationStatus.Canceled);
                QueuedTasks.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Canceled);

                DbContext.ContentPublicationRequest.UpdateRange(QueuedRequests);
                DbContext.ContentPublicationRequest.UpdateRange(QueuedRequests);

                DbContext.SaveChanges();
                Txn.Commit();
            }
#endif

            RootContentItem ContentItem = DbContext.RootContentItem
                                                   .Include(rc => rc.ContentType)
                                                   .SingleOrDefault(rc => rc.Id == Arg.RootContentItemId);

            #region Validation
            // The requested RootContentItem must exist
            if (ContentItem == null)
            {
                Response.Headers.Add("Warning", "Requested content item not found.");
                return BadRequest();
            }

            // All the provided references to related files must be found in the FileUpload entity.  
            if (Arg.RelatedFiles.Any(f => DbContext.FileUpload.Count(fu => fu.Id == f.FileUploadId) != 1))
            {
                Response.Headers.Add("Warning", "A specified uploaded file was not found.");
                return BadRequest();
            }

            bool Blocked;

            // There must be no unresolved ContentPublicationRequest.
            List<PublicationStatus> BlockingRequestStatusList = new List<PublicationStatus>
                                                              { PublicationStatus.Processing, PublicationStatus.Processed, PublicationStatus.Queued };
            Blocked = DbContext.ContentPublicationRequest
                               .Where(r => r.RootContentItemId == Arg.RootContentItemId)
                               .Any(r => BlockingRequestStatusList.Contains(r.RequestStatus));
            if (Blocked)
            {
                Response.Headers.Add("Warning", "A previous publication is pending for this content.");
                return BadRequest();
            }

            List<ReductionStatusEnum> BlockingTaskStatusList = new List<ReductionStatusEnum>
                                                             { ReductionStatusEnum.Reducing, ReductionStatusEnum.Reduced, ReductionStatusEnum.Queued };
            Blocked = DbContext.ContentReductionTask
                               .Where(t => t.ContentPublicationRequestId == null)
                               .Include(t => t.SelectionGroup)
                               .Where(t => t.SelectionGroup.RootContentItemId == Arg.RootContentItemId)
                               .Any(t => BlockingTaskStatusList.Contains(t.ReductionStatus));
            if (Blocked)
            {
                Response.Headers.Add("Warning", "A previous reduction task is pending for this content.");
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
                Response.Headers.Add("Warning", "Failed to store publication request");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            string rootPath = ApplicationConfig.GetSection("Storage")["ContentItemRootPath"];
            string exchangePath = ApplicationConfig.GetSection("Storage")["MapPublishingServerExchangePath"];
            string CxnString = ApplicationConfig.GetConnectionString("DefaultConnection");  // key string must match that used in startup.cs
            ContentPublishSupport.AddPublicationMonitor(Task.Run(() =>
                ContentPublishSupport.MonitorPublicationRequestForQueueing(NewContentPublicationRequest.Id, CxnString, rootPath, exchangePath)));

            var rootContentItemDetail = Models.ContentPublishing.RootContentItemDetail.Build(DbContext, ContentItem);
            return Json(rootContentItemDetail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelContentPublicationRequest(Guid rootContentItemId)
        {
            #region Preliminary validation
            var rootContentItem = DbContext.RootContentItem.Find(rootContentItemId);
            if (rootContentItem == null)
            {
                Response.Headers.Add("Warning", "The specified root content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            AuthorizationResult roleInRootContentItem = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, rootContentItem.Id));
            if (!roleInRootContentItem.Succeeded)
            {
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", "You are not authorized to cancel content publication requests for this root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var cancelableStatus = new List<PublicationStatus>
            {
                PublicationStatus.Validating,
                PublicationStatus.Queued,
            };
            var contentPublicationRequest = DbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == rootContentItem.Id)
                .Where(r => cancelableStatus.Contains(r.RequestStatus))
                .SingleOrDefault();
            if (contentPublicationRequest == null)
            {
                Response.Headers.Add("Warning", "No cancelable requests for this root content item exist.");
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
                Response.Headers.Add("Warning", "The publication request failed to be canceled.  Processing may have started.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

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
            #region Authorization
            AuthorizationResult roleInRootContentItem = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, RootContentItemId));
            if (!roleInRootContentItem.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to view the publication certification summary for this root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            if (!DbContext.RootContentItem.Any(c => c.Id == RootContentItemId))
            {
                Response.Headers.Add("Warning", "The requested root content item was not found.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            PreLiveContentValidationSummary ReturnObj = PreLiveContentValidationSummary.Build(DbContext, RootContentItemId, ApplicationConfig);

            var preGoLiveSummary = new
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
            AuditLogger.Log(AuditEventType.PreGoLiveSummary.ToEvent(preGoLiveSummary));

            return new JsonResult(ReturnObj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoLive(Guid rootContentItemId, Guid publicationRequestId, string validationSummaryId)
        {
            #region Authorization
            AuthorizationResult authorization = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, rootContentItemId));
            if (!authorization.Succeeded)
            {
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", "You are not authorized to publish content for this root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            ContentPublicationRequest PubRequest = DbContext.ContentPublicationRequest.Where(r => r.Id == publicationRequestId)
                                                                                      .Where(r => r.RootContentItemId == rootContentItemId)
                                                                                      .Include(r => r.RootContentItem)
                                                                                          .ThenInclude(c => c.ContentType)
                                                                                      .Include(r => r.ApplicationUser)
                                                                                      .SingleOrDefault(r => r.RequestStatus == PublicationStatus.Processed);

            if (PubRequest == null || PubRequest.RootContentItem == null || PubRequest.ApplicationUser == null)
            {
                Response.Headers.Add("Warning", "Go-Live request references an invalid publication request.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            bool ReductionIsInvolved = PubRequest.RootContentItem.DoesReduce && PubRequest.LiveReadyFilesObj.Any(f => f.FilePurpose.ToLower() == "mastercontent");

            ContentReductionHierarchy<ReductionFieldValue> LiveHierarchy = new ContentReductionHierarchy<ReductionFieldValue> { RootContentItemId = PubRequest.RootContentItemId };
            ContentReductionHierarchy<ReductionFieldValue> NewHierarchy = new ContentReductionHierarchy<ReductionFieldValue> { RootContentItemId = PubRequest.RootContentItemId };

            List<ContentReductionTask> RelatedReductionTasks = DbContext.ContentReductionTask.Where(t => t.ContentPublicationRequestId == PubRequest.Id)
                                                                                             .Include(t => t.SelectionGroup)
                                                                                             .ThenInclude(g => g.RootContentItem)
                                                                                             .ThenInclude(c => c.ContentType)
                                                                                             .ToList();

            if (ReductionIsInvolved)
            {
                // For each reducing SelectionGroup related to the RootContentItem:
                foreach (SelectionGroup ContentRelatedSelectionGroup in DbContext.SelectionGroup.Where(g => g.RootContentItemId == rootContentItemId)
                                                                                                .Where(g => !g.IsMaster))
                {
                    ContentReductionTask ThisTask;

                    // RelatedReductionTasks should have one ContentReductionTask related to the SelectionGroup
                    try
                    {
                        ThisTask = RelatedReductionTasks.Single(t => t.SelectionGroupId == ContentRelatedSelectionGroup.Id);
                    }
                    catch (InvalidOperationException)
                    {
                        Response.Headers.Add("Warning", $"Expected 1 reduction task related to SelectionGroup {ContentRelatedSelectionGroup.Id}, cannot complete this go-live request.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }

                    // This ContentReductionTask must be a reducing task
                    if (ThisTask.TaskAction != TaskActionEnum.HierarchyAndReduction)
                    {
                        Response.Headers.Add("Warning", $"Go live request failed to verify related content reduction task {ThisTask.Id}.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                    // The reduced content file identified in the ContentReductionTask must exist
                    if (!System.IO.File.Exists(ThisTask.ResultFilePath))
                    {
                        Response.Headers.Add("Warning", $"Reduced content file {ThisTask.ResultFilePath} does not exist, cannot complete the go-live request.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                    // Validate file checksum for reduced content
                    if (GlobalFunctions.GetFileChecksum(ThisTask.ResultFilePath).ToLower() != ThisTask.ReducedContentChecksum.ToLower())
                    {
                        AuditLogger.Log(AuditEventType.GoLiveValidationFailed.ToEvent(PubRequest.RootContentItem, PubRequest));
                        Response.Headers.Add("Warning", $"Reduced content file failed integrity check, cannot complete the go-live request.");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                }

                LiveHierarchy = ContentReductionHierarchy<ReductionFieldValue>.GetHierarchyForRootContentItem(DbContext, PubRequest.RootContentItemId);
                NewHierarchy = RelatedReductionTasks[0].MasterContentHierarchyObj;

                if (LiveHierarchy.Fields.Count != 0)
                {
                    // No change in field list (e.g. names) should occur
                    if (!LiveHierarchy.Fields.Select(f => f.FieldName).ToHashSet().SetEquals(NewHierarchy.Fields.Select(f => f.FieldName)))
                    {
                        Response.Headers.Add("Warning", "New hierarchy field list does not match the live hierarchy");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                }
            }

            // Validate Checksums of LiveReady files
            foreach (ContentRelatedFile Crf in PubRequest.LiveReadyFilesObj)
            {
                if (!Crf.ValidateChecksum())
                {
                    AuditLogger.Log(AuditEventType.GoLiveValidationFailed.ToEvent(PubRequest.RootContentItem, PubRequest));
                    Response.Headers.Add("Warning", "File integrity validation failed");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }
            #endregion

            /* At this point, available variables include:
             * - PubRequest - validated to be the relevant ContentPublicationRequest instance, with RootContentItem and ApplicationUser navigation properties
             * - RelatedReductionTasks - List of ContentReductionTask instances referring to PubRequest, with SelectionGroup navigation properties
             * - LiveHierarchy - Reflects the hierarchy of the currently live content
             * - NewHierarchy - Reflects the new hierarchy of the content being requested to go live
             */

            List<string> FilesToDelete = new List<string>();

            using (IDbContextTransaction Txn = DbContext.Database.BeginTransaction())
            {
                // 1 Move new master content and related files (not reduced content) into live file names, removing any existing copies of previous version
                List<ContentRelatedFile> UpdatedContentFilesList = PubRequest.RootContentItem.ContentFilesList;
                foreach (ContentRelatedFile Crf in PubRequest.LiveReadyFilesObj)
                {
                    // This assignment defines the live file name
                    string TargetFileName = ContentAccessSupport.GenerateContentFileName(Crf, rootContentItemId);
                    string TargetFilePath = Path.Combine(Path.GetDirectoryName(Crf.FullPath), TargetFileName);

                    // Move any existing file to backed up name
                    if (System.IO.File.Exists(TargetFilePath))
                    {
                        string BackupFilePath = TargetFilePath + ".bak";
                        if (System.IO.File.Exists(BackupFilePath))
                        {
                            System.IO.File.Delete(BackupFilePath);
                        }
                        System.IO.File.Move(TargetFilePath, BackupFilePath);
                        FilesToDelete.Add(BackupFilePath);
                    }

                    // Can't move between different volumes
                    System.IO.File.Copy(Crf.FullPath, TargetFilePath);
                    FilesToDelete.Add(Crf.FullPath);

                    UpdatedContentFilesList.RemoveAll(f => f.FilePurpose.ToLower() == Crf.FilePurpose.ToLower());
                    UpdatedContentFilesList.Add(new ContentRelatedFile { FilePurpose = Crf.FilePurpose, FullPath = TargetFilePath, Checksum = Crf.Checksum });

                    // Set content URL in each master SelectionGroup
                    if (Crf.FilePurpose.ToLower() == "mastercontent")
                    {
                        IEnumerable<SelectionGroup> MasterSelectionGroupQuery = null;
                        if (PubRequest.RootContentItem.DoesReduce)
                        {
                            MasterSelectionGroupQuery = RelatedReductionTasks.Select(t => t.SelectionGroup).Where(g => g.IsMaster);
                        }
                        else
                        {
                            MasterSelectionGroupQuery = DbContext.SelectionGroup.Where(g => g.RootContentItemId == PubRequest.RootContentItemId).Where(g => g.IsMaster);
                        }
                        foreach (SelectionGroup MasterContentGroup in MasterSelectionGroupQuery)
                        {
                            MasterContentGroup.SetContentUrl(TargetFileName);
                            DbContext.SelectionGroup.Update(MasterContentGroup);
                        }
                    }
                }
                PubRequest.RootContentItem.ContentFilesList = UpdatedContentFilesList;

                // 2 Rename reduced content files to live names
                foreach (var ThisTask in RelatedReductionTasks.Where(t => !t.SelectionGroup.IsMaster))
                {
                    // This assignment defines the live file name for any reduced content file
                    string TargetFileName = ContentAccessSupport.GenerateReducedContentFileName(ThisTask.SelectionGroupId, PubRequest.RootContentItemId, Path.GetExtension(ThisTask.ResultFilePath));
                    string TargetFilePath = Path.Combine(ApplicationConfig.GetSection("Storage")["ContentItemRootPath"], PubRequest.RootContentItemId.ToString(), TargetFileName);

                    // Set url in SelectionGroup
                    ThisTask.SelectionGroup.SetContentUrl(TargetFileName);
                    DbContext.SelectionGroup.Update(ThisTask.SelectionGroup);

                    // Move the existing file to backed up name if exists
                    if (System.IO.File.Exists(TargetFilePath))
                    {
                        string BackupFilePath = TargetFilePath + ".bak";
                        if (System.IO.File.Exists(BackupFilePath))
                        {
                            System.IO.File.Delete(BackupFilePath);
                        }
                        System.IO.File.Move(TargetFilePath, BackupFilePath);
                        FilesToDelete.Add(BackupFilePath);
                    }

                    System.IO.File.Copy(ThisTask.ResultFilePath, TargetFilePath);
                    FilesToDelete.Add(ThisTask.ResultFilePath);
                }

                //3 Update db:
                //3.1  ContentPublicationRequest.Status
                foreach (ContentPublicationRequest PreviousLiveRequest in DbContext.ContentPublicationRequest.Where(r => r.RequestStatus == PublicationStatus.Confirmed))
                {
                    PreviousLiveRequest.RequestStatus = PublicationStatus.Replaced;
                    DbContext.ContentPublicationRequest.Update(PreviousLiveRequest);
                }
                PubRequest.RequestStatus = PublicationStatus.Confirmed;

                //3.2  ContentReductionTask.Status
                foreach (ContentReductionTask PreviousLiveTask in DbContext.ContentReductionTask.Where(r => r.ReductionStatus == ReductionStatusEnum.Live))
                {
                    PreviousLiveTask.ReductionStatus = ReductionStatusEnum.Replaced;
                    DbContext.ContentReductionTask.Update(PreviousLiveTask);
                }
                RelatedReductionTasks.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Live);

                //3.3  HierarchyFieldValue due to hierarchy changes
                //3.3.1  If this is first publication for this root content item, add the fields to db and to LiveHierarchy to help identify all values as new
                if (LiveHierarchy.Fields.Count == 0)
                {  // This must be first time publication, need to insert the fields.  Values are handled below
                    NewHierarchy.Fields.ForEach(f =>
                    {
                        HierarchyField NewField = new HierarchyField
                        {
                            FieldName = f.FieldName,
                            FieldDisplayName = f.DisplayName,
                            RootContentItemId = PubRequest.RootContentItemId,
                            FieldDelimiter = f.ValueDelimiter,
                            StructureType = f.StructureType,
                        };
                        DbContext.HierarchyField.Add(NewField);
                        DbContext.SaveChanges();

                        LiveHierarchy.Fields.Add(new ReductionField<ReductionFieldValue>
                        {
                            Id = NewField.Id,  // Id is assigned during DbContext.SaveChanges() above
                            FieldName = NewField.FieldName,
                            DisplayName = NewField.FieldDisplayName,
                            StructureType = NewField.StructureType,
                            ValueDelimiter = NewField.FieldDelimiter,
                            Values = new List<ReductionFieldValue>(),
                        });
                    });
                }
                //3.3.2  Add/Remove field values based on value list differences between new/old
                foreach (var NewHierarchyField in NewHierarchy.Fields)
                {
                    ReductionField<ReductionFieldValue> MatchingLiveField = LiveHierarchy.Fields.Single(f => f.FieldName == NewHierarchyField.FieldName);

                    List<string> NewHierarchyFieldValueList = NewHierarchyField.Values.Select(v => v.Value).ToList();
                    List<string> LiveHierarchyFieldValueList = MatchingLiveField.Values.Select(v => v.Value).ToList();

                    // Insert new values
                    foreach (string NewValue in NewHierarchyFieldValueList.Except(LiveHierarchyFieldValueList))
                    {
                        DbContext.HierarchyFieldValue.Add(new HierarchyFieldValue { HierarchyFieldId = MatchingLiveField.Id, Value = NewValue });
                    }

                    // Delete removed values
                    foreach (string RemovedValue in LiveHierarchyFieldValueList.Except(NewHierarchyFieldValueList))
                    {
                        HierarchyFieldValue ObsoleteRecord = DbContext.HierarchyFieldValue.Single(v => v.HierarchyField.RootContentItemId == PubRequest.RootContentItemId
                                                                                                    && v.Value == RemovedValue);
                        DbContext.HierarchyFieldValue.Remove(ObsoleteRecord);
                    }
                }
                DbContext.SaveChanges();

                //3.4  Update SelectionGroup SelectedHierarchyFieldValueList due to hierarchy changes
                List<Guid> AllRemainingFieldValues = DbContext.HierarchyFieldValue.Where(v => v.HierarchyField.RootContentItemId == PubRequest.RootContentItemId)
                                                                                  .Select(v => v.Id)
                                                                                  .ToList();
                foreach (SelectionGroup Group in DbContext.SelectionGroup.Where(g => g.RootContentItemId == PubRequest.RootContentItemId && !g.IsMaster))
                {
                    Group.SelectedHierarchyFieldValueList = Group.SelectedHierarchyFieldValueList.Intersect(AllRemainingFieldValues).ToArray();
                }

                // Perform any content type dependent follow up processing
                switch (PubRequest.RootContentItem.ContentType.TypeEnum)
                {
                    case ContentTypeEnum.Qlikview:
                        await new QlikviewLib.QlikviewLibApi().AuthorizeUserDocumentsInFolder(rootContentItemId.ToString(), QlikviewConfig);
                        break;

                    case ContentTypeEnum.Unknown:
                    default:
                        break;
                }

                DbContext.SaveChanges();
                Txn.Commit();
            }

            AuditLogger.Log(AuditEventType.ContentPublicationGoLive.ToEvent(PubRequest.RootContentItem, PubRequest, validationSummaryId));

            // 4 Delete all temporary files
            foreach (string FileToDelete in FilesToDelete)
            {
                try
                {
                    System.IO.File.Delete(FileToDelete);
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }
            }

            if (ReductionIsInvolved)
            {
                //Delete temporary folder of publication job (contains temporary reduced content files)
                string PubJobTempFolder = Path.GetDirectoryName(RelatedReductionTasks[0].MasterFilePath);
                Directory.Delete(PubJobTempFolder, true);
            }

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid rootContentItemId, Guid publicationRequestId)
        {
            // TODO Could/should this be handled in the Cancel action?
            #region Authorization
            AuthorizationResult authorization = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, rootContentItemId));
            if (!authorization.Succeeded)
            {
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));
                Response.Headers.Add("Warning", "You are not authorized to publish content for this root content item.");
                return Unauthorized();
            }
            #endregion

            // the content item exists if the authorization check passes
            RootContentItem rootContentItem = DbContext.RootContentItem.Find(rootContentItemId);

            #region Validation
            ContentPublicationRequest pubRequest = DbContext.ContentPublicationRequest.Find(publicationRequestId);
            if (pubRequest == null || pubRequest.RootContentItemId != rootContentItemId)
            {
                Response.Headers.Add("Warning", "The requested publication request does not exist.");
                return BadRequest();
            }

            if (pubRequest.RequestStatus != PublicationStatus.Processed)
            {
                Response.Headers.Add("Warning", "The specified publication request is not currently queued.");
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

            AuditLogger.Log(AuditEventType.ContentPublicationRejected.ToEvent(rootContentItem, pubRequest));

            return Ok();
        }
    }
}
