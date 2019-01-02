using MapDbContextLib.Context;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.EntityModels.PublicationModels
{
    public class BasicReduction
    {
        public Guid Id { get; set; }
        public Guid? ContentPublicationRequestId { get; set; }
        public Guid ApplicationUserId { get; set; }
        public Guid? SelectionGroupId { get; set; }
        public List<Guid> SelectedValues { get; set; }
        public DateTime CreateDateTimeUtc { get; set; }
        public ReductionStatusEnum TaskStatus { get; set; }

        public static explicit operator BasicReduction(ContentReductionTask reduction)
        {
            if (reduction == null)
            {
                return null;
            }

            return new BasicReduction
            {
                Id = reduction.Id,
                ContentPublicationRequestId = reduction.ContentPublicationRequestId,
                ApplicationUserId = reduction.ApplicationUserId,
                SelectionGroupId = reduction.SelectionGroupId,
                SelectedValues = reduction.SelectionCriteriaObj.GetSelectedValueIds(),
                CreateDateTimeUtc = reduction.CreateDateTimeUtc,
                TaskStatus = reduction.ReductionStatus,
            };
        }
    }
}
