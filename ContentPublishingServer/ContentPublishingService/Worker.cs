/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Initiates top level execution of the service
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using ContentPublishingLib;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace ContentPublishingService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ProcessManager _processManager;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _processManager = new ProcessManager();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _processManager.Start();
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _processManager.Stop();
            return Task.CompletedTask;
        }
    }
}
