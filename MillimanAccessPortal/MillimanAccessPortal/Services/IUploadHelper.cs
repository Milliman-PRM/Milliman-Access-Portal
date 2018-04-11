using MillimanAccessPortal.Models.ContentPublicationViewModels;
using System.IO;

namespace MillimanAccessPortal.Services
{
    public interface IUploadHelper
    {
        bool GetChunkReceived(ResumableInfo resumableInfo, uint chunkNumber);

        FileStream OpenTempFile();

        void ProcessUpload(ResumableInfo resumableInfo, out bool AllChunksReceived);

        string GetOutputFilePath();
    }
}
