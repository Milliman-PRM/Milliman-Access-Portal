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
using MillimanAccessPortal.Models.SharedModels;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.Utilities;
using PowerBiLib;
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
        private readonly PowerBiConfig _powerBiConfig;
        private readonly QlikviewConfig QlikviewConfig;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly IConfiguration ApplicationConfig;
        private readonly AuthorizedContentQueries _authorizedContentQueries;

        /// <summary>
        /// Constructor.  Makes instance copies of injected resources from the application. 
        /// </summary>
        /// <param name="AuditLoggerArg"></param>
        /// <param name="AuthorizationServiceArg"></param>
        /// <param name="DataContextArg"></param>
        /// <param name="LoggerFactoryArg"></param>
        /// <param name="QlikviewOptionsAccessorArg"></param>
        /// <param name="UserManagerArg"></param>
        /// <param name="AppConfigurationArg"></param>
        public AuthorizedContentController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ApplicationDbContext DataContextArg,
            IMessageQueue MessageQueueArg,
            IOptions<QlikviewConfig> QlikviewOptionsAccessorArg,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration AppConfigurationArg,
            IOptions<PowerBiConfig> powerBiConfigArg,
            AuthorizedContentQueries AuthorizedContentQueriesArg)
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DataContext = DataContextArg;
            MessageQueue = MessageQueueArg;
            _powerBiConfig = powerBiConfigArg.Value;
            QlikviewConfig = QlikviewOptionsAccessorArg.Value;
            UserManager = UserManagerArg;
            ApplicationConfig = AppConfigurationArg;
            _authorizedContentQueries = AuthorizedContentQueriesArg;
        }

        /// <summary>
        /// Presents the user with links to all authorized content. This is the application landing page.
        /// </summary>
        /// <returns>The view</returns>
        public IActionResult Index()
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action");

            return View();
        }

        public async Task<IActionResult> Content()
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action");

            var model = await _authorizedContentQueries.GetAuthorizedContentViewModel(HttpContext);

            return Json(model);
        }

        /// <summary>
        /// Return a view that contains either content disclaimer text or the content
        /// </summary>
        public async Task<IActionResult> ContentWrapper(Guid selectionGroupId)
        {
            var currentUser = await UserManager.GetUserAsync(User);
            var userInSelectionGroup = await DataContext.UserInSelectionGroup
                .Include(usg => usg.SelectionGroup)
                    .ThenInclude(sg => sg.RootContentItem)
                        .ThenInclude(c => c.Client)
                .Include(usg => usg.User)
                .Where(usg => usg.UserId == currentUser.Id)
                .Where(usg => usg.SelectionGroupId == selectionGroupId)
                .FirstOrDefaultAsync();
            var selectionGroup = DataContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(i => i.ContentType)
                .Where(sg => sg.Id == selectionGroupId)
                .Where(sg => sg.ContentInstanceUrl != null)
                .Where(sg => !sg.IsSuspended)
                .Where(sg => !sg.RootContentItem.IsSuspended)
                .FirstOrDefault();

            #region Validation
            if (selectionGroup?.RootContentItem == null)
            {
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action, failed to obtain the requested selection group, content item, or content type: " +
                    $"user {User.Identity.Name}, selectionGroupId {selectionGroupId}, aborting");

                return View("UserMessage", new UserMessageModel("You are not authorized to access the requested content."));
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new UserInSelectionGroupRequirement(selectionGroupId));
            if (!Result1.Succeeded)
            {
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, selection group {selectionGroupId}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentUser), currentUser.UserName, currentUser.Id);

                return View("UserMessage", new UserMessageModel("You are not authorized to access the requested content."));
            }
            #endregion

            #region Content Disclaimer Verification
            if (!string.IsNullOrWhiteSpace(selectionGroup.RootContentItem.ContentDisclaimer)
                && !userInSelectionGroup.DisclaimerAccepted)
            {
                var disclaimer = new ContentDisclaimerModel
                {
                    ValidationId = Guid.NewGuid(),
                    SelectionGroupId = selectionGroupId,
                    ContentName = selectionGroup.RootContentItem.ContentName,
                    DisclaimerText = selectionGroup.RootContentItem.ContentDisclaimer,
                };
                AuditLogger.Log(AuditEventType.ContentDisclaimerPresented.ToEvent(
                    userInSelectionGroup, userInSelectionGroup.SelectionGroup.RootContentItem, userInSelectionGroup.SelectionGroup.RootContentItem.Client, disclaimer.ValidationId, disclaimer.DisclaimerText), 
                    currentUser.UserName, currentUser.Id);

                return View("ContentDisclaimer", disclaimer);
            }
            #endregion

            UriBuilder contentUrlBuilder = new UriBuilder
            {
                Host = Request.Host.Host,
                Scheme = Request.Scheme,
                Port = Request.Host.Port ?? -1,
                Path = $"/AuthorizedContent/{nameof(WebHostedContent)}",
            };
            if (Request.QueryString.HasValue)
            {
                contentUrlBuilder.Query = Request.QueryString.Value.Substring(1);
            }

            return View("ContentWrapper", new ContentWrapperModel
            {
                ContentURL = contentUrlBuilder.Uri.AbsoluteUri,
                ContentType = selectionGroup.RootContentItem.ContentType.TypeEnum,
            });
        }

        public async Task<IActionResult> AcceptDisclaimer(Guid selectionGroupId, Guid validationId)
        {
            var user = await UserManager.GetUserAsync(User);
            var userInSelectionGroup = await DataContext.UserInSelectionGroup
                .Include(usg => usg.SelectionGroup)
                    .ThenInclude(sg => sg.RootContentItem)
                        .ThenInclude(c => c.Client)
                .Include(usg => usg.User)
                .Where(usg => usg.UserId == user.Id)
                .Where(usg => usg.SelectionGroupId == selectionGroupId)
                .FirstOrDefaultAsync();

            if (!userInSelectionGroup.DisclaimerAccepted)
            {
                userInSelectionGroup.DisclaimerAccepted = true;

                await DataContext.SaveChangesAsync();
                ApplicationUser currentUser = await UserManager.GetUserAsync(User);
                AuditLogger.Log(AuditEventType.ContentDisclaimerAccepted.ToEvent(userInSelectionGroup, userInSelectionGroup.SelectionGroup.RootContentItem, userInSelectionGroup.SelectionGroup.RootContentItem.Client, validationId), currentUser.UserName, currentUser.Id);
            }

            return Ok();
        }

        /// <summary>
        /// Handles a request to display content that is hosted by a web server. 
        /// </summary>
        /// <param name="selectionGroupId">The primary key value of the SelectionGroup authorizing this user to the requested content</param>
        /// <returns>A View (and model) that displays the requested content</returns>
        public async Task<IActionResult> WebHostedContent(Guid selectionGroupId)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, selectionGroupId {selectionGroupId}");

            var currentUser = await UserManager.GetUserAsync(User);
            var userInSelectionGroup = DataContext.UserInSelectionGroup
                .Where(usg => usg.UserId == currentUser.Id)
                .Where(usg => usg.SelectionGroupId == selectionGroupId)
                .FirstOrDefault();
            var selectionGroup = DataContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rc => rc.ContentType)
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rc => rc.Client)
                .Where(sg => sg.Id == selectionGroupId)
                .Where(sg => sg.ContentInstanceUrl != null)
                .Where(sg => !sg.IsSuspended)
                .Where(sg => !sg.RootContentItem.IsSuspended)
                .FirstOrDefault();

            #region Validation
            if (selectionGroup?.RootContentItem?.ContentType == null)
            {
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action, failed to obtain the requested selection group, content item, or content type: " +
                    $"user {User.Identity.Name}, selectionGroupId {selectionGroupId}, aborting");

                return View("UserMessage", new UserMessageModel("You are not authorized to access the requested content."));
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new UserInSelectionGroupRequirement(selectionGroupId));
            if (!Result1.Succeeded)
            {
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, selection group {selectionGroupId}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentUser), currentUser.UserName, currentUser.Id);

                return View("UserMessage", new UserMessageModel("You are not authorized to access the requested content."));
            }
            #endregion

            #region Validation
            // user must have accepted the content disclaimer if one exists
            if (!string.IsNullOrWhiteSpace(selectionGroup.RootContentItem.ContentDisclaimer)
                && !userInSelectionGroup.DisclaimerAccepted)
            {
                return View("UserMessage", new UserMessageModel("You are not authorized to access the requested content."));
            }

            // This request must be referred by ContentWrapper
            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.Referer == null || !requestHeaders.Referer.AbsolutePath.Contains(nameof(ContentWrapper)))
            {
                UriBuilder contentUrlBuilder = new UriBuilder
                {
                    Host = Request.Host.Host,
                    Scheme = Request.Scheme,
                    Port = Request.Host.Port ?? -1,
                    Path = $"/AuthorizedContent/{nameof(ContentWrapper)}",
                };
                if (Request.QueryString.HasValue)
                {
                    contentUrlBuilder.Query = Request.QueryString.Value.Substring(1);
                }
                Log.Warning($"From {ControllerContext.ActionDescriptor.DisplayName}: Improper request not refered by AuthorizedContentController.{nameof(ContentWrapper)}, redirecting to {contentUrlBuilder.Uri.AbsoluteUri}");
                return Redirect(contentUrlBuilder.Uri.AbsoluteUri);
            }

            // The related client must not be past due for periodic client access review
            if (DateTime.UtcNow > selectionGroup.RootContentItem.Client.LastAccessReview.LastReviewDateTimeUtc
                                + TimeSpan.FromDays(ApplicationConfig.GetValue<int>("ClientReviewRenewalPeriodDays")))
            {
                // TODO email client admins?
                Log.Warning($"From {ControllerContext.ActionDescriptor.DisplayName}: Request for content of client {selectionGroup.RootContentItem.Client.Id} ({selectionGroup.RootContentItem.Client.Name}) with expired client access review");

                return View("UserMessage", new UserMessageModel
                {
                    PrimaryMessages = {
                        "This content cannot be displayed.",
                        "The client's access review is past due and must be performed.",
                        "Please click your browser's \"Back\" button or use a navigation button at the left." }, 
                    Buttons = new List<ConfiguredButton>(),  // remove the default "OK" button
                });
            }
            #endregion

            #region File Verification 
            ContentRelatedFile requestedContentFile = default;
            if (selectionGroup.RootContentItem.ContentType.TypeEnum.LiveContentFileStoredInMap())
            {
                var masterContentRelatedFile = selectionGroup.RootContentItem.ContentFilesList.SingleOrDefault(f => f.FilePurpose.ToLower() == "mastercontent");
                requestedContentFile = selectionGroup.IsMaster
                    ? masterContentRelatedFile
                    : selectionGroup.IsInactive
                        ? null
                        : new ContentRelatedFile
                        {
                            FullPath = Path.Combine(
                                ApplicationConfig["Storage:ContentItemRootPath"],
                                selectionGroup.ContentInstanceUrl),
                            Checksum = selectionGroup.ReducedContentChecksum,
                            FileOriginalName = masterContentRelatedFile.FileOriginalName,
                            FilePurpose = masterContentRelatedFile.FilePurpose,
                        };

                if (requestedContentFile == null)
                {
                    Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: content file path not found for {(selectionGroup.IsMaster ? "master" : "reduced")} " +
                        $"selection group {selectionGroupId}, aborting");

                    return View("UserMessage", new UserMessageModel { PrimaryMessages = {
                        "This content file path could not be found.",
                        "Please refresh this web page (F5) in a few minutes, and contact MAP Support if this error continues." }});
                }

                // Make sure the checksum currently matches the value stored at the time the file went live
                if (!requestedContentFile.ValidateChecksum())
                {
                    var ErrMsg = new string[]
                    {
                        $"The system could not validate the file for content item {selectionGroup.RootContentItem.ContentName}, selection group {selectionGroup.GroupName}.",
                        $"Please contact MAP Support if this error continues.",
                    };
                    string MailMsg = $"The content item below failed checksum validation and may have been altered improperly.{Environment.NewLine}{Environment.NewLine}Time stamp (UTC): {DateTime.UtcNow.ToString()}{Environment.NewLine}Content item: {selectionGroup.RootContentItem.ContentName}{Environment.NewLine}Selection group: {selectionGroup.GroupName}{Environment.NewLine}Client: {selectionGroup.RootContentItem.Client.Name}{Environment.NewLine}User: {HttpContext.User.Identity.Name}";
                    var notifier = new NotifySupport(MessageQueue, ApplicationConfig);

                    notifier.sendSupportMail(MailMsg, "Checksum verification (content item)");
                    Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} action: checksum failure for ContentFile {{@ContentFile}}, aborting", requestedContentFile);
                    AuditLogger.Log(AuditEventType.ChecksumInvalid.ToEvent(
                                        selectionGroup, selectionGroup.RootContentItem, selectionGroup.RootContentItem.Client, requestedContentFile, ControllerContext.ActionDescriptor.DisplayName), 
                                    currentUser.UserName, currentUser.Id);
                    return View("UserMessage", new UserMessageModel(ErrMsg));
                }
            }
            #endregion

            // Log content access
            AuditLogger.Log(AuditEventType.UserContentAccess.ToEvent(
                selectionGroup, 
                selectionGroup.RootContentItem, 
                selectionGroup.RootContentItem.Client), currentUser.UserName, currentUser.Id);

            try
            {
                // Instantiate the right content handler class
                ContentTypeSpecificApiBase ContentSpecificHandler = null;
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action, content type is <{selectionGroup.RootContentItem.ContentType.TypeEnum.GetDisplayNameString()}>");
                switch (selectionGroup.RootContentItem.ContentType.TypeEnum)
                {   // Never break out of this switch without a valid ContentSpecificHandler object
                    case ContentTypeEnum.Qlikview:
                        ContentSpecificHandler = new QlikviewLibApi(QlikviewConfig);
                        UriBuilder QvContentUri = await ContentSpecificHandler.GetContentUri(selectionGroup.ContentInstanceUrl, HttpContext.User.Identity.Name, HttpContext.Request);
                        Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: returning Qlikview URI {QvContentUri.Uri.AbsoluteUri}");
                        return Redirect(QvContentUri.Uri.AbsoluteUri);

                    case ContentTypeEnum.FileDownload:
                        Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: returning file {requestedContentFile.FullPath}");
                        return PhysicalFile(requestedContentFile.FullPath, "application/octet-stream", requestedContentFile.FileOriginalName);

                    case ContentTypeEnum.Pdf:
                        Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: returning file {requestedContentFile.FullPath}");
                        return PhysicalFile(requestedContentFile.FullPath, "application/pdf");

                    case ContentTypeEnum.Html:
                        Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: returning file {requestedContentFile.FullPath}");
                        return PhysicalFile(requestedContentFile.FullPath, "text/html");

                    case ContentTypeEnum.PowerBi:
                        ContentSpecificHandler = new PowerBiLibApi(_powerBiConfig);
                        UriBuilder pbiContentUri = await ContentSpecificHandler.GetContentUri(selectionGroup.Id.ToString(), HttpContext.User.Identity.Name, HttpContext.Request);
                        Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: returning PowerBI URI {pbiContentUri.Uri.AbsoluteUri}");
                        return Redirect(pbiContentUri.Uri.AbsoluteUri);

                    default:
                        Log.Error($"In AuthorizedContentController.WebHostedContent action, unsupported content type <{selectionGroup.RootContentItem.ContentType.TypeEnum.GetDisplayNameString()}>, aborting");
                        return View("UserMessage", new UserMessageModel($"Display of an unsupported ContentType was requested: {selectionGroup.RootContentItem.ContentType.TypeEnum.GetDisplayNameString()}"));
                }

            }
            catch (MapException e)
            {
                Log.Error(e, $"In AuthorizedContentController.WebHostedContent action, failed to obtain content URI, aborting");
                return View("UserMessage", new UserMessageModel(GlobalFunctions.LoggableExceptionString(e, "Exception:", true, true, true)));
            }
        }

        /// <summary>
        /// Display the preview report for the identified publication request
        /// </summary>
        /// <param name="request">A ContentPublicationRequest Id, used to display pre-approved content</param>
        /// <returns></returns>
        public async Task<IActionResult> PowerBiPreview(Guid request)
        {
            ApplicationUser currentUser = await UserManager.GetUserAsync(User);

            #region Authorization
            var PubRequest = DataContext.ContentPublicationRequest
                                        .FirstOrDefault(r => r.Id == request);
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(
                User, null, new RoleInRootContentItemRequirement(
                    RoleEnum.ContentPublisher, PubRequest.RootContentItemId));
            if (!Result1.Succeeded)
            {
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, "
                    + $"content item {PubRequest.RootContentItemId}, role {RoleEnum.ContentPublisher}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher), currentUser.UserName, currentUser.Id);

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            RootContentItem contentItem = await DataContext.ContentPublicationRequest
                                                     .Include(r => r.RootContentItem)
                                                        .ThenInclude(c => c.ContentType)
                                                     .Where(r => r.Id == request)
                                                     .Select(r => r.RootContentItem)
                                                     .SingleOrDefaultAsync();

            PowerBiContentItemProperties embedProperties = contentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;

            PowerBiLibApi api = await new PowerBiLibApi(_powerBiConfig).InitializeAsync();
            PowerBiEmbedModel embedModel = new PowerBiEmbedModel
                {
                    EmbedUrl = embedProperties.PreviewEmbedUrl,
                    EmbedToken = await api.GetEmbedTokenAsync(embedProperties.PreviewWorkspaceId.Value, embedProperties.PreviewReportId.Value, embedProperties.EditableEnabled),
                    ReportId = embedProperties.PreviewReportId.Value,
                    EditableEnabled = embedProperties.EditableEnabled,
                    FilterPaneEnabled = embedProperties.FilterPaneEnabled,
                    NavigationPaneEnabled = embedProperties.NavigationPaneEnabled,
                    BookmarksPaneEnabled = embedProperties.BookmarksPaneEnabled,
                };

            return View("PowerBi", embedModel);
        }

        /// <summary>
        /// Display the live report for the identified SelectionGroup
        /// </summary>
        /// <param name="request">A SelectionGroup Id, used to display content</param>
        /// <returns></returns>
        public async Task<IActionResult> PowerBi(Guid group)
        {
            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new UserInSelectionGroupRequirement(group));
            if (!Result1.Succeeded)
            {
                ApplicationUser currentUser = await UserManager.GetUserAsync(User);

                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, selection group {group}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentUser), currentUser.UserName, currentUser.Id);

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            SelectionGroup selectionGroup = DataContext.SelectionGroup
                                                       .Include(sg => sg.RootContentItem)
                                                           .ThenInclude(rci => rci.ContentType)
                                                       .Where(sg => sg.Id == group)
                                                       .SingleOrDefault();
            if (selectionGroup == null)
            {
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} requested selection group with ID {group} not found");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            PowerBiContentItemProperties embedProperties = selectionGroup.RootContentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;

            try
            {
                PowerBiLibApi api = await new PowerBiLibApi(_powerBiConfig).InitializeAsync();
                PowerBiEmbedModel embedModel = new PowerBiEmbedModel
                {
                    EmbedUrl = embedProperties.LiveEmbedUrl,
                    EmbedToken = await api.GetEmbedTokenAsync(embedProperties.LiveWorkspaceId.Value, embedProperties.LiveReportId.Value, selectionGroup.Editable),
                    ReportId = embedProperties.LiveReportId.Value,
                    EditableEnabled = selectionGroup.Editable,
                    FilterPaneEnabled = embedProperties.FilterPaneEnabled,
                    NavigationPaneEnabled = embedProperties.NavigationPaneEnabled,
                    BookmarksPaneEnabled = embedProperties.BookmarksPaneEnabled,
                };

                return View("PowerBi", embedModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"In {ControllerContext.ActionDescriptor.DisplayName} Exception while building return model");
                throw;
            }
        }

        /// <summary>
        /// Preview a reduced QVW file
        /// </summary>
        /// <param name="publicationRequestId"></param>
        /// <param name="reductionTaskId"></param>
        /// <returns></returns>
        public async Task<IActionResult> ReducedQvwPreview(Guid publicationRequestId, Guid reductionTaskId)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, publicationRequestId {publicationRequestId}, reductionTaskId {reductionTaskId}");

            var ReductionTask = DataContext.ContentReductionTask.FirstOrDefault(t => t.Id == reductionTaskId);
            var PublicationRequest = DataContext.ContentPublicationRequest.FirstOrDefault(r => r.Id == publicationRequestId);

            #region Validation
            if (ReductionTask == null)
            {
                string Msg = $"Failed to obtain the requested reduction task";
                Log.Error($"{ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, reduction task {reductionTaskId} not found, aborting");
                return StatusCode(StatusCodes.Status422UnprocessableEntity, Msg);
            }
            if (PublicationRequest == null)
            {
                string Msg = $"Failed to obtain the requested publication request";
                Log.Error($"{ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, publication request {publicationRequestId} not found, aborting");
                return StatusCode(StatusCodes.Status422UnprocessableEntity, Msg);
            }
            if (ReductionTask.ContentPublicationRequestId != PublicationRequest.Id)
            {
                string Msg = $"The requested publication request is not related to the requested reduction task";
                Log.Error($"{ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, requested publication request {publicationRequestId} is not related to the requested reduction task {reductionTaskId}, aborting");
                return StatusCode(StatusCodes.Status422UnprocessableEntity, Msg);
            }
            if (ReductionTask.ReductionStatus != ReductionStatusEnum.Reduced)  // could also validate pub request status, maybe too much?
            {
                string Msg = $"The requested reduction task does not have the appropriate status for preview";
                Log.Error($"{ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, requested reduction task {reductionTaskId} does not have the appropriate status for preview, aborting");
                return StatusCode(StatusCodes.Status422UnprocessableEntity, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(
                User, null, new RoleInRootContentItemRequirement(
                    RoleEnum.ContentPublisher, PublicationRequest.RootContentItemId));
            if (!Result1.Succeeded)
            {
                ApplicationUser currentUser = await UserManager.GetUserAsync(User);
                Log.Debug($"{ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, "
                    + $"content item {PublicationRequest.RootContentItemId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher), currentUser.UserName, currentUser.Id);

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            try
            {
                string ContentRootPath = ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath");
                string FullFilePath = ReductionTask.ResultFilePath;
                if (!Path.GetExtension(FullFilePath).Equals(".qvw", StringComparison.InvariantCultureIgnoreCase))
                {
                    Log.Error($"{ControllerContext.ActionDescriptor.DisplayName} action: Error, requested QVW file {FullFilePath} has unexpected extension, aborting");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                string Link = Path.GetRelativePath(ContentRootPath, FullFilePath);
                await new QlikviewLibApi(QlikviewConfig).AuthorizeUserDocumentsInFolderAsync(Path.GetDirectoryName(Link), Path.GetFileName(Link));

                UriBuilder QvwUri = await new QlikviewLibApi(QlikviewConfig).GetContentUri(Link, User.Identity.Name, Request);

                Log.Verbose($"{ControllerContext.ActionDescriptor.DisplayName} action: success, content item {PublicationRequest.RootContentItemId}, selection group {ReductionTask.SelectionGroupId}, redirecting");

                return Redirect(QvwUri.Uri.AbsoluteUri);
            }
            catch (Exception e)
            {
                Log.Error(e, $"{ControllerContext.ActionDescriptor.DisplayName} action: exception while redirecting for reduction task {ReductionTask.Id}, aborting");
                Response.Headers.Add("Warning", "Failed to load requested reduced QVW file for preview");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Preview the master content QVW file
        /// </summary>
        /// <param name="publicationRequestId"></param>
        /// <returns></returns>
        public async Task<IActionResult> QvwPreview(Guid publicationRequestId)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, publicationRequestId {publicationRequestId}");

            var PubRequest = DataContext.ContentPublicationRequest
                                        .FirstOrDefault(r => r.Id == publicationRequestId);

            #region Validation
            if (PubRequest == null)
            {
                string Msg = $"Failed to obtain the requested publication request";
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: user {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(
                User, null, new RoleInRootContentItemRequirement(
                    RoleEnum.ContentPublisher, PubRequest.RootContentItemId));
            if (!Result1.Succeeded)
            {
                ApplicationUser currentUser = await UserManager.GetUserAsync(User);
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, "
                    + $"content item {PubRequest.RootContentItemId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher), currentUser.UserName, currentUser.Id);

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            try
            {
                string ContentRootPath = ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath");
                string FullFilePath = PubRequest.LiveReadyFilesObj
                    .Single(f => f.FilePurpose.ToLower() == "mastercontent")
                    .FullPath;
                if (!Path.GetExtension(FullFilePath).Equals(".qvw", StringComparison.InvariantCultureIgnoreCase))
                {
                    Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: Error, requested QVW file {FullFilePath} has unexpected extension, aborting");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                string Link = Path.GetRelativePath(ContentRootPath, FullFilePath);
                await new QlikviewLibApi(QlikviewConfig).AuthorizeUserDocumentsInFolderAsync(Path.GetDirectoryName(Link), Path.GetFileName(Link));

                UriBuilder QvwUri = await new QlikviewLibApi(QlikviewConfig).GetContentUri(Link, User.Identity.Name, Request);

                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: success, redirecting");

                return Redirect(QvwUri.Uri.AbsoluteUri);
            }
            catch (Exception e)
            {
                Log.Error(e, $"In {ControllerContext.ActionDescriptor.DisplayName} action: exception while redirecting for publication request {PubRequest.Id}, aborting");
                Response.Headers.Add("Warning", "Failed to load requested master qvw file for preview");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Handles a request to display a content item's related thumbnail 
        /// </summary>
        /// <param name="rootContentItemId">The primary key value of the RootContentItem</param>
        /// <returns>A View (and model) that displays the requested content</returns>
        public IActionResult Thumbnail(Guid rootContentItemId)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, rootContentItemId {rootContentItemId}");

            var rootContentItem = DataContext.RootContentItem
                                            .Include(rc => rc.ContentType)
                                            .FirstOrDefault(rc => rc.Id == rootContentItemId);

            #region Validation
            if (rootContentItem == null || rootContentItem.ContentType == null)
            {
                string Msg = "Failed to obtain the requested content item or content type";
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: {Msg}, aborting");
                return StatusCode(StatusCodes.Status422UnprocessableEntity, Msg);
            }
            #endregion

            try
            {
                ContentRelatedFile contentRelatedThumbnail = rootContentItem.ContentFilesList.SingleOrDefault(cf => cf.FilePurpose.ToLower() == "thumbnail");

                if (contentRelatedThumbnail != null && System.IO.File.Exists(contentRelatedThumbnail.FullPath))
                {
                    Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: attempting to return image file <{contentRelatedThumbnail.FullPath}>");
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
                            Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: unsupported file extension <{contentRelatedThumbnail.FullPath}> encountered, aborting");
                            throw new Exception();
                    }
                }
                else
                {
                    // when a content item thumbnail file is specified but the file is not found, return the default image for the ContentType
                    Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} action: specified image file <{contentRelatedThumbnail.FullPath}> not found, using default icon <{rootContentItem.ContentType.DefaultIconName}> for content type <{rootContentItem.ContentType.TypeEnum.GetDisplayNameString()}>, aborting");
                    return Redirect($"/images/{rootContentItem.ContentType.DefaultIconName}");
                }
            }
            catch
            {
                // ControllerBase.File does not throw, but the Stream can throw all sorts of things.
                string ErrMsg = $"Failed to obtain thumbnail for RootcontentItem {rootContentItemId}";
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: {ErrMsg}, aborting");
                return StatusCode(StatusCodes.Status422UnprocessableEntity, ErrMsg);
            }
        }

        /// <summary>
        /// Handles a request to display a pre-production thumbnail 
        /// </summary>
        /// <param name="publicationRequestId">The primary key value of the ContentPublicationRequest for this action</param>
        /// <returns>A View (and model) that displays the requested content</returns>
        public IActionResult ThumbnailPreview(Guid publicationRequestId)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, publicationRequestId {publicationRequestId}");

            var PubRequest = DataContext.ContentPublicationRequest
                                        .Include(r => r.RootContentItem)
                                            .ThenInclude(c => c.ContentType)
                                        .FirstOrDefault(r => r.Id == publicationRequestId);

            #region Validation
            if (PubRequest == null || PubRequest.RootContentItem == null || PubRequest.RootContentItem.ContentType == null)
            {
                string Msg = $"Failed to obtain the requested publication request, content item, or content type";
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            try
            {
                ContentRelatedFile contentRelatedThumbnail = PubRequest.LiveReadyFilesObj.SingleOrDefault(cf => cf.FilePurpose.ToLower() == "thumbnail");

                if (contentRelatedThumbnail != null && System.IO.File.Exists(contentRelatedThumbnail.FullPath))
                {
                    Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: attempting to return image file <{contentRelatedThumbnail.FullPath}>");
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
                            Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: unsupported file extension <{contentRelatedThumbnail.FullPath}> encountered, aborting");
                            throw new Exception();
                    }
                }
                else
                {
                    // when the content item has no thumbnail, return the default image for the ContentType
                    Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} action: specified image file <{contentRelatedThumbnail.FullPath}> not found, using default icon <{PubRequest.RootContentItem.ContentType.DefaultIconName}> for content type <{PubRequest.RootContentItem.ContentType.TypeEnum.GetDisplayNameString()}>, aborting");
                    return Redirect($"/images/{PubRequest.RootContentItem.ContentType.DefaultIconName}");
                }
            }
            catch
            {
                // ControllerBase.File does not throw, but the Stream can throw all sorts of things.
                string ErrMsg = $"Failed to obtain preview image for ContentPublicationRequest {publicationRequestId}";
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: {ErrMsg}, aborting");
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
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, purpose {purpose}, selectionGroupId {selectionGroupId}");

            ApplicationUser currentUser = await UserManager.GetUserAsync(User);
            var selectionGroup = DataContext.SelectionGroup
                                            .Include(sg => sg.RootContentItem)
                                                .ThenInclude(rc => rc.Client)
                                            .FirstOrDefault(sg => sg.Id == selectionGroupId);

            #region Validation
            if (selectionGroup == null || selectionGroup.RootContentItem == null)
            {
                string Msg = $"Failed to obtain the requested selection group or content item";
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new UserInSelectionGroupRequirement(selectionGroupId));
            if (!Result1.Succeeded)
            {
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, selection group {selectionGroupId}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentUser), currentUser.UserName, currentUser.Id);

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
                AuditLogger.Log(AuditEventType.ChecksumInvalid.ToEvent(selectionGroup, selectionGroup.RootContentItem, selectionGroup.RootContentItem.Client, contentRelatedPdf, "AuthorizedContentController.RelatedPdf"), 
                                currentUser.UserName, currentUser.Id);
                return View("UserMessage", new UserMessageModel { PrimaryMessages = ErrMsg });
            }
            #endregion

            // Log access to related file
            AuditLogger.Log(AuditEventType.UserContentRelatedFileAccess.ToEvent(
                    selectionGroup, 
                    selectionGroup.RootContentItem, 
                    selectionGroup.RootContentItem.Client,
                    purpose), currentUser.UserName, currentUser.Id);

            try
            {
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: success, returning file {contentRelatedPdf.FullPath}");
                return PhysicalFile(contentRelatedPdf.FullPath, "application/pdf");
            }
            catch
            {
                string ErrMsg = $"Failed to load requested {purpose} PDF {contentRelatedPdf.FullPath} for SelectionGroup {selectionGroupId}";
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: {ErrMsg}, aborting");
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
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, purpose {purpose}, publicationRequestId {publicationRequestId}");

            var PubRequest = DataContext.ContentPublicationRequest
                                        .FirstOrDefault(r => r.Id == publicationRequestId);

            #region Validation
            if (PubRequest == null)
            {
                string Msg = $"Failed to obtain the requested publication request";
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: user {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, PubRequest.RootContentItemId));
            if (!Result1.Succeeded)
            {
                ApplicationUser currentUser = await UserManager.GetUserAsync(User);
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, content item {PubRequest.RootContentItemId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher), currentUser.UserName, currentUser.Id);

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            try
            {
                string FullFilePath = PubRequest.LiveReadyFilesObj.Single(f => f.FilePurpose.ToLower() == purpose).FullPath;
                if (System.IO.Path.GetExtension(FullFilePath).ToLower() != ".pdf")
                {
                    Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: Error, requested PDF file {FullFilePath} has unexpected extension, aborting");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: success, returning file {FullFilePath}");

                return PhysicalFile(FullFilePath, "application/pdf");
            }
            catch (Exception e)
            {
                Log.Error(e, $"In {ControllerContext.ActionDescriptor.DisplayName} action: exception while returning PhysicalFile for publication request {PubRequest.Id}, aborting");
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
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, publicationRequestId {publicationRequestId}");

            var PubRequest = DataContext.ContentPublicationRequest
                                        .FirstOrDefault(r => r.Id == publicationRequestId);

            #region Validation
            if (PubRequest == null)
            {
                string Msg = $"Failed to obtain the requested publication request";
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: user {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, PubRequest.RootContentItemId));
            if (!Result1.Succeeded)
            {
                ApplicationUser currentUser = await UserManager.GetUserAsync(User);
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, content item {PubRequest.RootContentItemId}, role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher), currentUser.UserName, currentUser.Id);

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            try
            {
                string FullFilePath = PubRequest.LiveReadyFilesObj.Single(f => f.FilePurpose.ToLower() == "mastercontent").FullPath;

                if (!System.IO.Path.GetExtension(FullFilePath).ToLower().Contains(".htm"))  // could be htm or html
                {
                    Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: Error, requested HTML file {FullFilePath} has unexpected extension, aborting");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: success, returning file {FullFilePath}");

                return PhysicalFile(FullFilePath, "text/html");
            }
            catch (Exception e)
            {
                Log.Error(e, $"In {ControllerContext.ActionDescriptor.DisplayName} action: exception while returning PhysicalFile for publication request {PubRequest.Id}, aborting");
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
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, publicationRequestId {publicationRequestId}");

            var PubRequest = DataContext.ContentPublicationRequest
                                        .FirstOrDefault(r => r.Id == publicationRequestId);

            #region Validation
            if (PubRequest == null)
            {
                string Msg = $"Failed to obtain the requested publication request";
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: user {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(
                User, null, new RoleInRootContentItemRequirement(
                    RoleEnum.ContentPublisher, PubRequest.RootContentItemId));
            if (!Result1.Succeeded)
            {
                ApplicationUser currentUser = await UserManager.GetUserAsync(User);
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, content item {PubRequest.RootContentItemId}, "
                          + $"role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher), currentUser.UserName, currentUser.Id);

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            try
            {
                var contentFile = PubRequest.LiveReadyFilesObj
                    .Single(f => f.FilePurpose.ToLower() == "mastercontent");

                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: success, returning file {contentFile.FullPath}");

                return PhysicalFile(contentFile.FullPath, "pbix");
            }
            catch (Exception e)
            {
                Log.Error(e, $"In {ControllerContext.ActionDescriptor.DisplayName} action: exception while returning PhysicalFile for publication request {PubRequest.Id}, aborting");
                Response.Headers.Add("Warning", "Failed to load requested master file download for preview");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Handles a request to preview a ContentAssociatedFile
        /// </summary>
        /// <param name="publicationRequestId"></param>
        /// <returns></returns>
        public async Task<IActionResult> AssociatedFilePreview(Guid publicationRequestId, Guid fileId)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, publicationRequestId {publicationRequestId}, fileId {fileId}");

            var PubRequest = await DataContext.ContentPublicationRequest.SingleOrDefaultAsync(r => r.Id == publicationRequestId);

            #region Preliminary validation
            if (PubRequest == null)
            {
                string Msg = $"Failed to obtain the requested publication request";
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(
                User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, PubRequest.RootContentItemId));
            if (!Result1.Succeeded)
            {
                ApplicationUser currentUser = await UserManager.GetUserAsync(User);
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, content item {PubRequest.RootContentItemId}, "
                          + $"role {RoleEnum.ContentPublisher.ToString()}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentPublisher), currentUser.UserName, currentUser.Id);

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            ContentAssociatedFile caf = PubRequest.LiveReadyAssociatedFilesList.SingleOrDefault(f => f.Id == fileId);

            #region Validation
            if (caf == null)
            {
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName}: error, no file with the requested id, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            if (!System.IO.File.Exists(caf.FullPath) ||
                !caf.ValidateChecksum())
            {
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName}: error, file {caf.FullPath} not found or failed checksum, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            #endregion

            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: returning file {caf.FullPath}");
            switch (caf.FileType)
            {
                case ContentAssociatedFileType.Html:
                    return PhysicalFile(caf.FullPath, "text/html");

                case ContentAssociatedFileType.Pdf:
                    return PhysicalFile(caf.FullPath, "application/pdf");

                case ContentAssociatedFileType.FileDownload:
                    return PhysicalFile(caf.FullPath, "application/octet-stream", caf.FileOriginalName);

                default:
                    string Msg = $"Request was for associated file of unsupported type {caf.FileType} ({caf.FileType.ToString()})";
                    Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: {Msg}, aborting");
                    return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Handles a request to view a live ContentAssociatedFile
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public async Task<IActionResult> AssociatedFile(Guid selectionGroupId, Guid fileId)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action: user {User.Identity.Name}, "
                      + $"selectionGroupId {selectionGroupId}, fileId {fileId}");

            RootContentItem contentItem = await DataContext.SelectionGroup
                .Where(g => g.Id == selectionGroupId)
                .Select(sg => sg.RootContentItem)
                .SingleOrDefaultAsync();

            #region Preliminary validation
            if (contentItem == null)
            {
                string Msg = $"Failed to find the requested content item";
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: {Msg}, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError, Msg);
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(
                User, null, new UserInSelectionGroupRequirement(selectionGroupId));
            if (!Result1.Succeeded)
            {
                ApplicationUser currentUser = await UserManager.GetUserAsync(User);
                Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, selection group {selectionGroupId}, aborting");
                AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.ContentUser), currentUser.UserName, currentUser.Id);

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            ContentAssociatedFile caf = contentItem.AssociatedFilesList.SingleOrDefault(f => f.Id == fileId);

            #region Validation
            if (caf == null)
            {
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName}: error, no file with the requested id, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            if (!System.IO.File.Exists(caf.FullPath) ||
                !caf.ValidateChecksum())
            {
                Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName}: error, file {caf.FullPath} not found or failed checksum, aborting");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            #endregion

            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: returning file {caf.FullPath}");
            switch (caf.FileType)
            {
                case ContentAssociatedFileType.Html:
                    return PhysicalFile(caf.FullPath, "text/html");

                case ContentAssociatedFileType.Pdf:
                    return PhysicalFile(caf.FullPath, "application/pdf");

                case ContentAssociatedFileType.FileDownload:
                    return PhysicalFile(caf.FullPath, "application/octet-stream", caf.FileOriginalName);

                default:
                    string Msg = $"Request was for associated file of unsupported type {caf.FileType} ({caf.FileType.ToString()})";
                    Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: {Msg}, aborting");
                    return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
