/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing hosted content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Event;
using AuditLogLib.Services;
using MapCommonLib;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.AuthorizedContentViewModels;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.Utilities;
using QlikviewLib;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace MillimanAccessPortal.Controllers
{
    public class AuthorizedContentController : Controller
    {
        // Things provided by the application that this controller should need to use
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ApplicationDbContext DataContext;
        private readonly IMessageQueue MessageQueue;
        private readonly ILogger Logger;
        private readonly QlikviewConfig QlikviewConfig;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly IConfiguration ApplicationConfig;

        /// <summary>
        /// Constructor.  Makes instance copies of injected resources from the application. 
        /// </summary>
        /// <param name="AuditLoggerArg"></param>
        /// <param name="AuthorizationServiceArg"></param>
        /// <param name="DataContextArg"></param>
        /// <param name="LoggerFactoryArg"></param>
        /// <param name="QlikviewOptionsAccessorArg"></param>
        /// <param name="QueryArg"></param>
        /// <param name="UserManagerArg"></param>
        /// <param name="AppConfigurationArg"></param>
        public AuthorizedContentController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ApplicationDbContext DataContextArg,
            IMessageQueue MessageQueueArg,
            ILoggerFactory LoggerFactoryArg,
            IOptions<QlikviewConfig> QlikviewOptionsAccessorArg,
            StandardQueries QueryArg,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration AppConfigurationArg)
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DataContext = DataContextArg;
            MessageQueue = MessageQueueArg;
            Logger = LoggerFactoryArg.CreateLogger<AuthorizedContentController>();
            QlikviewConfig = QlikviewOptionsAccessorArg.Value;
            Queries = QueryArg;
            UserManager = UserManagerArg;
            ApplicationConfig = AppConfigurationArg;
        }

        /// <summary>
        /// Presents the user with links to all authorized content. This is the application landing page.
        /// </summary>
        /// <returns>The view</returns>
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Content()
        {
            var model = AuthorizedContentViewModel.Build(DataContext, await Queries.GetCurrentApplicationUser(User), HttpContext);

            return Json(model);
        }

        /// <summary>
        /// Handles a request to display content that is hosted by a web server. 
        /// </summary>
        /// <param name="selectionGroupId">The primary key value of the SelectionGroup authorizing this user to the requested content</param>
        /// <returns>A View (and model) that displays the requested content</returns>
        [Authorize]
        public async Task<IActionResult> WebHostedContent(Guid selectionGroupId)
        {
            var selectionGroup = DataContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rc => rc.ContentType)
                .Where(sg => sg.Id == selectionGroupId)
                .Where(sg => !sg.IsSuspended)
                .Where(sg => !sg.RootContentItem.IsSuspended)
                .FirstOrDefault();
            #region Validation
            if (selectionGroup?.RootContentItem?.ContentType == null)
            {
                string ErrMsg = $"Failed to obtain the requested selection group, root content item, or content type";
                Logger.LogError(ErrMsg);

                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
                // something that appropriately returns to a logical next view
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new UserInSelectionGroupRequirement(selectionGroupId));
            if (!Result1.Succeeded)
            {
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentUser));

                Response.Headers.Add("Warning", "You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            #region File Verification 

            // Get a reference to the file
            var contentFile = selectionGroup.RootContentItem.ContentFilesList
                .FirstOrDefault(f => f.FullPath.EndsWith(selectionGroup.ContentInstanceUrl));

            if (contentFile == null)
            {
                Response.Headers.Add("Warning", "This content could not be found. Try again in a few minutes, and contact MAP Support if this error continues.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            // Make sure the checksum currently matches the value stored at the time the file was generated or uploaded
            if (!contentFile.ValidateChecksum())
            {
                string ErrMsg = $"The system could not validate the content item {selectionGroup.RootContentItem.ContentName} for selection group {selectionGroup.GroupName}. Try again in a few minutes, and contact MAP Support if this error continues.";
                var notifier = new NotifySupport(MessageQueue, ApplicationConfig);

                notifier.sendSupportMail(ErrMsg);
                AuditLogger.Log(AuditEventType.ChecksumInvalid.ToEvent());
                Response.Headers.Add("Warning", ErrMsg);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            #endregion

            try
            {
                // Instantiate the right content handler class
                ContentTypeSpecificApiBase ContentSpecificHandler = null;
                switch (selectionGroup.RootContentItem.ContentType.TypeEnum)
                {   // Never break out of this switch without a valid ContentSpecificHandler object
                    case ContentTypeEnum.Qlikview:
                        ContentSpecificHandler = new QlikviewLibApi();
                        break;

                    //case ContentTypeEnum.SomeOther":
                    //    ContentSpecificHandler = new AnotherTypeSpecificLib();
                    //    break;

                    default:
                        TempData["Message"] = $"Display of an unsupported ContentType was requested: {selectionGroup.RootContentItem.ContentType.Name}";
                        TempData["ReturnToController"] = "AuthorizedContent";
                        TempData["ReturnToAction"] = "Index";
                        return RedirectToAction(nameof(ErrorController.Error), nameof(ErrorController).Replace("Controller", ""));
                }

                UriBuilder ContentUri = await ContentSpecificHandler.GetContentUri(selectionGroup.ContentInstanceUrl, HttpContext.User.Identity.Name, QlikviewConfig);

                // Now return the appropriate view for the requested content
                switch (selectionGroup.RootContentItem.ContentType.Name)
                {
                    case "Qlikview":
                        return Redirect(ContentUri.Uri.AbsoluteUri);

                    //case "Another web hosted type":
                        //return TheRightThing;

                    default:
                        // Perhaps this can't happen since this case is handled above
                        TempData["Message"] = $"An unsupported ContentType was requested: {selectionGroup.RootContentItem.ContentType.Name}";
                        TempData["ReturnToController"] = "AuthorizedContent";
                        TempData["ReturnToAction"] = "Index";
                        return RedirectToAction(nameof(ErrorController.Error), nameof(ErrorController).Replace("Controller", ""));
                }
            }
            catch (MapException e)
            {
                TempData["Message"] = $"{e.Message}<br>{e.StackTrace}";
                TempData["ReturnToController"] = "AuthorizedContent";
                TempData["ReturnToAction"] = "Index";
                return RedirectToAction(nameof(ErrorController.Error), nameof(ErrorController).Replace("Controller", ""));
            }
        }

        /// <summary>
        /// Handles a request to display a content items associated thumbnail 
        /// </summary>
        /// <param name="selectionGroupId">The primary key value of the SelectionGroup authorizing this user to the requested content</param>
        /// <returns>A View (and model) that displays the requested content</returns>
        [Authorize]
        public IActionResult Thumbnail(Guid selectionGroupId)
        {
            var selectionGroup = DataContext.SelectionGroup
                                            .Include(sg => sg.RootContentItem)
                                                .ThenInclude(rc => rc.ContentType)
                                            .FirstOrDefault(sg => sg.Id == selectionGroupId);

            #region Validation
            if (selectionGroup == null || selectionGroup.RootContentItem == null || selectionGroup.RootContentItem.ContentType == null)
            {
                string Msg = $"Failed to obtain the requested selection group, root content item, or content type";
                Logger.LogError(Msg);
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            try
            {
                ContentRelatedFile contentRelatedThumbnail = selectionGroup.RootContentItem.ContentFilesList.SingleOrDefault(cf => cf.FilePurpose.ToLower() == "thumbnail");

                if (contentRelatedThumbnail != null && System.IO.File.Exists(contentRelatedThumbnail.FullPath))
                {
                    switch (Path.GetExtension(contentRelatedThumbnail.FullPath).ToLower())
                    {
                        case ".png":
                            return File(System.IO.File.OpenRead(contentRelatedThumbnail.FullPath), "image/png");

                        case ".gif":
                            return File(System.IO.File.OpenRead(contentRelatedThumbnail.FullPath), "image/gif");

                        case ".jpg":
                        case ".jpeg":
                            return File(System.IO.File.OpenRead(contentRelatedThumbnail.FullPath), "image/jpeg");

                        default:
                            throw new Exception();
                    }
                }
                else
                {
                    // when the content item has no thumbnail, return the default image for the ContentType
                    return Redirect($"/images/{selectionGroup.RootContentItem.ContentType.DefaultIconName}");
                }
            }
            catch
            {
                // ControllerBase.File does not throw, but the Stream can throw all sorts of things.
                string ErrMsg = $"Failed to obtain image for SelectionGroup {selectionGroupId}";
                Logger.LogError(ErrMsg);
                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
            }
        }

        /// <summary>
        /// Handles a request to display a pre-production thumbnail 
        /// </summary>
        /// <param name="publicationRequestId">The primary key value of the ContentPublicationRequest associated with this request</param>
        /// <returns>A View (and model) that displays the requested content</returns>
        [Authorize]
        public IActionResult ThumbnailPreview(Guid publicationRequestId)
        {
            var PubRequest = DataContext.ContentPublicationRequest
                                        .Include(r => r.RootContentItem)
                                        .FirstOrDefault(r => r.Id == publicationRequestId);

            #region Validation
            if (PubRequest == null || PubRequest.RootContentItem == null || PubRequest.RootContentItem.ContentType == null)
            {
                string Msg = $"Failed to obtain the requested publication request, root content item, or content type";
                Logger.LogError(Msg);
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            try
            {
                ContentRelatedFile contentRelatedThumbnail = PubRequest.LiveReadyFilesObj.SingleOrDefault(cf => cf.FilePurpose.ToLower() == "thumbnail");

                if (contentRelatedThumbnail != null && System.IO.File.Exists(contentRelatedThumbnail.FullPath))
                {
                    switch (Path.GetExtension(contentRelatedThumbnail.FullPath).ToLower())
                    {
                        case ".png":
                            return File(System.IO.File.OpenRead(contentRelatedThumbnail.FullPath), "image/png");

                        case ".gif":
                            return File(System.IO.File.OpenRead(contentRelatedThumbnail.FullPath), "image/gif");

                        case ".jpg":
                        case ".jpeg":
                            return File(System.IO.File.OpenRead(contentRelatedThumbnail.FullPath), "image/jpeg");

                        default:
                            throw new Exception();
                    }
                }
                else
                {
                    // when the content item has no thumbnail, return the default image for the ContentType
                    return Redirect($"/images/{PubRequest.RootContentItem.ContentType.DefaultIconName}");
                }
            }
            catch
            {
                // ControllerBase.File does not throw, but the Stream can throw all sorts of things.
                string ErrMsg = $"Failed to obtain preview image for ContentPublicationRequest {publicationRequestId}";
                Logger.LogError(ErrMsg);
                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
            }
        }

        [Authorize]
        public async Task<IActionResult> RelatedPdf(string purpose, Guid selectionGroupId)
        {
            var selectionGroup = DataContext.SelectionGroup
                                            .Include(sg => sg.RootContentItem)
                                            .FirstOrDefault(sg => sg.Id == selectionGroupId);

            #region Validation
            if (selectionGroup == null || selectionGroup.RootContentItem == null)
            {
                string Msg = $"Failed to obtain the requested selection group or root content item";
                Logger.LogError(Msg);
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new UserInSelectionGroupRequirement(selectionGroupId));
            if (!Result1.Succeeded)
            {
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentUser));

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion


            #region File verification

            ContentRelatedFile contentRelatedPdf = selectionGroup.RootContentItem.ContentFilesList.Single(cf => cf.FilePurpose.ToLower() == purpose.ToLower());

            if (!contentRelatedPdf.ValidateChecksum())
            {
                
                string ErrMsg = $"Failed to load requested {purpose} PDF for SelectionGroup {selectionGroupId}";
                var notifier = new NotifySupport(MessageQueue, ApplicationConfig);

                notifier.sendSupportMail(ErrMsg);
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", ErrMsg);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            #endregion

            try
            {
                FileStream fileStream = System.IO.File.OpenRead(contentRelatedPdf.FullPath);
                return File(fileStream, "application/pdf");
            }
            catch
            {
                string ErrMsg = $"Failed to load requested {purpose} PDF for SelectionGroup {selectionGroupId}";
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", ErrMsg);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [Authorize]
        public async Task<IActionResult> PdfPreview(string purpose, Guid publicationRequestId)
        {
            var PubRequest = DataContext.ContentPublicationRequest
                                        .Include(r => r.RootContentItem)
                                        .FirstOrDefault(r => r.Id == publicationRequestId);

            #region Validation
            if (PubRequest == null || PubRequest.RootContentItem == null)
            {
                string Msg = $"Failed to obtain the requested publication request or related root content item";
                Logger.LogError(Msg);
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, PubRequest.RootContentItemId));
            if (!Result1.Succeeded)
            {
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            try
            {
                string FullFilePath = Path.Combine(
                    ApplicationConfig["Storage:ContentItemRootPath"], 
                    PubRequest.RootContentItemId.ToString(), 
                    PubRequest.LiveReadyFilesObj.Single(f => f.FilePurpose.ToLower() == purpose).FullPath
                );
                FileStream fileStream = System.IO.File.OpenRead(FullFilePath);

                return File(fileStream, "application/pdf");
            }
            catch (Exception e)
            {
                string ErrMsg = $"Failed to load requested PDF for RootContentItem {PubRequest.RootContentItemId}";
                Logger.LogError(GlobalFunctions.LoggableExceptionString(e, ErrMsg, true));
                Response.Headers.Add("Warning", ErrMsg);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

    }
}
