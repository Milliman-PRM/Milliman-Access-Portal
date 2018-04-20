/*
 * CODE OWNERS: Joseph Sweeney, 
 * OBJECTIVE: Provide a service to abstract away file system operations involved in managing chunked uploads
 * DEVELOPER NOTES:
 *      This service was designed with only a single implementation (UploadHelper) in mind.
 *      If further implementations are required, it is likely that changing this interface
 *      would be beneficial.
 */

using MillimanAccessPortal.Models.ContentPublicationViewModels;
using System.Collections.Generic;
using System.IO;

namespace MillimanAccessPortal.Services
{
    public interface IUploadHelper
    {
        /// <summary>
        /// Get which chunks have already been received for an upload
        /// </summary>
        /// <remarks>
        /// Only useful to resumable.js, and then mostly useful when resuming an interupted upload.
        /// </remarks>
        /// <param name="resumableInfo">Identifies the resumable upload</param>
        /// <returns>List of chunks that already exist on the server</returns>
        List<uint> GetChunkStatus(ResumableInfo resumableInfo);

        /// <summary>
        /// Open a stream to a new temporary file
        /// </summary>
        /// <returns>A Stream to the new temporary file</returns>
        Stream OpenTempFile();

        /// <summary>
        /// Run post-chunk upload procedures
        /// </summary>
        /// <param name="resumableInfo">Identifies the resumable upload</param>
        void FinalizeChunk(ResumableInfo resumableInfo);

        /// <summary>
        /// Run post-file upload procedures
        /// </summary>
        /// <param name="resumableInfo">Identifies the resumable upload</param>
        void FinalizeUpload(ResumableInfo resumableInfo);

        /// <summary>
        /// Get the location of the uploaded file
        /// </summary>
        /// <returns>Path to the file</returns>
        string GetOutputFilePath();
    }
}
