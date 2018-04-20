/*
 * CODE OWNERS: Joseph Sweeney, 
 * OBJECTIVE: Controller for actions supporting arbitrary file upload
 * DEVELOPER NOTES:
 *      This controller is intended to be generic - all use cases where a
 *      file is to be uploaded should be able to use this controller.
 *                  
 *      Code from the following page was referenced when writing this controller:
 *      https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-2.0#uploading-large-files-with-streaming
 */

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
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
    public class FileUploadController : Controller
    {
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly IUploadHelper UploadHelper;
        private readonly ILogger Logger;


        /// <summary>
        /// Constructor, stores local references to injected service instances
        /// </summary>
        /// <param name="AuditLoggerArg"></param>
        /// <param name="AuthorizationServiceArg"></param>
        /// <param name="UploadHelperArg"></param>
        /// <param name="LoggerFactoryArg"></param>
        public FileUploadController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            IUploadHelper UploadHelperArg,
            ILoggerFactory LoggerFactoryArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            UploadHelper = UploadHelperArg;
            Logger = LoggerFactoryArg.CreateLogger<FileUploadController>();
        }

        /// <summary>
        /// Get a list of chunks that have already been received
        /// </summary>
        /// <param name="resumableInfo">Identifies the resumable upload</param>
        /// <returns>List of chunks that have already been received</returns>
        [HttpGet]
        public ActionResult ChunkStatus(ResumableInfo resumableInfo)
        {
            return new JsonResult(UploadHelper.GetChunkStatus(resumableInfo));
        }

        /// <summary>
        /// Upload a chunk of a file. Set as target for resumable.js
        /// </summary>
        /// <returns>Ok or BadRequest</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadChunk()
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

                            // This action expects at most 8 values
                            if (formAccumulator.ValueCount > 8)
                            {
                                throw new InvalidDataException("Form key count limit 8 exceeded.");
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
            if (!(bindingSuccessful || ModelState.IsValid))
            {
                return BadRequest(ModelState);
            }
            #endregion

            UploadHelper.FinalizeChunk(resumableInfo);

            return Ok();
        }

        /// <summary>
        /// Finalize upload by reassmebling and verifying the file
        /// </summary>
        /// <param name="resumableInfo">Identifies the resumable upload</param>
        /// <returns>Ok or 409</returns>
        [HttpPost]
        public IActionResult FinalizeUpload(ResumableInfo resumableInfo)
        {
            UploadHelper.FinalizeUpload(resumableInfo);

            return Ok();
        }
    }

    /// <summary>
    /// Static helper class for managing multipart requests
    /// </summary>
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