/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.AccountViewModels;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ReductionDetails
    {
        public UserInfoViewModel User { get; set; }
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
                User = ((UserInfoViewModel) contentReductionTask.ApplicationUser),
                StatusEnum = contentReductionTask.ReductionStatus,
                StatusName = ContentReductionTask.ReductionStatusDisplayNames[contentReductionTask.ReductionStatus],
                SelectionGroupId = contentReductionTask.SelectionGroupId,
                RootContentItemId = contentReductionTask.ContentPublicationRequest?.RootContentItemId,
            };
        }
    }
}
