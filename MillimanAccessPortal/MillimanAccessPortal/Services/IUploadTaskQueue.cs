using MillimanAccessPortal.Models.ContentPublishing;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    // Adapted from https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-2.1
    public interface IUploadTaskQueue
    {
        void QueueUploadFinalization(ResumableInfo upload);

        Task<ResumableInfo> DequeueAsync(CancellationToken cancellationToken);
    }

    public class UploadTaskQueue : IUploadTaskQueue
    {
        private ConcurrentQueue<ResumableInfo> _uploadIds = new ConcurrentQueue<ResumableInfo>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void QueueUploadFinalization(ResumableInfo upload)
        {
            if (upload == null)
            {
                throw new ArgumentNullException(nameof(upload));
            }

            _uploadIds.Enqueue(upload);
            _signal.Release();
        }

        public async Task<ResumableInfo> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _uploadIds.TryDequeue(out var uploadId);

            return uploadId;
        }
    }
}
