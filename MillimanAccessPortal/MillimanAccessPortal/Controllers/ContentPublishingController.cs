/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Controller for actions supporting the content publishing page
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Authorization;
using AuditLogLib.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Net.Http.Headers;
using System.IO;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Text;
using MillimanAccessPortal.Models.ContentPublicationViewModels;
using MillimanAccessPortal.Services;

namespace MillimanAccessPortal.Controllers
{
    public class ContentPublishingController : Controller
    {
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ApplicationDbContext DbContext;
        private readonly IUploadHelper UploadHelper;
        private readonly ILogger Logger;
        private readonly StandardQueries Queries;


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
            IUploadHelper UploadHelperArg,
            ILoggerFactory LoggerFactoryArg,
            StandardQueries QueriesArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DbContext = ContextArg;
            UploadHelper = UploadHelperArg;
            Logger = LoggerFactoryArg.CreateLogger<ContentPublishingController>(); ;
            Queries = QueriesArg;
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
        public ActionResult ChunkStatus(ResumableInfo resumableInfo)
        {
            return UploadHelper.GetChunkReceived(resumableInfo, resumableInfo.ChunkNumber)
                ? ((ActionResult) Ok())
                : NoContent();
        }

        public async Task<IActionResult> Upload()
        {
            #region Model binding
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }

            // Used to accumulate all the form url encoded key value pairs in the request.
            var formAccumulator = new KeyValueAccumulator();

            // The encapsulation boundary should never exceed 70 characters.
            // See https://tools.ietf.org/html/rfc2046#section-5.1.1
            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                lengthLimit: 70);
            var reader = new MultipartReader(boundary, Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        using (var tempFileStream = UploadHelper.OpenTempFile())
                        {
                            await section.Body.CopyToAsync(tempFileStream);
                        }
                    }
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        // Do not limit the key name length here because the
                        // multipart headers length limit is already in effect.
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                        var encoding = MultipartRequestHelper.GetEncoding(section);
                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 1024,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();
                            if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = String.Empty;
                            }
                            formAccumulator.Append(key.Value, value);

                            // This action expects at most 10 values
                            if (formAccumulator.ValueCount > 10)
                            {
                                throw new InvalidDataException("Form key count limit 10 exceeded.");
                            }
                        }
                    }
                }

                // Drains any remaining section body that has not been consumed and
                // reads the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }

            // Bind form data to a model
            var resumableInfo = new ResumableInfo();
            var formValueProvider = new FormValueProvider(
                BindingSource.Form,
                new FormCollection(formAccumulator.GetResults()),
                CultureInfo.CurrentCulture);

            // All required model attributes must be present for binding to succeed
            var bindingSuccessful = await TryUpdateModelAsync(resumableInfo, prefix: "",
                valueProvider: formValueProvider);
            if (!bindingSuccessful)
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
            }
            #endregion

            return await Publish(resumableInfo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(ResumableInfo resumableInfo)
        {
            #region Preliminary Validation
            RootContentItem rootContentItem = DbContext.RootContentItem.SingleOrDefault(rc => rc.Id == resumableInfo.RootContentItemId);
            if (rootContentItem == null)
            {
                Response.Headers.Add("Warning", "Requested content item not found.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            AuthorizationResult RoleInRootContentItemResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, resumableInfo.RootContentItemId));
            if (!RoleInRootContentItemResult.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to publish this content");
                return Unauthorized();
            }
            #endregion

            #region Validation
            // TODO: add additional validation
            #endregion

            int? returnStatus = UploadHelper.ProcessUpload(resumableInfo);

            if (returnStatus.HasValue)
            {
                Response.Headers.Add("Warning", $"{returnStatus.Value}"); // TODO: Return meaningful messages
                return new StatusCodeResult(returnStatus.Value);
            }

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

    public static class MultipartRequestHelper
    {
        public static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary);
            if (string.IsNullOrWhiteSpace(boundary.Value))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            if (boundary.Length > lengthLimit)
            {
                throw new InvalidDataException(
                    $"Multipart boundary length limit {lengthLimit} exceeded.");
            }

            return boundary.Value;
        }

        public static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool HasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && string.IsNullOrEmpty(contentDisposition.FileName.Value)
                   && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
        }

        public static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                       || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
        }

        public static Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out MediaTypeHeaderValue mediaType);
            // Use UTF-8 instead of UTF-7 if it is requested
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }
    }
}