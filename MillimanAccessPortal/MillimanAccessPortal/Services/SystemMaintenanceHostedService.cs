/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An ASP.NET Core hosted service that performs various system maintenance operations
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class SystemMaintenanceHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer _clientAccessReviewNotificationTimer;

        public SystemMaintenanceHostedService(
            IServiceProvider serviceProviderArg)
        {
            _serviceProvider = serviceProviderArg;

            _clientAccessReviewNotificationTimer = new Timer(ClientAccessReviewNotificationHandler);
            _clientAccessReviewNotificationTimer.Change(0,Timeout.Infinite);
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
        }

        private void ClientAccessReviewNotificationHandler(object state)
        {
            Timer thisTimer = (Timer)state;

            using (var scope = _serviceProvider.CreateScope())
            {
            }

            // Next timer callback should be tomorrow at time 09:00 UTC
            DateTime nextCall = DateTime.Today + TimeSpan.FromHours(24 + 9);
            thisTimer.Change(nextCall - DateTime.UtcNow, Timeout.InfiniteTimeSpan);
        }
    }
}
