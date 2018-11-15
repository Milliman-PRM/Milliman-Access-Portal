using AuditLogLib.Event;
using AuditLogLib.Services;
using MapCommonLib;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MillimanAccessPortal.Models.ContentPublishing;
using MillimanAccessPortal.Services;
using Serilog;
using System;
using System.Linq;
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
            var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            while (!cancellationToken.IsCancellationRequested)
            {
                // Retrieve the relevant data to finalize the goLive
                var goLiveViewModel = await TaskQueue.DequeueAsync(cancellationToken);

            }
        }
    }

    private async void ProcessGoLive(
        GoLiveViewModel goLiveViewModel, ApplicationDbContext dbContext, IAuditLogger auditLogger)
    {
        #region Checksum verification
        var publicationRequest = dbContext.ContentPublicationRequest
            .Include(r => r.RootContentItem)
                .ThenInclude(c => c.ContentType)
            .Include(r => r.ApplicationUser)
            .Where(r => r.Id == goLiveViewModel.PublicationRequestId)
            .Where(r => r.RootContentItemId == goLiveViewModel.PublicationRequestId)
            .SingleOrDefault(r => r.RequestStatus == PublicationStatus.Processed);

        if (publicationRequest?.RootContentItem == null || publicationRequest?.ApplicationUser == null)
        {
            Log.Error(
                "In QueueGoLiveTaskHostedService.ExecuteAsync action: " +
                $"publication request {goLiveViewModel.PublicationRequestId} not found, " + 
                $"or related user {publicationRequest?.ApplicationUserId} not found, " +
                $"or related content item {publicationRequest?.RootContentItemId} not found");
            FailGoLive(dbContext, "Go-Live request references an invalid publication request.");
            return;
        }

        bool ReductionIsInvolved = publicationRequest.RootContentItem.DoesReduce
            && publicationRequest.LiveReadyFilesObj.Any(f => f.FilePurpose.ToLower() == "mastercontent");

        var relatedReductionTasks = dbContext.ContentReductionTask
            .Include(t => t.SelectionGroup)
                .ThenInclude(g => g.RootContentItem)
                    .ThenInclude(c => c.ContentType)
            .Where(t => t.ContentPublicationRequestId == publicationRequest.Id)
            .ToList();

        if (ReductionIsInvolved)
        {
            // For each reducing SelectionGroup related to the RootContentItem:
            var relatedSelectionGroups = dbContext.SelectionGroup
                .Where(g => g.RootContentItemId == goLiveViewModel.RootContentItemId)
                .Where(g => !g.IsMaster);
            foreach (var relatedSelectionGroup in relatedSelectionGroups)
            {
                ContentReductionTask ThisTask;

                // RelatedReductionTasks should have one ContentReductionTask related to the SelectionGroup
                try
                {
                    ThisTask = relatedReductionTasks.Single(t => t.SelectionGroupId == relatedSelectionGroup.Id);
                }
                catch (InvalidOperationException)
                {
                    Log.Error($"" +
                        "In QueueGoLiveTaskHostedService.ExecuteAsync action: " +
                        "expected one reduction task for each non-master selection group, " +
                        $"failed for selection group {relatedSelectionGroup.Id}, " +
                        "aborting");
                    FailGoLive(dbContext,
                        $"Expected 1 reduction task related to SelectionGroup {relatedSelectionGroup.Id}, " +
                        "cannot complete this go-live request.");
                    return;
                }

                // Validate file checksum for reduced content
                var currentChecksum = GlobalFunctions.GetFileChecksum(ThisTask.ResultFilePath).ToLower();
                if (currentChecksum != ThisTask.ReducedContentChecksum.ToLower())
                {
                    Log.Error($"In QueueGoLiveTaskHostedService.ExecuteAsync action: " +
                        "for selection group {relatedSelectionGroup.Id}, " +
                        "reduced content file {ThisTask.ResultFilePath} failed checksum validation, " +
                        "aborting");
                    auditLogger.Log(AuditEventType.GoLiveValidationFailed.ToEvent(
                        publicationRequest.RootContentItem, publicationRequest));
                    FailGoLive(dbContext,
                        $"Reduced content file failed integrity check, " +
                        "cannot complete the go-live request.");
                    return;
                }
            }
        }

        // Validate Checksums of LiveReady files
        foreach (var Crf in publicationRequest.LiveReadyFilesObj)
        {
            if (!Crf.ValidateChecksum())
            {
                Log.Error($"In QueueGoLiveTaskHostedService.ExecuteAsync action: " +
                    $"for publication request {publicationRequest.Id}, " +
                    $"live ready file {Crf.FullPath} failed checksum validation, " +
                    "aborting");
                auditLogger.Log(AuditEventType.GoLiveValidationFailed.ToEvent(
                    publicationRequest.RootContentItem, publicationRequest));
                FailGoLive(dbContext, "File integrity validation failed");
                return;
            }
        }
        #endregion

        Console.WriteLine("Validated a Go Live request.");
    }

    private async void FailGoLive(DbContext dbContext, string reason)
    {
        Console.WriteLine("Go Live failed.");
    }
} 
