/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System;
using System.Linq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.AccountViewModels;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class ReductionSummary
    {
        public UserInfoViewModel User { get; set; }
        public ReductionStatusEnum StatusEnum { get; set; }
        public string StatusName { get; set; }
        public Guid SelectionGroupId { get; set; }
        public Guid? RootContentItemId { get; set; }
        public int QueuedDuration { get; set; }
        public int QueuePosition { get; set; } = -1;

        public static explicit operator ReductionSummary(ContentReductionTask contentReductionTask)
        {
            if (contentReductionTask == null)
            {
                return null;
            }
            return new ReductionSummary
            {
                User = ((UserInfoViewModel)contentReductionTask.ApplicationUser),
                StatusEnum = contentReductionTask.ReductionStatus,
                StatusName = ContentReductionTask.ReductionStatusDisplayNames[contentReductionTask.ReductionStatus],
                SelectionGroupId = contentReductionTask.SelectionGroupId,
                RootContentItemId = contentReductionTask.ContentPublicationRequest?.RootContentItemId,
                QueuedDuration = (int)(contentReductionTask.ReductionStatus.IsActive()
                    ? DateTime.UtcNow - contentReductionTask.CreateDateTimeUtc
                    : TimeSpan.Zero).TotalMilliseconds,
            };
        }
    }

    public static class ReductionSummaryExtensions
    {
        public static ReductionSummary ToSummaryWithQueueInformation(this ContentReductionTask reductionTask, ApplicationDbContext dbContext)
        {
            var reductionSummary = (ReductionSummary)reductionTask;

            if (reductionTask?.ReductionStatus.IsCancelable() ?? false)
            {
                var precedingReductionTaskCount = dbContext.ContentReductionTask
                    .Where(r => r.CreateDateTimeUtc < reductionTask.CreateDateTimeUtc)
                    .Where(r => r.ReductionStatus.IsCancelable())
                    .Count();
                reductionSummary.QueuePosition = precedingReductionTaskCount;
            }

            return reductionSummary;
        }
    }
}
