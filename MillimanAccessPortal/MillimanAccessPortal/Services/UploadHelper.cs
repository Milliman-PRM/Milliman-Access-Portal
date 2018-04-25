/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: Controller for actions supporting the content publishing page
 * DEVELOPER NOTES:
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapCommonLib;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.ContentPublicationViewModels;

namespace MillimanAccessPortal.Services
{
    public class UploadHelper : IUploadHelper, IDisposable
    {
        /// <summary>
        /// Enumerates the paths managed by UploadHelper.
        /// </summary>
        private class PathSet : IEnumerable<string>
        {
            public string Temp { get; set; } = null;
            public string Chunk { get; set; } = null;
            public string Concat { get; set; } = null;
            public string Output { get; set; } = null;

            public string ChunkFilePath(uint chunkNumber)
            {
                return Path.Combine(Chunk, $"{chunkNumber:D8}.chunk");
            }

            public IEnumerator<string> GetEnumerator()
            {
                yield return Temp;
                yield return Chunk;
                yield return Concat;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private readonly IFileProvider _fileProvider;
        private PathSet pathSet { get; set; } = null;
        private ResumableInfo _info = null;
        private ResumableInfo Info
        {
            get
            {
                return _info;
            }
            set
            {
                if (pathSet == null)
                {
                    pathSet = new PathSet
                    {
                        Temp = Path.GetRandomFileName(),
                        Chunk = value.UID,
                        Concat = $"{Info.UID}.upload",
                        Output = $"{Info.UID}{Info.FileExt}",
                    };
                }                
                _info = value;
            }
        }

        public UploadHelper(
            IFileProvider fileProvider
            )
        {
            _fileProvider = fileProvider;
        }

        /// <summary>
        /// Get chunk status based on existing files on disk
        /// </summary>
        /// <remarks>
        /// Only useful to resumable.js, and mostly useful when resuming an interupted upload.
        /// Not currently in use since resumable uploads are not currently supported.
        /// </remarks>
        /// <param name="resumableInfo">Identifies the resumable upload</param>
        /// <returns>List of chunks that already exist on the server</returns>
        public List<uint> GetChunkStatus(ResumableInfo resumableInfo)
        {
            var receivedChunks = new List<uint>();
            var chunkDirInfo = _fileProvider.GetFileInfo(pathSet.Chunk);

            if (chunkDirInfo.Exists && chunkDirInfo.IsDirectory)
            {
                receivedChunks.AddRange(_fileProvider.GetDirectoryContents(pathSet.Chunk)
                    .Where(f => f.Exists && f.Length == resumableInfo.ChunkSize)
                    .Select(f => Convert.ToUInt32(f.Name.Split('.')[0])));
            }
            
            return receivedChunks;
        }

        /// <summary>
        /// Open a stream to a new temporary file
        /// </summary>
        /// <remarks>
        /// Use this method to have UploadHelper remove the temporary file when it goes out of scope.
        /// </remarks>
        /// <returns>A Stream to the new temporary file</returns>
        public Stream OpenTempFile()
        {
            return File.Create(_fileProvider.GetFileInfo(pathSet.Temp).PhysicalPath);
        }

        /// <summary>
        /// Copy the temporary file opened in OpenTempFile() to a known location
        /// </summary>
        /// <param name="resumableInfo">Identifies the resumable upload</param>
        public void FinalizeChunk(ResumableInfo resumableInfo)
        {
            Info = resumableInfo;

            var tempFileInfo = _fileProvider.GetFileInfo(pathSet.Temp);

            // Expect the chunk to have been uploaded
            if (!tempFileInfo.Exists)
            {
                throw new FileUploadException(StatusCodes.Status400BadRequest, "Uploaded chunk does not exist.");
            }

            // Expect the total file size to be within the limit
            if (Info.TotalSize > GlobalFunctions.maxFileUploadSize)
            {
                throw new FileUploadException(StatusCodes.Status413PayloadTooLarge, "File size is too large.");
            }

            // Ensure the temp file size is as expected
            if (tempFileInfo.Length != ((long) resumableInfo.ExpectedSize()))
            {
                throw new FileUploadException(StatusCodes.Status400BadRequest, "Uploaded chunk is not expected size.");
            }

            var tempFilePath = _fileProvider.GetFileInfo(pathSet.Temp).PhysicalPath;
            var chunkFilePath = _fileProvider.GetFileInfo(pathSet.ChunkFilePath(Info.ChunkNumber)).PhysicalPath;
            var chunkDirPath = _fileProvider.GetFileInfo(pathSet.Chunk).PhysicalPath;

            if (File.Exists(chunkFilePath))
            {
                File.Delete(chunkFilePath);
            }
            Directory.CreateDirectory(chunkDirPath);
            File.Move(tempFilePath, chunkFilePath);

            // Chunk was finalized properly; do not delete chunk directory in Dispose()
            pathSet.Chunk = null;
        }

        /// <summary>
        /// Concatenate the uploaded chunks into a single file and verify
        /// </summary>
        /// <param name="resumableInfo">Identifies the resumable upload</param>
        public void FinalizeUpload(ResumableInfo resumableInfo)
        {
            Info = resumableInfo;

            // Make sure the expected and actual number of chunks match
            if (_fileProvider.GetDirectoryContents(pathSet.Chunk).Count() != Info.TotalChunks)
            {
                throw new FileUploadException(StatusCodes.Status400BadRequest, "Number of uploaded chunks did not match expectation.");
            }

            #region Concatenate chunks
            var concatenationFilePath = _fileProvider.GetFileInfo(pathSet.Concat).PhysicalPath;
            using (var concatenationStream = File.OpenWrite(concatenationFilePath))
            {
                var chunkFilePaths = Enumerable.Range(1, Convert.ToInt32(Info.TotalChunks))
                    .Select(chunkNumber => pathSet.ChunkFilePath(((uint) chunkNumber)));
                foreach (var chunkFilePath in chunkFilePaths)
                {
                    var chunkFilePathPhysical = _fileProvider.GetFileInfo(chunkFilePath).PhysicalPath;
                    using (var chunkStream = File.OpenRead(chunkFilePathPhysical))
                    {
                        chunkStream.CopyTo(concatenationStream);
                    }
                    File.Delete(chunkFilePathPhysical);
                }
            }
            var chunkDirPath = _fileProvider.GetFileInfo(pathSet.Chunk).PhysicalPath;
            Directory.Delete(chunkDirPath);
            #endregion

            #region Verify upload
            var computedChecksum = GlobalFunctions.GetFileChecksum(concatenationFilePath);
            if (!Info.Checksum.Equals(computedChecksum, StringComparison.OrdinalIgnoreCase))
            {
                throw new FileUploadException(StatusCodes.Status409Conflict, "Checksums do not match.");
            }

            // Rename the file with proper extension - this makes it visible to the virus scanner
            var outputFilePath = _fileProvider.GetFileInfo(pathSet.Output).PhysicalPath;
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }
            File.Move(concatenationFilePath, outputFilePath);
            #endregion
        }

        /// <summary>
        /// Get the location of the verified uploaded file
        /// </summary>
        /// <returns>Absolute path to the file</returns>
        public string GetOutputFilePath()
        {
            return (Info != null)
                ? _fileProvider.GetFileInfo(pathSet.Output).PhysicalPath
                : null;
        }

        /// <summary>
        /// Delete any leftover files
        /// </summary>
        public void Dispose()
        {
            foreach (var path in pathSet)
            {
                var fileInfo = _fileProvider.GetDirectoryContents("/")
                    .Where(i => i.Name == path)
                    .SingleOrDefault();

                if (fileInfo == null)
                {
                    continue; // the file was not used or was already removed
                }

                if (fileInfo.IsDirectory)
                {
                    Directory.Delete(fileInfo.PhysicalPath, recursive: true);
                }
                else
                {
                    File.Delete(fileInfo.PhysicalPath);
                }
            }
        }
    }
}
