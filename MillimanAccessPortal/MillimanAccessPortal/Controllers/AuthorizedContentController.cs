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
using Microsoft.Extensions.Options;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.AuthorizedContentViewModels;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.Utilities;
using QlikviewLib;
using Serilog;
using System;
using System.Collections.Generic;
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
            IOptions<QlikviewConfig> QlikviewOptionsAccessorArg,
            StandardQueries QueryArg,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration AppConfigurationArg)
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DataContext = DataContextArg;
            MessageQueue = MessageQueueArg;
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
            Log.Verbose("Entered AuthorizedContentController.Index action");

            return View();
        }

        public async Task<IActionResult> Content()
        {
            Log.Verbose("Entered AuthorizedContentController.Content action");

            var model = AuthorizedContentViewModel.Build(DataContext, await Queries.GetCurrentApplicationUser(User), HttpContext);

            return Json(model);
        }

        /// <summary>
        /// Handles a request to display content that is hosted by a web server. 
        /// </summary>
        /// <param name="selectionGroupId">The primary key value of the SelectionGroup authorizing this user to the requested content</param>
        /// <returns>A View (and model) that displays the requested content</returns>
        public async Task<IActionResult> WebHostedContent(Guid selectionGroupId)
        {
            Log.Verbose($"Entered AuthorizedContentController.WebHostedContent action: user {User.Identity.Name}, selectionGroupId {selectionGroupId}");

            var selectionGroup = DataContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rc => rc.ContentType)
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rc => rc.Client)
                .Where(sg => sg.Id == selectionGroupId)
                .Where(sg => !sg.IsSuspended)
                .Where(sg => !sg.RootContentItem.IsSuspended)
                .FirstOrDefault();
            #region Validation
            if (selectionGroup?.RootContentItem?.ContentType == null)
            {
                string ErrMsg = $"In AuthorizedContentController.WebHostedContent action, failed to obtain the requested selection group, content item, or content type";
                Log.Error(ErrMsg + $": user {User.Identity.Name}, selectionGroupId {selectionGroupId}, aborting");

                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new UserInSelectionGroupRequirement(selectionGroupId));
            if (!Result1.Succeeded)
            {
                Log.Verbose($"In AuthorizedContentController.WebHostedContent action: authorization failed for user {User.Identity.Name}, selection group {selectionGroupId}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentUser));

                Response.Headers.Add("Warning", "You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            #region File Verification 
            var masterContentRelatedFile = selectionGroup.RootContentItem.ContentFilesList.SingleOrDefault(f => f.FilePurpose.ToLower() == "mastercontent");
            var requestedContentFile = selectionGroup.IsMaster
                ? masterContentRelatedFile
                : new ContentRelatedFile
                    {
                        FullPath = Path.Combine(ApplicationConfig["Storage:ContentItemRootPath"], selectionGroup.ContentInstanceUrl),
                        Checksum = selectionGroup.ReducedContentChecksum,
                        FileOriginalName = masterContentRelatedFile.FileOriginalName,
                    };

            if (requestedContentFile == null)
            {
                Log.Error($"In AuthorizedContentController.WebHostedContent action: content file path not found for {(selectionGroup.IsMaster ? "master" : "reduced")} selection group {selectionGroupId}, aborting");
                Response.Headers.Add("Warning", "This content file path could not be found. Please refresh this web page (F5) in a few minutes, and contact MAP Support if this error continues.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            // Make sure the checksum currently matches the value stored at the time the file went live
            if (!requestedContentFile.ValidateChecksum())
            {
                var ErrMsg = new List<string>
                {
                    $"The system could not validate the file for content item {selectionGroup.RootContentItem.ContentName}, selection group {selectionGroup.GroupName}.",
                    $"Please contact MAP Support if this error continues.",
                };
                string MailMsg = $"The content item below failed checksum validation and may have been altered improperly.{Environment.NewLine}{Environment.NewLine}Content item: {selectionGroup.RootContentItem.ContentName}{Environment.NewLine}Selection group: {selectionGroup.GroupName}{Environment.NewLine}Client: {selectionGroup.RootContentItem.Client.Name}{Environment.NewLine}User: {HttpContext.User.Identity.Name}";
                var notifier = new NotifySupport(MessageQueue, ApplicationConfig);

                notifier.sendSupportMail(MailMsg, "Checksum verification (content item)");
                Log.Warning("In AuthorizedContentController.WebHostedContent action: checksum failure for ContentFile {@ContentFile}, aborting", requestedContentFile);
                AuditLogger.Log(AuditEventType.ChecksumInvalid.ToEvent());
                return View("ContentMessage", ErrMsg);
            }
            #endregion

            try
            {
                // Instantiate the right content handler class
                ContentTypeSpecificApiBase ContentSpecificHandler = null;
                Log.Verbose($"In AuthorizedContentController.WebHostedContent action, content type is <{selectionGroup.RootContentItem.ContentType.Name}>");
                switch (selectionGroup.RootContentItem.ContentType.TypeEnum)
                {   // Never break out of this switch without a valid ContentSpecificHandler object
                    case ContentTypeEnum.Qlikview:
                        ContentSpecificHandler = new QlikviewLibApi();
                        UriBuilder ContentUri = await ContentSpecificHandler.GetContentUri(selectionGroup.ContentInstanceUrl, HttpContext.User.Identity.Name, QlikviewConfig, HttpContext.Request);
                        Log.Verbose($"In AuthorizedContentController.WebHostedContent action: returning Qlikview URI {ContentUri}");
                        return Redirect(ContentUri.Uri.AbsoluteUri);

                    case ContentTypeEnum.FileDownload:
                        Log.Verbose($"In AuthorizedContentController.WebHostedContent action: returning file {requestedContentFile.FullPath}");
                        return PhysicalFile(requestedContentFile.FullPath, "application/octet-stream", requestedContentFile.FileOriginalName);

                    case ContentTypeEnum.Pdf:
                        Log.Verbose($"In AuthorizedContentController.WebHostedContent action: returning file {requestedContentFile.FullPath}");
                        return PhysicalFile(requestedContentFile.FullPath, "application/pdf");

                    case ContentTypeEnum.Html:
                        Log.Verbose($"In AuthorizedContentController.WebHostedContent action: returning file {requestedContentFile.FullPath}");
                        return PhysicalFile(requestedContentFile.FullPath, "text/html");

                    default:
                        Log.Error($"In AuthorizedContentController.WebHostedContent action, unsupported content type <{selectionGroup.RootContentItem.ContentType.Name}>, aborting");
                        TempData["Message"] = $"Display of an unsupported ContentType was requested: {selectionGroup.RootContentItem.ContentType.Name}";
                        TempData["ReturnToController"] = "AuthorizedContent";
                        TempData["ReturnToAction"] = "Index";
                        return RedirectToAction(nameof(ErrorController.Error), nameof(ErrorController).Replace("Controller", ""));
                }

            }
            catch (MapException e)
            {
                Log.Error(e, $"In AuthorizedContentController.WebHostedContent action, failed to obtain content URI, aborting");
                TempData["Message"] = GlobalFunctions.LoggableExceptionString(e, "Exception:", true, true, true);
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
        public IActionResult Thumbnail(Guid selectionGroupId)
        {
            Log.Verbose($"Entered AuthorizedContentController.Thumbnail action: user {User.Identity.Name}, selectionGroupId {selectionGroupId}");

            var selectionGroup = DataContext.SelectionGroup
                                            .Include(sg => sg.RootContentItem)
                                                .ThenInclude(rc => rc.ContentType)
                                            .FirstOrDefault(sg => sg.Id == selectionGroupId);

            #region Validation
            if (selectionGroup == null || selectionGroup.RootContentItem == null || selectionGroup.RootContentItem.ContentType == null)
            {
                string Msg = "Failed to obtain the requested selection group, content item, or content type";
                Log.Error($"In AuthorizedContentController.Thumbnail action: {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            try
            {
                ContentRelatedFile contentRelatedThumbnail = selectionGroup.RootContentItem.ContentFilesList.SingleOrDefault(cf => cf.FilePurpose.ToLower() == "thumbnail");

                if (contentRelatedThumbnail != null && System.IO.File.Exists(contentRelatedThumbnail.FullPath))
                {
                    Log.Verbose($"In AuthorizedContentController.Thumbnail action: attempting to return image file <{contentRelatedThumbnail.FullPath}>");
                    switch (Path.GetExtension(contentRelatedThumbnail.FullPath).ToLower())
                    {
                        case ".png":
                            return PhysicalFile(contentRelatedThumbnail.FullPath, "image/png");

                        case ".gif":
                            return PhysicalFile(contentRelatedThumbnail.FullPath, "image/gif");

                        case ".jpg":
                        case ".jpeg":
                            return PhysicalFile(contentRelatedThumbnail.FullPath, "image/jpeg");

                        default:
                            Log.Error($"In AuthorizedContentController.Thumbnail action: unsupported file extension <{contentRelatedThumbnail.FullPath}> encountered, aborting");
                            throw new Exception();
                    }
                }
                else
                {
                    // when a content item thumbnail file is specified but the file is not found, return the default image for the ContentType
                    Log.Warning($"In AuthorizedContentController.Thumbnail action: specified image file <{contentRelatedThumbnail.FullPath}> not found, using default icon <{selectionGroup.RootContentItem.ContentType.DefaultIconName}> for content type <{selectionGroup.RootContentItem.ContentType.Name}>, aborting");
                    return Redirect($"/images/{selectionGroup.RootContentItem.ContentType.DefaultIconName}");
                }
            }
            catch
            {
                // ControllerBase.File does not throw, but the Stream can throw all sorts of things.
                string ErrMsg = $"Failed to obtain image for SelectionGroup {selectionGroupId}";
                Log.Error($"In AuthorizedContentController.Thumbnail action: {ErrMsg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
            }
        }

        /// <summary>
        /// Handles a request to display a pre-production thumbnail 
        /// </summary>
        /// <param name="publicationRequestId">The primary key value of the ContentPublicationRequest associated with this request</param>
        /// <returns>A View (and model) that displays the requested content</returns>
        public IActionResult ThumbnailPreview(Guid publicationRequestId)
        {
            Log.Verbose($"Entered AuthorizedContentController.ThumbnailPreview action: user {User.Identity.Name}, publicationRequestId {publicationRequestId}");

            var PubRequest = DataContext.ContentPublicationRequest
                                        .Include(r => r.RootContentItem)
                                        .FirstOrDefault(r => r.Id == publicationRequestId);

            #region Validation
            if (PubRequest == null || PubRequest.RootContentItem == null || PubRequest.RootContentItem.ContentType == null)
            {
                string Msg = $"Failed to obtain the requested publication request, content item, or content type";
                Log.Error($"In AuthorizedContentController.ThumbnailPreview action: {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            try
            {
                ContentRelatedFile contentRelatedThumbnail = PubRequest.LiveReadyFilesObj.SingleOrDefault(cf => cf.FilePurpose.ToLower() == "thumbnail");

                if (contentRelatedThumbnail != null && System.IO.File.Exists(contentRelatedThumbnail.FullPath))
                {
                    Log.Verbose($"In AuthorizedContentController.ThumbnailPreview action: attempting to return image file <{contentRelatedThumbnail.FullPath}>");
                    switch (Path.GetExtension(contentRelatedThumbnail.FullPath).ToLower())
                    {
                        case ".png":
                            return PhysicalFile(contentRelatedThumbnail.FullPath, "image/png");

                        case ".gif":
                            return PhysicalFile(contentRelatedThumbnail.FullPath, "image/gif");

                        case ".jpg":
                        case ".jpeg":
                            return PhysicalFile(contentRelatedThumbnail.FullPath, "image/jpeg");

                        default:
                            Log.Error($"In AuthorizedContentController.ThumbnailPreview action: unsupported file extension <{contentRelatedThumbnail.FullPath}> encountered, aborting");
                            throw new Exception();
                    }
                }
                else
                {
                    // when the content item has no thumbnail, return the default image for the ContentType
                    Log.Warning($"In AuthorizedContentController.ThumbnailPreview action: specified image file <{contentRelatedThumbnail.FullPath}> not found, using default icon <{PubRequest.RootContentItem.ContentType.DefaultIconName}> for content type <{PubRequest.RootContentItem.ContentType.Name}>, aborting");
                    return Redirect($"/images/{PubRequest.RootContentItem.ContentType.DefaultIconName}");
                }
            }
            catch
            {
                // ControllerBase.File does not throw, but the Stream can throw all sorts of things.
                string ErrMsg = $"Failed to obtain preview image for ContentPublicationRequest {publicationRequestId}";
                Log.Error($"In AuthorizedContentController.Thumbnail action: {ErrMsg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
            }
        }

        /// <summary>
        /// Loads a live PDF file related to main content (e.g. user guide, release notes)
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="selectionGroupId"></param>
        /// <returns></returns>
        public async Task<IActionResult> RelatedPdf(string purpose, Guid selectionGroupId)
        {
            Log.Verbose($"Entered AuthorizedContentController.RelatedPdf action: user {User.Identity.Name}, purpose {purpose}, selectionGroupId {selectionGroupId}");

            var selectionGroup = DataContext.SelectionGroup
                                            .Include(sg => sg.RootContentItem)
                                                .ThenInclude(rc => rc.Client)
                                            .FirstOrDefault(sg => sg.Id == selectionGroupId);

            #region Validation
            if (selectionGroup == null || selectionGroup.RootContentItem == null)
            {
                string Msg = $"Failed to obtain the requested selection group or content item";
                Log.Error($"In AuthorizedContentController.RelatedPdf action: {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new UserInSelectionGroupRequirement(selectionGroupId));
            if (!Result1.Succeeded)
            {
                Log.Verbose($"In AuthorizedContentController.RelatedPdf action: authorization failed for user {User.Identity.Name}, selection group {selectionGroupId}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentUser));

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion


            ContentRelatedFile contentRelatedPdf = selectionGroup.RootContentItem.ContentFilesList.Single(cf => cf.FilePurpose.ToLower() == purpose.ToLower());

            #region File verification
            if (!contentRelatedPdf.ValidateChecksum())
            {

                var ErrMsg = new List<string>
                {
                    $"The system could not validate the checksum of {purpose} PDF file for selection group {selectionGroup.GroupName}.",
                    $"Please contact MAP Support if this error continues.",
                };
                string MailMsg = $"The {purpose} PDF for the below content item failed checksum validation and may have been altered improperly.{Environment.NewLine}{Environment.NewLine}Content item: {selectionGroup.RootContentItem.ContentName}{Environment.NewLine}Selection group: {selectionGroup.GroupName}{Environment.NewLine}Client: {selectionGroup.RootContentItem.Client.Name}{Environment.NewLine}User: {HttpContext.User.Identity.Name}";
                var notifier = new NotifySupport(MessageQueue, ApplicationConfig);

                notifier.sendSupportMail(MailMsg, $"Checksum verification ({purpose})");
                Log.Error("In AuthorizedContentController.RelatedPdf action: file checksum failure, ContentRelatedFile {@ContentRelatedFile}, aborting", contentRelatedPdf);
                AuditLogger.Log(AuditEventType.ChecksumInvalid.ToEvent());
                return View("ContentMessage", ErrMsg);
            }
            #endregion

            try
            {
                Log.Verbose($"In AuthorizedContentController.RelatedPdf action: success, returning file {contentRelatedPdf.FullPath}");
                return PhysicalFile(contentRelatedPdf.FullPath, "application/pdf");
            }
            catch
            {
                string ErrMsg = $"Failed to load requested {purpose} PDF {contentRelatedPdf.FullPath} for SelectionGroup {selectionGroupId}";
                Log.Error($"In AuthorizedContentController.RelatedPdf action: {ErrMsg}, aborting");
                Response.Headers.Add("Warning", ErrMsg);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Handles a request to display a preview of a PDF content or related file
        /// </summary>
        /// <param name="publicationRequestId"></param>
        /// <returns></returns>
        public async Task<IActionResult> PdfPreview(string purpose, Guid publicationRequestId)
        {
            Log.Verbose($"Entered AuthorizedContentController.PdfPreview action: user {User.Identity.Name}, purpose {purpose}, publicationRequestId {publicationRequestId}");

            var PubRequest = DataContext.ContentPublicationRequest
                                        .FirstOrDefault(r => r.Id == publicationRequestId);

            #region Validation
            if (PubRequest == null)
            {
                string Msg = $"Failed to obtain the requested publication request";
                Log.Error($"In AuthorizedContentController.PdfPreview action: user {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, PubRequest.RootContentItemId));
            if (!Result1.Succeeded)
            {
                Log.Verbose($"In AuthorizedContentController.PdfPreview action: authorization failed for user {User.Identity.Name}, content item {PubRequest.RootContentItemId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            try
            {
                string FullFilePath = PubRequest.LiveReadyFilesObj.Single(f => f.FilePurpose.ToLower() == purpose).FullPath;
                if (System.IO.Path.GetExtension(FullFilePath).ToLower() != ".pdf")
                {
                    Log.Error($"In AuthorizedContentController.PdfPreview action: Error, requested PDF file {FullFilePath} has unexpected extension, aborting");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                Log.Verbose($"In AuthorizedContentController.PdfPreview action: success, returning file {FullFilePath}");

                return PhysicalFile(FullFilePath, "application/pdf");
            }
            catch (Exception e)
            {
                Log.Error(e, $"In AuthorizedContentController.PdfPreview action: exception while returning PhysicalFile for publication request {PubRequest.Id}, aborting");
                Response.Headers.Add("Warning", "Failed to load requested master PDF file for preview");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Handles a request to display a preview of a master HTML content file
        /// </summary>
        /// <param name="publicationRequestId"></param>
        /// <returns></returns>
        public async Task<IActionResult> HtmlPreview(Guid publicationRequestId)
        {
            Log.Verbose($"Entered AuthorizedContentController.HtmlPreview action: user {User.Identity.Name}, publicationRequestId {publicationRequestId}");

            var PubRequest = DataContext.ContentPublicationRequest
                                        .FirstOrDefault(r => r.Id == publicationRequestId);

            #region Validation
            if (PubRequest == null)
            {
                string Msg = $"Failed to obtain the requested publication request";
                Log.Error($"In AuthorizedContentController.HtmlPreview action: user {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, PubRequest.RootContentItemId));
            if (!Result1.Succeeded)
            {
                Log.Verbose($"In AuthorizedContentController.HtmlPreview action: authorization failed for user {User.Identity.Name}, content item {PubRequest.RootContentItemId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            try
            {
                string FullFilePath = PubRequest.LiveReadyFilesObj.Single(f => f.FilePurpose.ToLower() == "mastercontent").FullPath;

                if (!System.IO.Path.GetExtension(FullFilePath).ToLower().Contains(".htm"))  // could be htm or html
                {
                    Log.Error($"In AuthorizedContentController.HtmlPreview action: Error, requested HTML file {FullFilePath} has unexpected extension, aborting");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                Log.Verbose($"In AuthorizedContentController.HtmlPreview action: success, returning file {FullFilePath}");

                return PhysicalFile(FullFilePath, "text/html");
            }
            catch (Exception e)
            {
                Log.Error(e, $"In AuthorizedContentController.HtmlPreview action: exception while returning PhysicalFile for publication request {PubRequest.Id}, aborting");
                Response.Headers.Add("Warning", "Failed to load requested master HTML file for preview");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Handles a request to a preview of a download link
        /// </summary>
        /// <param name="publicationRequestId"></param>
        /// <returns></returns>
        public async Task<IActionResult> FileDownloadPreview(Guid publicationRequestId)
        {
            Log.Verbose($"Entered AuthorizedContentController.FileDownloadPreview action: user {User.Identity.Name}, "
                      + $"publicationRequestId {publicationRequestId}");

            var PubRequest = DataContext.ContentPublicationRequest
                                        .FirstOrDefault(r => r.Id == publicationRequestId);

            #region Validation
            if (PubRequest == null)
            {
                string Msg = $"Failed to obtain the requested publication request";
                Log.Error($"In AuthorizedContentController.FileDownloadPreview action: user {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(
                User, null, new RoleInRootContentItemRequirement(
                    RoleEnum.ContentPublisher, PubRequest.RootContentItemId));
            if (!Result1.Succeeded)
            {
                Log.Verbose($"In AuthorizedContentController.FileDownloadPreview action: authorization failed "
                          + $"for user {User.Identity.Name}, content item {PubRequest.RootContentItemId}, "
                          + $"role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher));

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            try
            {
                var contentFile = PubRequest.LiveReadyFilesObj
                    .Single(f => f.FilePurpose.ToLower() == "mastercontent");

                Log.Verbose($"In AuthorizedContentController.FileDownloadPreview action: success, "
                          + $"returning file {contentFile.FullPath}");

                return PhysicalFile(contentFile.FullPath, "application/octet-stream", contentFile.FileOriginalName);
            }
            catch (Exception e)
            {
                Log.Error(e, "In AuthorizedContentController.FileDownloadPreview action: "
                           + "exception while returning PhysicalFile "
                          + $"for publication request {PubRequest.Id}, aborting");
                Response.Headers.Add("Warning", "Failed to load requested master file download for preview");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
