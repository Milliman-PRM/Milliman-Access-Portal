/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Controller for actions supporting the content publishing page
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapCommonLib;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Authorization;
using AuditLogLib;
using AuditLogLib.Services;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Net.Http.Headers;
using System.IO;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Text;
using MillimanAccessPortal.Models.ContentPublicationViewModels;

namespace MillimanAccessPortal.Controllers
{
    public class ContentPublishingController : Controller
    {
        private readonly ApplicationDbContext DbContext;
        private readonly StandardQueries DbQueries;
        private readonly ILogger Logger;
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;

        /// <summary>
        /// Constructor, stores local references to injected service instances
        /// </summary>
        /// <param name="ContextArg"></param>
        /// <param name="DbQueriesArg"></param>
        /// <param name="LoggerFactoryArg"></param>
        /// <param name="AuditLoggerArg"></param>
        /// <param name="AuthorizationServiceArg"></param>
        public ContentPublishingController(
            ApplicationDbContext ContextArg,
            StandardQueries DbQueriesArg,
            ILoggerFactory LoggerFactoryArg,
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg
            )
        {
            DbContext = ContextArg;
            DbQueries = DbQueriesArg;
            Logger = LoggerFactoryArg.CreateLogger<ContentPublishingController>(); ;
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
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
        /// Queues a request for publication and associated reduction tasks
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="RootContentId"></param>
        /// <returns></returns>
        public async Task<IActionResult> RequestContentPublication(string FileName, long RootContentId)
        {
            #region Preliminary Validation
            RootContentItem RootContent = DbContext.RootContentItem.SingleOrDefault(rc => rc.Id == RootContentId);
            if (RootContent == null)
            {
                Response.Headers.Add("Warning", "Requested content item not found.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.ContentPublisher, RootContentId));
            if (!Result1.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to publish this content");
                return Unauthorized();
            }
            #endregion

            #region Validation
            int ExistingTaskCountForRootContent = DbContext.ContentReductionTask
                                                           .Include(t => t.ContentPublicationRequest)
                                                           .Where(t => t.ContentPublicationRequest.RootContentItemId == RootContentId)
                                                           .Where(t => t.ReductionStatus != ReductionStatusEnum.Live)
                                                           .Count();
            if (ExistingTaskCountForRootContent > 0)
            {
                Response.Headers.Add("Warning", "Tasks are already pending for this content item.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Virus checking here?

            // more?
            #endregion

            try
            {
                using (IDbContextTransaction Transaction = DbContext.Database.BeginTransaction())
                {
                    ContentPublicationRequest NewPubRequest = new ContentPublicationRequest { ApplicationUser = await DbQueries.GetCurrentApplicationUser(User),
                        RootContentItemId = RootContentId,
                        MasterFilePath = "ThisInputFile" };
                    DbContext.ContentPublicationRequest.Add(NewPubRequest);
                    DbContext.SaveChanges();

                    foreach (SelectionGroup SelGrp in DbContext.SelectionGroup.Where(sg => sg.RootContentItemId == RootContentId).ToList())
                    {
                        string SelectionCriteriaString = JsonConvert.SerializeObject(DbQueries.GetFieldSelectionsForSelectionGroup(SelGrp.Id), Formatting.Indented);
                        var NewTask = new ContentReductionTask
                        {
                            ApplicationUser = await DbQueries.GetCurrentApplicationUser(User),
                            SelectionGroupId = SelGrp.Id,
                            MasterFilePath = "ThisInputFile",
                            ResultFilePath = "ThisOutputFile",
                            ContentPublicationRequest = NewPubRequest,
                            SelectionCriteria = SelectionCriteriaString,
                            ReductionStatus = ReductionStatusEnum.Queued,
                        };

                        DbContext.ContentReductionTask.Add(NewTask);
                    }

                    DbContext.SaveChanges();
                    Transaction.Commit();
                }
            }
            catch (Exception e)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(e, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): failed to store request to database");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", "Error processing request.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Json(new object());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }

            // Used to accumulate all the form url encoded key value pairs in the request.
            var formAccumulator = new KeyValueAccumulator();
            string targetFilePath = null;

            // The encapsulation boundary should never exceed 70 characters.
            // See https://tools.ietf.org/html/rfc2046#section-5.1.1
            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                lengthLimit: 70);
            var reader = new MultipartReader(boundary, Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                ContentDispositionHeaderValue contentDisposition;
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        targetFilePath = Path.GetTempFileName();
                        using (var targetStream = System.IO.File.Create(targetFilePath))
                        {
                            await section.Body.CopyToAsync(targetStream);
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
            var formValueProvider = new FormValueProvider(
                BindingSource.Form,
                new FormCollection(formAccumulator.GetResults()),
                CultureInfo.CurrentCulture);

            var ApplicationUser = await DbQueries.GetCurrentApplicationUser(User);
            var RootContentItemId = Convert.ToInt64(formValueProvider.GetValue("RootContentItemId").FirstValue);

            var ContentPublicationRequest = new ContentPublicationRequest()
            {
                RootContentItemId = RootContentItemId,
                ApplicationUserId = ApplicationUser.Id,
                MasterFilePath = targetFilePath
            };
            DbContext.ContentPublicationRequest.Add(ContentPublicationRequest);
            DbContext.SaveChanges();

            return Json(ContentPublicationRequest);
        }

        [HttpGet]
        public ActionResult ChunkStatus(ResumableData resumableData)
        {
            ISet<int> remainingChunks;
            string serializedData = ((string) TempData[resumableData.UID]);
            if (serializedData == null)
            {
                // Verify that existing chunks are of specified chunk size to prevent an interupted chunk from spoiling the file upload
                var targetPath = Path.Combine(Path.GetTempPath(), resumableData.UID);
                var existingChunks = Directory.Exists(targetPath)
                    ? Directory.GetFiles(targetPath)
                        .Where(fileName => new FileInfo(fileName).Length == resumableData.ChunkSize)
                        .Select(fileName => Path.GetFileName(fileName))
                    : new List<string>();

                remainingChunks = new HashSet<int>(Enumerable.Range(1, resumableData.TotalChunks)
                    .Where(chunkNumber => !existingChunks.Contains($"{chunkNumber:D8}.chunk")));

                serializedData = JsonConvert.SerializeObject(remainingChunks);
            }
            else
            {
                remainingChunks = JsonConvert.DeserializeObject<HashSet<int>>(serializedData);
            }

            // TempData removes values after they are read, so reset the chunk data even if it hasn't changed
            TempData[resumableData.UID] = serializedData;

            return remainingChunks.Contains(resumableData.ChunkNumber)
                ? ((ActionResult) NotFound())
                : Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadResumable()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }

            // Used to accumulate all the form url encoded key value pairs in the request.
            var formAccumulator = new KeyValueAccumulator();
            string targetFilePath = null;

            var tempFilePath = Path.GetTempFileName();

            // The encapsulation boundary should never exceed 70 characters.
            // See https://tools.ietf.org/html/rfc2046#section-5.1.1
            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                lengthLimit: 70);
            var reader = new MultipartReader(boundary, Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                ContentDispositionHeaderValue contentDisposition;
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        using (var targetStream = System.IO.File.Create(tempFilePath))
                        {
                            await section.Body.CopyToAsync(targetStream);
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
            var resumableData = new ResumableData();
            var formValueProvider = new FormValueProvider(
                BindingSource.Form,
                new FormCollection(formAccumulator.GetResults()),
                CultureInfo.CurrentCulture);

            var bindingSuccessful = await TryUpdateModelAsync(resumableData, prefix: "",
                valueProvider: formValueProvider);
            if (!bindingSuccessful)
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
            }

            // Move the temporary file
            targetFilePath = Path.Combine(
                Path.GetTempPath(),
                resumableData.UID,
                $"{resumableData.ChunkNumber:D8}.chunk");
            
            if (System.IO.File.Exists(targetFilePath))
            {
                // It is OK to receive a chunk more than once
                System.IO.File.Delete(targetFilePath);
            }
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath));
            System.IO.File.Move(tempFilePath, targetFilePath);


            // Keep track of which chunks we have
            string serializedData = ((string) TempData[resumableData.UID]);
            ISet<int> remainingChunks = JsonConvert.DeserializeObject<HashSet<int>>(serializedData);
            remainingChunks.Remove(Convert.ToInt32(resumableData.ChunkNumber));
            if (remainingChunks.Count > 0)
            {
                serializedData = JsonConvert.SerializeObject(remainingChunks);
                TempData[resumableData.UID] = serializedData;
            }
            else
            {
                // Reassemble the file from its parts
                var srcPath = Path.Combine(Path.GetTempPath(), resumableData.UID);
                var srcFileNames = Enumerable.Range(1, Convert.ToInt32(resumableData.TotalChunks))
                    .Select(chunkNumber => Path.Combine(srcPath, $"{chunkNumber:D8}.chunk"));
                var dstFileName = Path.Combine(Path.GetTempPath(), $"{resumableData.UID}.upload");
                using (Stream dstStream = System.IO.File.OpenWrite(dstFileName))
                {
                    foreach (var srcFileName in srcFileNames)
                    {
                        using (Stream srcStream = System.IO.File.OpenRead(srcFileName))
                        {
                            srcStream.CopyTo(dstStream);
                        }
                        System.IO.File.Delete(srcFileName);
                    }
                }
                try
                {
                    Directory.Delete(srcPath);
                }
                catch (UnauthorizedAccessException)
                {
                    // TODO: notify that directory was not empty
                    Directory.Delete(srcPath, true);
                }

                // checksum the file

                // rename the file with proper extension, this will allow it to be noticed by virus scanner
            }

            return Ok();
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
            MediaTypeHeaderValue mediaType;
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out mediaType);
            // Use UTF-8 instead of UTF-7 if it is requested
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }
    }
}