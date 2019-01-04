using System;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.EntityModels.SelectionGroupModels
{
    public class BasicSelectionGroup
    {
        public Guid Id { get; set; }
        public Guid RootContentItemId { get; set; }
        public bool IsSuspended { get; set; }
        public bool IsMaster { get; set; }
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
                IsMaster = selectionGroup.IsMaster,
                Name = selectionGroup.GroupName,
            };
        }
    }
}
