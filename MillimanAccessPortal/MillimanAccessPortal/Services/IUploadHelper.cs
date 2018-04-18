using MillimanAccessPortal.Models.ContentPublicationViewModels;
using System.Collections.Generic;
using System.IO;

namespace MillimanAccessPortal.Services
{
    public interface IUploadHelper
    {
        List<uint> GetChunkStatus(ResumableInfo resumableInfo);

        Stream OpenTempFile();

        void FinalizeChunk(ResumableInfo resumableInfo);

        void FinalizeUpload(ResumableInfo resumableInfo);

        string GetOutputFilePath();
    }
}
