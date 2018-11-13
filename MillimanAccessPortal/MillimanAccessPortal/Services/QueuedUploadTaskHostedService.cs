using MapDbContextLib.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Services;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class QueuedUploadTaskHostedService : BackgroundService
{
    public QueuedUploadTaskHostedService(
        IServiceProvider services,
        IUploadTaskQueue taskQueue)
    {
        Services = services;
        TaskQueue = taskQueue;
    }

    public IServiceProvider Services { get; }
    public IUploadTaskQueue TaskQueue { get; }

    protected async override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var uploadHelper = scope.ServiceProvider.GetRequiredService<IUploadHelper>();

            while (!cancellationToken.IsCancellationRequested)
            {
                // Retrieve the relevant data to finalize the upload
                var resumableInfo = await TaskQueue.DequeueAsync(cancellationToken);
                var fileUpload = dbContext.FileUpload
                    .OrderByDescending(f => f.InitiatedDateTimeUtc)
                    .First(f => f.ClientFileIdentifier == resumableInfo.UID);

                try
                {
                    uploadHelper.FinalizeUpload(resumableInfo);
                }
                catch (Exception e)
                {
                    Log.Error(e, "In QueuedUploadTaskHostedService.ExecuteAsync for {@ResumableInfo}", resumableInfo);
                    fileUpload.Status = FileUploadStatus.Error;
                    fileUpload.StatusMessage = e is FileUploadException
                        ? e.Message
                        : null;
                    await dbContext.SaveChangesAsync();
                    continue;
                }

                fileUpload.Status = FileUploadStatus.Complete;
                fileUpload.Checksum = resumableInfo.Checksum;
                fileUpload.CreatedDateTimeUtc = DateTime.UtcNow;
                fileUpload.StoragePath = uploadHelper.GetOutputFilePath();

                await dbContext.SaveChangesAsync();
            }
        }
    }
} 
