/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Controller for actions supporting the content publishing page
 * DEVELOPER NOTES:
 */

using AuditLogLib;
using AuditLogLib.Services;
using MapCommonLib;
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
using System.Linq;
using System.Threading.Tasks;
using System.Linq;

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
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified client.");
                return Unauthorized();
            }
            #endregion

            #region Validation
            #endregion

            RootContentItemList model = RootContentItemList.Build(DbContext, client);

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
                Response.Headers.Add("Warning", "You are not authorized to administer content access to the specified root content item.");
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

            try
            {
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
            }
            catch (Exception ex)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Exception while creating root content item\"{rootContentItem.Id}\"");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", $"Failed to complete transaction.");
                return StatusCode(StatusCodes.Status500InternalServerError);
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

        [HttpDelete]
        //[ValidateAntiForgeryToken]
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

            try
            {
                DbContext.RootContentItem.Remove(rootContentItem);
                DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Exception while deleting root content item \"{rootContentItemId}\"");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", $"Failed to complete transaction.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

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

            RootContentItemList model = RootContentItemList.Build(DbContext, rootContentItem.Client);

            return Json(model);
        }

        [HttpPost]
        public async Task<IActionResult> Publish(PublishRequest Arg)
        {
            #region Preliminary Validation
            // maybe none
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, Arg.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to publish this content");
                return Unauthorized();
            }

            #endregion

            #region Validation
            // The requested RootContentItem must exist
            RootContentItem rootContentItem = DbContext.RootContentItem.SingleOrDefault(rc => rc.Id == Arg.RootContentItemId);
            if (rootContentItem == null)
            {
                Response.Headers.Add("Warning", "Requested content item not found.");
                return BadRequest();
            }

            // All the provided references to related files must be found in the FileUpload entity.  
            if (Arg.RelatedFiles.Any(f => DbContext.FileUpload.Find(f.FileUploadId) == null))
            {
                Response.Headers.Add("Warning", "A requested related file has not been uploaded.");
                return BadRequest();
            }
            #endregion

            // TODO need to know if this is first time publish or replacement. 

            // Create the publication request and reduction task(s)
            /* TODO: correct this section
            ContentPublicationRequest contentPublicationRequest;
            {
                var currentApplicationUser = await Queries.GetCurrentApplicationUser(User);
                contentPublicationRequest = new ContentPublicationRequest
                {
                    ApplicationUserId = currentApplicationUser.Id,
                    MasterFilePath = UploadHelper.GetOutputFilePath(),
                    RootContentItemId = resumableInfo.RootContentItemId,
                };
                DbContext.ContentPublicationRequest.Add(contentPublicationRequest);
                DbContext.SaveChanges();
            }

            // Master selection group is created when root content item is created, so there must always
            // be at least one available selection group.
            // TODO: possibly create master selection group at publication time (here).
            {
                var selectionGroups = DbContext.SelectionGroup
                    .Where(sg => sg.RootContentItemId == resumableInfo.RootContentItemId)
                    .ToList();

                var contentReductionTasks = selectionGroups
                    .Select(sg => new ContentReductionTask
                    {
                        ApplicationUserId = contentPublicationRequest.ApplicationUserId,
                        ContentPublicationRequestId = contentPublicationRequest.Id,
                        SelectionGroupId = sg.Id,
                        MasterFilePath = contentPublicationRequest.MasterFilePath,
                        SelectionCriteria = JsonConvert.SerializeObject(
                            Queries.GetFieldSelectionsForSelectionGroup(sg.Id, sg.SelectedHierarchyFieldValueList)), // TODO: special case when selection group is a master selection group
                    ReductionStatus = ReductionStatusEnum.Validating,
                    });

                DbContext.ContentReductionTask.AddRange(contentReductionTasks);
                DbContext.SaveChanges();
            }
            */
            return Json(new { });
        }
    }
}