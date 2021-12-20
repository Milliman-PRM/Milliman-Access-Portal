using Microsoft.AspNetCore.SignalR.Client;

namespace ContainerReverseProxy
{
    public class SignalRHubRetryForeverPolicy : IRetryPolicy
    {
        private readonly TimeSpan _delay;

        public SignalRHubRetryForeverPolicy(TimeSpan delay)
        {
            _delay = delay;
        }

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return _delay;
        }
    }
}
