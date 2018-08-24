/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Functionality to support actions in the ContentAccessAdminController
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Event;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using QlikviewLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        internal static async Task MonitorReductionTaskForGoLive(Guid TaskGuid, string connectionString, string contentRootFolder, object ContentTypeConfig = null)
        {
            TimeSpan timeoutDuration = new TimeSpan(0, 8, 0);
            ContentReductionTask thisContentReductionTask = null;

            DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString);
            DbContextOptions<ApplicationDbContext> ContextOptions = ContextBuilder.Options;

            do
            {
                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                {
                    thisContentReductionTask = Db.ContentReductionTask.Find(TaskGuid);
                }

                Thread.Sleep(1000);
            }
            while (thisContentReductionTask.ReductionStatus == ReductionStatusEnum.Queued);

            // The task might stay queued a while, waiting for other tasks.  Don't start timing till it has started
            DateTime expireTimeUtc = DateTime.UtcNow + timeoutDuration;

            while (thisContentReductionTask.ReductionStatus != ReductionStatusEnum.Reduced)
            {
                Thread.Sleep(2000);

                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                {
                    thisContentReductionTask = Db.ContentReductionTask.Find(TaskGuid);
                }

                if (DateTime.UtcNow > expireTimeUtc)
                {
                    // TODO log something
                    return;
                }

                if (thisContentReductionTask.ReductionStatus == ReductionStatusEnum.Canceled || 
                    thisContentReductionTask.ReductionStatus == ReductionStatusEnum.Error)
                {
                    throw new ApplicationException($"Reduction error while processing UpdateSelections request{Environment.NewLine}{thisContentReductionTask.ReductionStatusMessage}");
                }
            }

            // ready for go live, get all the navigation properties
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                thisContentReductionTask = Db.ContentReductionTask.Include(t => t.ApplicationUser)
                                                                  .Include(t => t.SelectionGroup)
                                                                      .ThenInclude(g => g.RootContentItem)
                                                                          .ThenInclude(c=> c.ContentType)
                                                                  .Single(t => t.Id == thisContentReductionTask.Id);

                List<string> FilesToDelete = await ReducedContentGoLive(Db, thisContentReductionTask, contentRootFolder, ContentTypeConfig);

                FilesToDelete.ForEach(f => File.Delete(f));
                Directory.Delete(Path.GetDirectoryName(thisContentReductionTask.ResultFilePath), true);

                if (thisContentReductionTask.ReductionStatus == ReductionStatusEnum.Live)
                {
                    #region Log audit event
                    AuditLogger Logger = new AuditLogger();
                    Logger.Log(AuditEventType.SelectionChangeReductionLive.ToEvent(thisContentReductionTask));
                    #endregion
                }
            }

        }

        /// <summary>
        /// Moves files and updates db, requires navigation property chain SelectionGroup and RootContentItem. Cooperates in a transaction
        /// </summary>
        /// <param name="Db">A valid instance of database context</param>
        /// <param name="reductionTask">The reduction task with navigation property chain SelectionGroup and RootContentItem</param>
        /// <param name="contentRootShareFolder">The configured root path for live content files</param>
        /// <returns>A list of files that should be deleted by the caller</returns>
        internal static async Task<List<string>> ReducedContentGoLive(ApplicationDbContext Db, ContentReductionTask reductionTask, string contentRootShareFolder, object ContentTypeConfig = null)
        {
            List<string> FilesToDelete = new List<string>();

            if (reductionTask == null || 
                reductionTask.SelectionGroup == null || 
                reductionTask.SelectionGroup.RootContentItem == null)
            {
                throw new ApplicationException("ContentAccessSupport.PositionReducedContentForGoLive called without required navigation properties");
            }
            if (reductionTask.SelectionCriteria == null)
            {
                throw new ApplicationException("ContentAccessSupport.PositionReducedContentForGoLive called for ContentReductionTask with null SelectionCriteria");
            }

            string targetFileName = GenerateReducedContentFileName(reductionTask.SelectionGroupId, reductionTask.SelectionGroup.RootContentItemId, Path.GetExtension(reductionTask.ResultFilePath));
            string targetFilePath = Path.Combine(contentRootShareFolder, reductionTask.SelectionGroup.RootContentItemId.ToString(), targetFileName);
            string backupFilePath = targetFilePath + ".bak";

            // rename the live reduced file to *.bak
            if (File.Exists(targetFilePath))
            {
                File.Delete(backupFilePath);  // does not throw if !Exists
                File.Move(targetFilePath, backupFilePath);
                FilesToDelete.Add(backupFilePath);
            }

            // Copy the new reduced file to live.  Entire source directirectory is removed by the caller of this function
            File.Copy(reductionTask.ResultFilePath, targetFilePath);

            // update selection group
            List<long> ValueIdList = new List<long>();
            reductionTask.SelectionCriteriaObj.Fields.ForEach(f => ValueIdList.AddRange(f.Values.Where(v => v.SelectionStatus).Select(v => v.Id)));
            reductionTask.SelectionGroup.SelectedHierarchyFieldValueList = ValueIdList.ToArray();
            reductionTask.SelectionGroup.IsMaster = false;
            reductionTask.SelectionGroup.SetContentUrl(Path.GetFileName(targetFileName));

            // update reduction tasks status (previous: Live -> Replaced, new: Reduced -> Live
            ContentReductionTask PreviousLiveTask = Db.ContentReductionTask.SingleOrDefault(t => t.SelectionGroupId == reductionTask.SelectionGroupId && 
                                                                                                 t.ReductionStatus == ReductionStatusEnum.Live);
            if (PreviousLiveTask != null)
            {
                PreviousLiveTask.ReductionStatus = ReductionStatusEnum.Replaced;
            }
            reductionTask.ReductionStatus = ReductionStatusEnum.Live;

            // Perform any content type dependent follow up processing
            switch (reductionTask.SelectionGroup.RootContentItem.ContentType.TypeEnum)
            {
                case ContentTypeEnum.Qlikview:
                    QlikviewConfig QvConfig = (QlikviewConfig)ContentTypeConfig;
                    await new QlikviewLibApi().AuthorizeUserDocumentsInFolder(reductionTask.SelectionGroup.RootContentItemId.ToString(), QvConfig);
                    break;

                case ContentTypeEnum.Unknown:
                default:
                    break;
            }

            // save changes
            Db.SaveChanges();

            return FilesToDelete;
        }
    }
}
