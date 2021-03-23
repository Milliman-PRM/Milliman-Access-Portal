using System;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.EntityModels.SelectionGroupModels
{
    /// <summary>
    /// A simplified representation of a SelectionGroup.
    /// This model is intended to be extended to satisfy front end needs.
    /// </summary>
    public class BasicSelectionGroup
    {
        public Guid Id { get; set; }
        public Guid RootContentItemId { get; set; }
        public bool IsSuspended { get; set; }
        public bool IsInactive { get; set; }
        public bool IsMaster { get; set; }
        public bool IsEditableEligible { get; set; }
        public bool Editable { get; set; }
        public string Name { get; set; }

        public static explicit operator BasicSelectionGroup(SelectionGroup selectionGroup)
        {
            if (selectionGroup == null)
            {
                return null;
            }

            return new BasicSelectionGroup
            {
                Id = selectionGroup.Id,
                RootContentItemId = selectionGroup.RootContentItemId,
                IsSuspended = selectionGroup.IsSuspended,
                IsInactive = selectionGroup.IsInactive,
                IsMaster = selectionGroup.IsMaster,
                Name = selectionGroup.GroupName,
                Editable = selectionGroup.Editable,
            };
        }
    }
}
