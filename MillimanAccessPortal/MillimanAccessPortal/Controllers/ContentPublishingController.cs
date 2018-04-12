/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Controller for actions supporting the content publishing page
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
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
using AuditLogLib.Services;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Net.Http.Headers;
using System.IO;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Text;
using MillimanAccessPortal.Models.ContentPublicationViewModels;
using System.Security.Cryptography;
using Microsoft.Extensions.FileProviders;

namespace MillimanAccessPortal.Controllers
{
    public class ContentPublishingController : Controller
    {
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ApplicationDbContext DbContext;
//      private readonly IFileProvider FileProvider;
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
//          IFileProvider FileProverArg,
            ILoggerFactory LoggerFactoryArg,
            StandardQueries QueriesArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DbContext = ContextArg;
//          FileProvider = FileProverArg;
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
                    ContentPublicationRequest NewPubRequest = new ContentPublicationRequest { ApplicationUser = await Queries.GetCurrentApplicationUser(User),
                        RootContentItemId = RootContentId,
                        MasterFilePath = "ThisInputFile" };
                    DbContext.ContentPublicationRequest.Add(NewPubRequest);
                    DbContext.SaveChanges();

                    foreach (SelectionGroup SelGrp in DbContext.SelectionGroup.Where(sg => sg.RootContentItemId == RootContentId).ToList())
                    {
                        string SelectionCriteriaString = JsonConvert.SerializeObject(Queries.GetFieldSelectionsForSelectionGroup(SelGrp.Id), Formatting.Indented);
                        var NewTask = new ContentReductionTask
                        {
                            ApplicationUser = await Queries.GetCurrentApplicationUser(User),
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

        [HttpGet]
        public ActionResult ChunkStatus(ResumableData resumableData)
        {
            var chunkRelPath = Path.Combine(resumableData.UID, $"{resumableData.ChunkNumber:D8}.chunk");
//          var chunkInfo = FileProvider.GetFileInfo(chunkRelPath);
            var chunkInfo = new FileInfo(Path.Combine(Path.GetTempPath(), chunkRelPath));

            return (chunkInfo.Exists
                    && chunkInfo.Length == resumableData.ChunkSize // basic validation
                    && resumableData.ChunkNumber != resumableData.TotalChunks) // always upload the last chunk
                ? ((ActionResult) Ok())
                : NoContent();
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

            // TODO: It is possible, however unlikely, to have multiple chunks choose the same temp path. Use managed temp file names instead.
//          var tempFilePath = FileProvider.GetFileInfo(Path.GetRandomFileName()).PhysicalPath;
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
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue contentDisposition);

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

            // All required model attributes must be present for binding to succeed
            var bindingSuccessful = await TryUpdateModelAsync(resumableData, prefix: "",
                valueProvider: formValueProvider);
            if (!bindingSuccessful)
            {
                if (!ModelState.IsValid)
                {
                    System.IO.File.Delete(tempFilePath);
                    return BadRequest(ModelState);
                }
            }

            // Ensure file is within size limit
            if (resumableData.TotalSize > (5 * 1000 * 1000 * 1000L)) // TODO: move to configuration
            {
                System.IO.File.Delete(tempFilePath);
                return BadRequest();
            }

            // Move the temporary file
//          var targetFileInfo = FileProvider.GetFileInfo(Path.Combine(
//              resumableData.UID, $"{resumableData.ChunkNumber:D8}.chunk"));
//          var targetFilePath = targetFileInfo.PhysicalPath;
            targetFilePath = Path.Combine(Path.GetTempPath(), resumableData.UID, $"{resumableData.ChunkNumber:D8}.chunk");

            var targetDirPath = Path.GetDirectoryName(targetFilePath);

            // It is OK to receive a chunk more than once
            if (System.IO.File.Exists(targetFilePath))
            {
                System.IO.File.Delete(targetFilePath);
            }
            Directory.CreateDirectory(targetDirPath);
            System.IO.File.Move(tempFilePath, targetFilePath);

//          if (FileProvider.GetDirectoryContents(resumableData.UID).Count() == resumableData.TotalChunks)
            if (new DirectoryInfo(Path.GetDirectoryName(targetFilePath)).GetFiles().Length == resumableData.TotalChunks)
            {
                var chunkPath = Path.Combine(Path.GetTempPath(), resumableData.UID);//
                // Reassemble the file from its parts
                var chunkFileNames = Enumerable.Range(1, Convert.ToInt32(resumableData.TotalChunks))
//                  .Select(chunkNumber => FileProvider.GetFileInfo(Path.Combine(resumableData.UID, $"{chunkNumber:D8}.chunk")).PhysicalPath);
//              var concatFileName = FileProvider.GetFileInfo($"{resumableData.UID}.upload").PhysicalPath;
                    .Select(chunkNumber => Path.Combine(chunkPath, $"{chunkNumber:D8}.chunk"));
                var concatFileName = Path.Combine(Path.GetTempPath(), $"{resumableData.UID}.upload");
                using (Stream concatStream = System.IO.File.OpenWrite(concatFileName))
                {
                    foreach (var chunkFileName in chunkFileNames)
                    {
                        using (Stream srcStream = System.IO.File.OpenRead(chunkFileName))
                        {
                            srcStream.CopyTo(concatStream);
                        }
                        System.IO.File.Delete(chunkFileName);
                    }
                }
//              Directory.Delete(FileProvider.GetFileInfo(resumableData.UID).PhysicalPath);
                Directory.Delete(chunkPath);

                // checksum the file
                byte[] checksumBytes;
                using (Stream concatStream = System.IO.File.OpenRead(concatFileName))
                using (HashAlgorithm hashAlgorithm = new SHA1Managed())
                {
                    checksumBytes = hashAlgorithm.ComputeHash(concatStream);
                }
                var checksum = BitConverter.ToString(checksumBytes).Replace("-", "");

                if (!resumableData.Checksum.Equals(checksum, StringComparison.OrdinalIgnoreCase))
                {
                    // checksums do not match, something went wrong during upload
                    // there isn't a great response code for this, so return a 400 with a warning.
                    Directory.Delete(concatFileName);
                    return BadRequest();
                }

                // rename the file with proper extension, this will allow it to be noticed by virus scanner
//              var finalFileName = FileProvider.GetFileInfo($"{resumableData.UID}{resumableData.FileExt}").PhysicalPath;
                if (System.IO.File.Exists(finalFileName))
                {
                    System.IO.File.Delete(finalFileName);
                }
                System.IO.File.Move(concatFileName, finalFileName);

                // create the publication request and reduction task(s)
                var currentApplicationUser = await Queries.GetCurrentApplicationUser(User);
                var contentPublicationRequest = new ContentPublicationRequest
                {
                    ApplicationUserId = currentApplicationUser.Id,
                    MasterFilePath = finalFileName,
                    RootContentItemId = resumableData.RootContentItemId,
                };
                DbContext.ContentPublicationRequest.Add(contentPublicationRequest);
                DbContext.SaveChanges();

                // Master selection group is created when root content item is created, so there must always
                // be at least one available selection group.
                // TODO: possibly create master selection group at publication time (here).
                var selectionGroups = DbContext.SelectionGroup
                    .Where(sg => sg.RootContentItemId == resumableData.RootContentItemId)
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

                return Json(contentPublicationRequest);
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