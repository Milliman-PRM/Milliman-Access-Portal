/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ReductionDetails
    {
        public ApplicationUser User { get; set; }
        public ReductionStatusEnum StatusEnum { get; set; }
        public string StatusName { get; set; }
        public long SelectionGroupId { get; set; }
        public long? RootContentItemId { get; set; }

        public static explicit operator ReductionDetails(ContentReductionTask contentReductionTask)
        {
            if (contentReductionTask == null)
            {
                return null;
            }
            return new ReductionDetails
            {
                User = contentReductionTask.ApplicationUser,
                StatusEnum = contentReductionTask.ReductionStatus,
                StatusName = ContentReductionTask.ReductionStatusDisplayNames[contentReductionTask.ReductionStatus],
                SelectionGroupId = contentReductionTask.SelectionGroupId,
                RootContentItemId = contentReductionTask.ContentPublicationRequest?.RootContentItemId,
            };
        }
    }
}
