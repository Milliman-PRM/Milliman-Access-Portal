/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Controller for actions supporting the content publishing page
 * DEVELOPER NOTES:
 */

using AuditLogLib;
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
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ContentPublishing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    public class ContentPublishingController : Controller
    {
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ApplicationDbContext DbContext;
        private readonly ILogger Logger;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> UserManager;


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
            UserManager<ApplicationUser> UserManagerArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DbContext = ContextArg;
            Logger = LoggerFactoryArg.CreateLogger<ContentPublishingController>(); ;
            Queries = QueriesArg;
            UserManager = UserManagerArg;
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

            var model = await ClientTree.Build(await Queries.GetCurrentApplicationUser(User), UserManager, DbContext);

            return new JsonResult(model);
        }

        [HttpGet]
        public async Task<IActionResult> RootContentItems(long clientId)
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

            RootContentItemList model = RootContentItemList.Build(DbContext, client, await Queries.GetCurrentApplicationUser(User));

            return Json(model);
        }

        [HttpGet]
        public async Task<IActionResult> RootContentItemDetail(long rootContentItemId)
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
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to create root content item without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentPublisher]} role in client",
                    AuditEventId.Unauthorized,
                    new { ClientId = rootContentItem.ClientId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

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
                var automaticRoles = DbContext.UserRoleInClient
                    .Where(r => r.ClientId == rootContentItem.ClientId)
                    .Where(r => r.RoleId == ((long) RoleEnum.ContentPublisher))
                    .Select(r => new UserRoleInRootContentItem
                    {
                        UserId = r.UserId,
                        RootContentItemId = rootContentItem.Id,
                        RoleId = ((long) RoleEnum.ContentPublisher),
                    });
                DbContext.UserRoleInRootContentItem.AddRange(automaticRoles);
                DbContext.SaveChanges();

                DbTransaction.Commit();
            }

            #region Log audit event
            AuditEvent rootContentItemCreatedEvent = AuditEvent.New(
                $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                "Root content item created",
                AuditEventId.RootContentItemCreated,
                new { ClientId = rootContentItem.ClientId, RootContentItemId = rootContentItem.Id },
                User.Identity.Name,
                HttpContext.Session.Id
                );
            AuditLogger.Log(rootContentItemCreatedEvent);
            #endregion

            RootContentItemSummary model = RootContentItemSummary.Build(DbContext, rootContentItem);

            return Json(model);
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
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to update root content item without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentPublisher]} role in item",
                    AuditEventId.Unauthorized,
                    new { RootContentItemId = rootContentItem.Id },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

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
            currentRootContentItem.ContentTypeId = rootContentItem.ContentTypeId;
            currentRootContentItem.Description = rootContentItem.Description;
            currentRootContentItem.DoesReduce = rootContentItem.DoesReduce;
            currentRootContentItem.Notes = rootContentItem.Notes;
            currentRootContentItem.TypeSpecificDetail = rootContentItem.TypeSpecificDetail;

            DbContext.RootContentItem.Update(currentRootContentItem);
            DbContext.SaveChanges();

            #region Log audit event
            AuditEvent rootContentItemUpdatedEvent = AuditEvent.New(
                $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                "Root content item updated",
                AuditEventId.RootContentItemUpdated,
                new { ClientId = rootContentItem.ClientId, RootContentItemId = rootContentItem.Id },
                User.Identity.Name,
                HttpContext.Session.Id
                );
            AuditLogger.Log(rootContentItemUpdatedEvent);
            #endregion

            RootContentItemSummary model = RootContentItemSummary.Build(DbContext, currentRootContentItem);

            return Json(model);
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRootContentItem(long rootContentItemId)
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
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to delete root content item without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentPublisher]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { ClientId = rootContentItem.ClientId, RootContentItemId = rootContentItem.Id },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to administer the specified root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            DbContext.RootContentItem.Remove(rootContentItem);
            DbContext.SaveChanges();

            #region Log audit event(s)
            AuditEvent rootContentItemDeletedEvent = AuditEvent.New(
                $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                "Root content item deleted",
                AuditEventId.RootContentItemDeleted,
                new { ClientId = rootContentItem.ClientId, RootContentItemId = rootContentItem.Id },
                User.Identity.Name,
                HttpContext.Session.Id
                );
            AuditLogger.Log(rootContentItemDeletedEvent);
            #endregion

            RootContentItemList model = RootContentItemList.Build(DbContext, rootContentItem.Client, await Queries.GetCurrentApplicationUser(User));

            return Json(model);
        }

        [HttpPost]
        public async Task<IActionResult> Publish(PublishRequest Arg)
        {
            AuditEvent AuditLogEvent;
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
                #region Log audit event
                AuditLogEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to queue a publication request without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentPublisher]} role in content item",
                    AuditEventId.Unauthorized,
                    new { UserId = currentApplicationUser.Id, RequestedContentItem = Arg.RootContentItemId },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuditLogEvent);
                #endregion

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

            #region Validation
            // The requested RootContentItem must exist
            if (DbContext.RootContentItem.Count(rc => rc.Id == Arg.RootContentItemId) != 1)
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
                                                              { PublicationStatus.Processing, PublicationStatus.Queued };
            Blocked = DbContext.ContentPublicationRequest
                               .Where(r => r.RootContentItemId == Arg.RootContentItemId)
                               .Any(r => BlockingRequestStatusList.Contains(r.RequestStatus));
            if (Blocked)
            {
                Response.Headers.Add("Warning", "A previous publication is pending for this content.");
                return BadRequest();
            }

            List<ReductionStatusEnum> BlockingTaskStatusList = new List<ReductionStatusEnum>
                                                             { ReductionStatusEnum.Reducing, ReductionStatusEnum.Queued };
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

            // Create the publication request and reduction task(s)
            ContentPublicationRequest NewContentPublicationRequest = new ContentPublicationRequest
            {
                ApplicationUserId = currentApplicationUser.Id,
                RequestStatus = PublicationStatus.Queued,
                PublishRequest = Arg,
                CreateDateTimeUtc = DateTime.UtcNow,
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

            // Log the queued publication request
            #region Log audit event
            AuditLogEvent = AuditEvent.New(
                $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                $"New publication request successfully stored",
                AuditEventId.PublicationQueued,
                new { UserId = currentApplicationUser.Id, RequestId = NewContentPublicationRequest.Id, RootContentItem = NewContentPublicationRequest.RootContentItemId },
                User.Identity.Name,
                HttpContext.Session.Id
                );
            AuditLogger.Log(AuditLogEvent);
            #endregion

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelContentPublicationRequest(long rootContentItemId)
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
                #region Log audit event
                AuditEvent AuthorizationFailedEvent = AuditEvent.New(
                    $"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                    $"Request to cancel content publication request without {ApplicationRole.RoleDisplayNames[RoleEnum.ContentPublisher]} role in root content item",
                    AuditEventId.Unauthorized,
                    new { RootContentItemId = rootContentItem.Id },
                    User.Identity.Name,
                    HttpContext.Session.Id
                    );
                AuditLogger.Log(AuthorizationFailedEvent);
                #endregion

                Response.Headers.Add("Warning", "You are not authorized to cancel content publication requests for this root content item.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            var contentPublicationRequest = DbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == rootContentItem.Id)
                .Where(r => r.RequestStatus == PublicationStatus.Queued)
                .SingleOrDefault();
            if (contentPublicationRequest == null)
            {
                Response.Headers.Add("Warning", "No cancelable requests for this root content item exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            contentPublicationRequest.RequestStatus = PublicationStatus.Canceled;
            DbContext.ContentPublicationRequest.Update(contentPublicationRequest);
            try
            {
                DbContext.SaveChanges();
            }
            catch
            {
                Response.Headers.Add("Warning", "The publication request failed to be canceled.  Processing may have started.");
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
        public async Task<IActionResult> PreLiveSummary(long RootContentItemId)
        {
            // Return a model summarizing the publication summary

            return new JsonResult(new object());
        }
    }
}
