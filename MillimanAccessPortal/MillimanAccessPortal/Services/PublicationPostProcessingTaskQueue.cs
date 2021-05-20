/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A queue of postprocessing tasks to be performed after a reducing content publication
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public interface IPublicationPostProcessingTaskQueue
    {
        void QueuePublicationPostProcess(Guid publicationRequestId);

        Task<Guid> DequeueAsync(CancellationToken cancellationToken, Int32 timeoutMs = -1);
    }

    public class PublicationPostProcessingTaskQueue : IPublicationPostProcessingTaskQueue
    {
        private ConcurrentQueue<Guid> _publicationRequestIdQueue = new ConcurrentQueue<Guid>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void QueuePublicationPostProcess(Guid publicationRequestId)
        {
            _publicationRequestIdQueue.Enqueue(publicationRequestId);

            GlobalFunctions.IssueLog(IssueLogEnum.PublishingStuck, $"Publication request Id {publicationRequestId} enqueued for detection by postprocessing hosted service");

            int initialCount = _signal.CurrentCount;
            _signal.Release();
        }

        /// <summary>
        /// Waits for the semaphore to be signaled and then returns a queued Guid
        /// </summary>
        /// <param name="cancellationToken">May be canceled in another thread to stop waiting</param>
        /// <param name="timeoutMs">default or -1 to wait indefinitely, 0 for test with no wait</param>
        /// <returns>A Guid from the queue, or Guid.Empty if none was found</returns>
        public async Task<Guid> DequeueAsync(CancellationToken cancellationToken, int timeoutMs = -1)
        {
            int initialCount = _signal.CurrentCount;

            if (! await _signal.WaitAsync(timeoutMs, cancellationToken))
            {
                return Guid.Empty;
            }

            bool LegitId = _publicationRequestIdQueue.TryDequeue(out var publicationRequestId);
            if (!LegitId)
            {
                return Guid.Empty;
            }

            return publicationRequestId;
        }
    }
}
