using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapCommonLib;
using Microsoft.Extensions.FileProviders;
using MillimanAccessPortal.Models.ContentPublicationViewModels;

namespace MillimanAccessPortal.Services
{
    public class UploadHelper : IUploadHelper, IDisposable
    {
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

        public bool GetChunkReceived(ResumableInfo resumableInfo, uint chunkNumber)
        {
            Info = resumableInfo;
            var chunkInfo = _fileProvider.GetFileInfo(ChunkFilePath(chunkNumber));
            return (chunkInfo.Exists
                    && chunkInfo.Length == resumableInfo.ChunkSize // basic validation
                    && resumableInfo.ChunkNumber != resumableInfo.TotalChunks);
        }

        public Stream OpenTempFile()
        {
            return File.Create(_fileProvider.GetFileInfo(TempFilePath).PhysicalPath);
        }

        public void ProcessUpload(ResumableInfo resumableInfo, out bool AllChunksReceived)
        {
            AllChunksReceived = false;
            Info = resumableInfo;

            if (!_fileProvider.GetFileInfo(TempFilePath).Exists)
            {
                // panic
            }

            if (Info.TotalSize > GlobalFunctions.maxFileUploadSize)
            {
                // panic
            }

            SolidifyChunk();

            if (_fileProvider.GetDirectoryContents(ChunkDirPath).Count() < Info.TotalChunks)
            {
                return;
            }
            AllChunksReceived = true;

            ConcatenateChunks();

            VerifyUpload();
        }

        public string GetOutputFilePath()
        {
            return (Info != null)
                ? _fileProvider.GetFileInfo(OutputFilePath).PhysicalPath
                : null;
        }

        private void SolidifyChunk()
        {
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

        private void ConcatenateChunks()
        {
            var concatenationFilePath = ConcatenationFilePath;
            var chunkFilePaths = Enumerable.Range(1, Convert.ToInt32(Info.TotalChunks))
                .Select(chunkNumber => ChunkFilePath(((uint) chunkNumber)));
            using (var concatenationStream = File.OpenWrite(_fileProvider.GetFileInfo(concatenationFilePath).PhysicalPath))
            {
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
            Directory.Delete(_fileProvider.GetFileInfo(ChunkDirPath).PhysicalPath);
        }

        private void VerifyUpload()
        {
            var concatenationFilePath = _fileProvider.GetFileInfo(ConcatenationFilePath).PhysicalPath;
            var computedChecksum = GlobalFunctions.GetFileChecksum(concatenationFilePath);
            if (!Info.Checksum.Equals(computedChecksum, StringComparison.OrdinalIgnoreCase))
            {
                // panic
            }

            // Rename the file with proper extension - this makes it visible to the virus scanner
            var outputFilePath = _fileProvider.GetFileInfo(OutputFilePath).PhysicalPath;
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }
            File.Move(concatenationFilePath, outputFilePath);
        }

        public void Dispose()
        {
            // Remove any leftover files
            foreach (var path in _paths)
            {
                var fileInfo = _fileProvider.GetFileInfo(path);

                if (path == null || !fileInfo.Exists)
                {
                    continue; // the file was not used or was already removed
                }

                try
                {
                    File.Delete(fileInfo.PhysicalPath);
                }
                catch (UnauthorizedAccessException)
                {
                    Directory.Delete(fileInfo.PhysicalPath, recursive: true);
                }
            }
        }
    }
}
