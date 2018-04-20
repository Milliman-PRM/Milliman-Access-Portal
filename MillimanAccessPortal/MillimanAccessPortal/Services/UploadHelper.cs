using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapCommonLib;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
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
            public string Temp;
            public string Chunk;
            public string Concat;
            public string Output;

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
        private PathSet _paths = new PathSet();
        private ResumableInfo Info { get; set; }

        private string TempFilePath
        {
            get
            {
                if (_paths.Temp == null)
                {
                    _paths.Temp = Path.GetRandomFileName();
                }
                return _paths.Temp;
            }
        }

        private string ChunkFilePath(uint chunkNumber)
        {
            return Path.Combine(ChunkDirPath, $"{chunkNumber:D8}.chunk");
        }

        private string ChunkDirPath
        {
            get
            {
                if (_paths.Chunk == null)
                {
                    _paths.Chunk = Info.UID;
                }
                return _paths.Chunk;
            }
        }

        private string ConcatenationFilePath
        {
            get
            {
                if (_paths.Concat == null)
                {
                    _paths.Concat = $"{Info.UID}.upload";
                }
                return _paths.Concat;
            }
        }

        private string OutputFilePath
        {
            get
            {
                if (_paths.Output == null)
                {
                    _paths.Output = $"{Info.UID}{Info.FileExt}";
                }
                return _paths.Output;
            }
        }

        public UploadHelper(
            IFileProvider fileProvider
            )
        {
            _fileProvider = fileProvider;
        }

        /// <summary>
        /// Get which chunks have already been received for an upload
        /// </summary>
        /// <remarks>
        /// Only useful to resumable.js, and mostly useful when resuming an interupted upload.
        /// </remarks>
        /// <param name="resumableInfo">Identifies the resumable upload</param>
        /// <returns>List of chunks that already exist on the server</returns>
        public List<uint> GetChunkStatus(ResumableInfo resumableInfo)
        {
            var receivedChunks = new List<uint>();
            var chunkDirInfo = _fileProvider.GetFileInfo(ChunkDirPath);

            if (chunkDirInfo.Exists && chunkDirInfo.IsDirectory)
            {
                receivedChunks.AddRange(_fileProvider.GetDirectoryContents(ChunkDirPath)
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
            return File.Create(_fileProvider.GetFileInfo(TempFilePath).PhysicalPath);
        }

        /// <summary>
        /// Copy the temporary file opened in OpenTempFile() to a known location
        /// </summary>
        /// <param name="resumableInfo">Identifies the resumable upload</param>
        public void FinalizeChunk(ResumableInfo resumableInfo)
        {
            Info = resumableInfo;

            var tempFileInfo = _fileProvider.GetFileInfo(TempFilePath);
            // Expect the chunk to have been uploaded
            if (!tempFileInfo.Exists)
            {
                return; // StatusCodes.Status400BadRequest;
            }

            // Expect the total file size to be within the limit
            if (Info.TotalSize > GlobalFunctions.maxFileUploadSize)
            {
                return; // StatusCodes.Status413PayloadTooLarge;
            }

            // Ensure the temp file size is as expected
            if (tempFileInfo.Length != ((long) resumableInfo.ExpectedSize()))
            {
                return; // StatusCodes.Status400BadRequest;
            }

            var tempFilePath = _fileProvider.GetFileInfo(TempFilePath).PhysicalPath;
            var chunkFilePath = _fileProvider.GetFileInfo(ChunkFilePath(Info.ChunkNumber)).PhysicalPath;
            var chunkDirPath = _fileProvider.GetFileInfo(ChunkDirPath).PhysicalPath;

            if (File.Exists(chunkFilePath))
            {
                File.Delete(chunkFilePath);
            }
            Directory.CreateDirectory(chunkDirPath);
            try
            {
                File.Move(tempFilePath, chunkFilePath);
            }
            catch (Exception e)
            {
                Console.Write(e);
            }
        }

        /// <summary>
        /// Concatenate the uploaded chunks into a single file and verify
        /// </summary>
        /// <param name="resumableInfo">Identifies the resumable upload</param>
        public void FinalizeUpload(ResumableInfo resumableInfo)
        {
            Info = resumableInfo;

            // Make sure the expected and actual number of chunks match
            if (_fileProvider.GetDirectoryContents(ChunkDirPath).Count() != Info.TotalChunks)
            {
                return; // StatusCodes.Status400BadRequest;
            }

            #region Concatenate chunks
            var concatenationFilePath = _fileProvider.GetFileInfo(ConcatenationFilePath).PhysicalPath;
            using (var concatenationStream = File.OpenWrite(concatenationFilePath))
            {
                var chunkFilePaths = Enumerable.Range(1, Convert.ToInt32(Info.TotalChunks))
                    .Select(chunkNumber => ChunkFilePath(((uint) chunkNumber)));
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
            var chunkDirPath = _fileProvider.GetFileInfo(ChunkDirPath).PhysicalPath;
            Directory.Delete(chunkDirPath);
            #endregion

            #region Verify upload
            var computedChecksum = GlobalFunctions.GetFileChecksum(concatenationFilePath);
            if (!Info.Checksum.Equals(computedChecksum, StringComparison.OrdinalIgnoreCase))
            {
                return; // StatusCodes.Status409Conflict;
            }

            // Rename the file with proper extension - this makes it visible to the virus scanner
            var outputFilePath = _fileProvider.GetFileInfo(OutputFilePath).PhysicalPath;
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }
            File.Move(concatenationFilePath, outputFilePath);
            #endregion

            return; // null;
        }

        /// <summary>
        /// Get the location of the verified uploaded file
        /// </summary>
        /// <returns>Absolute path to the file</returns>
        public string GetOutputFilePath()
        {
            return (Info != null)
                ? _fileProvider.GetFileInfo(OutputFilePath).PhysicalPath
                : null;
        }

        /// <summary>
        /// Delete any leftover files
        /// </summary>
        public void Dispose()
        {
            foreach (var path in _paths)
            {
                var fileInfo = _fileProvider.GetFileInfo(path);

                if (path == null || !fileInfo.Exists)
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
