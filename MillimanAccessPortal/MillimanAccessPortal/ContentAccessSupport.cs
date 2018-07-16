using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal
{
    internal class ContentAccessSupport
    {
        private static List<Task> RunningReductionMonitors = new List<Task>();
        private static Timer CleanupTimer = null;

        internal static void AddReductionMonitor (Task NewTask)
        {
            lock(RunningReductionMonitors)
            {
                RunningReductionMonitors.Add(NewTask);
                if (CleanupTimer == null)
                {
                    CleanupTimer = new Timer(Cleanup, null, 2000, 2000);
                }
            }
        }

        private static void Cleanup(object state)
        {
            lock(RunningReductionMonitors)
            {
                int CompletedTask = Task.WaitAny(RunningReductionMonitors.ToArray(), 10);
                if (CompletedTask == -1)
                {
                    return;
                }

                // A task is completed
                RunningReductionMonitors.RemoveAt(CompletedTask);

                if (RunningReductionMonitors.Count == 0)
                {
                    CleanupTimer.Dispose();
                    CleanupTimer = null;
                }
            }
        }

        /// <summary>
        /// Returns a standardized file name for a non-reduced content or related file
        /// </summary>
        /// <param name="crf"></param>
        /// <param name="rootContentItemId"></param>
        /// <returns></returns>
        internal static string GenerateContentFileName(ContentRelatedFile crf, long rootContentItemId)
        {
            return $"{crf.FilePurpose}.Content[{rootContentItemId}]{Path.GetExtension(crf.FullPath)}";
        }

        /// <summary>
        /// Returns a standard file name for a reduced content file
        /// </summary>
        /// <param name="selectionGroupId"></param>
        /// <param name="rootContentItemId"></param>
        /// <param name="extensionWithDot"></param>
        /// <returns></returns>
        internal static string GenerateReducedContentFileName(long selectionGroupId, long rootContentItemId, string extensionWithDot)
        {
            return $"ReducedContent.SelGrp[{selectionGroupId}].Content[{rootContentItemId}]{extensionWithDot}";
        }

        /// <summary>
        /// Task entry point for monitoring a ContentReductionTask queued due to a selection group change.
        /// </summary>
        /// <param name="TaskGuid"></param>
        /// <param name="connectionString"></param>
        internal static void MonitorReductionTaskForGoLive(Guid TaskGuid, string connectionString, string contentRootFolder)
        {
            TimeSpan timeoutDuration = new TimeSpan(0, 8, 0);

            // Maybe there is a better way to configure the context...
            DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString);
            DbContextOptions<ApplicationDbContext> ContextOptions = ContextBuilder.Options;

            DateTime expireTimeUtc = DateTime.UtcNow + timeoutDuration;

            ContentReductionTask thisContentReductionTask = null;
            while (thisContentReductionTask == null || thisContentReductionTask.ReductionStatus != ReductionStatusEnum.Reduced)
            {
                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                {
                    thisContentReductionTask = Db.ContentReductionTask.Find(TaskGuid);
                }

                if (DateTime.UtcNow < expireTimeUtc)
                {
                    // Time out
                    // log something
                    return;
                }

                Thread.Sleep(2000);
            }

            // ready for go live, get all the navigation properties
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                thisContentReductionTask = Db.ContentReductionTask.Include(t => t.SelectionGroup)
                                                                  .ThenInclude(g => g.RootContentItem)
                                                                  //.ThenInclude(c => c.ContentType)
                                                                  .Single(t => t.Id == thisContentReductionTask.Id);

                PositionReducedContentForGoLive(Db, thisContentReductionTask.SelectionGroup, contentRootFolder);
                Db.SaveChanges();
            }

        }

        /// <summary>
        /// Moves files and writes db updates, requires navigation property chain SelectionGroup, RootContentItem
        /// </summary>
        /// <param name="Db"></param>
        internal static void PositionReducedContentForGoLive(ApplicationDbContext Db, ContentReductionTask reductionTask, string contentRootFolder)
        {
            string targetFileName = GenerateReducedContentFileName(reductionTask.SelectionGroupId, reductionTask.SelectionGroup.RootContentItemId, Path.GetExtension(reductionTask.ResultFilePath));
            string targetFilePath = Path.Combine(contentRootFolder, reductionTask.SelectionGroup.RootContentItemId.ToString(), targetFileName);

            continue here
            // rename the live reduced file to .bak
            // rename the new reduced file to live
            // update selection group with new selection list
            // update selection group set IsMaster = false

            // save changes
        }
    }
}
