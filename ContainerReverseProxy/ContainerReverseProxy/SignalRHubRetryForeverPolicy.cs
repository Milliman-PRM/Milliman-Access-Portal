using Microsoft.AspNetCore.SignalR.Client;

namespace ContainerReverseProxy
{
    public class SignalRHubRetryForeverPolicy : IRetryPolicy
    {
        private TimeSpan Delay { get; init; }

        public SignalRHubRetryForeverPolicy(TimeSpan delay)
        {
            Delay = delay;
        }

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return Delay;
        }
    }
}
