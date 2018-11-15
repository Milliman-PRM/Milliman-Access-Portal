using MapDbContextLib.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MillimanAccessPortal.Models.ContentPublishing;
using MillimanAccessPortal.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

public class QueuedGoLiveTaskHostedService : BackgroundService
{
    public QueuedGoLiveTaskHostedService(
        IServiceProvider services,
        IGoLiveTaskQueue taskQueue)
    {
        Services = services;
        TaskQueue = taskQueue;
    }

    public IServiceProvider Services { get; }
    public IGoLiveTaskQueue TaskQueue { get; }

    protected async override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            while (!cancellationToken.IsCancellationRequested)
            {
                // Retrieve the relevant data to finalize the goLive
                var goLiveViewModel = await TaskQueue.DequeueAsync(cancellationToken);

                Console.WriteLine(
                    $"Received request to Go Live for publication {goLiveViewModel.PublicationRequestId}");
            }
        }
    }
} 
