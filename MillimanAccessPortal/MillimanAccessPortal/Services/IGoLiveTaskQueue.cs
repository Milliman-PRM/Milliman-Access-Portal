using MillimanAccessPortal.Models.ContentPublishing;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    // Adapted from https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-2.1
    public interface IGoLiveTaskQueue
    {
        void QueueGoLive(GoLiveViewModel goLive);

        Task<GoLiveViewModel> DequeueAsync(CancellationToken cancellationToken);
    }

    public class GoLiveTaskQueue : IGoLiveTaskQueue
    {
        private ConcurrentQueue<GoLiveViewModel> _goLiveIds = new ConcurrentQueue<GoLiveViewModel>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void QueueGoLive(GoLiveViewModel goLive)
        {
            if (goLive == null)
            {
                throw new ArgumentNullException(nameof(goLive));
            }

            _goLiveIds.Enqueue(goLive);
            _signal.Release();
        }

        public async Task<GoLiveViewModel> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _goLiveIds.TryDequeue(out var goLiveId);

            return goLiveId;
        }
    }
}
