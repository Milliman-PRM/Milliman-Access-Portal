/*
 * CODE OWNERS: Evan Klein
 * OBJECTIVE: Represents a request model for the toggling of the 'editability' status of a Selection Group with a PowerBI document.
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib.ActionFilters;
using System;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SetPowerBiEditabilityRequestModel
    {
        /// <summary>
        /// ID of the selection group to update.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid GroupId { get; set; }

        /// <summary>
        /// Determines if the PowerBI document should be visible to members of this selection group in the 'Editable' mode.
        /// </summary>
        [EmitBeforeAfterLog]
        public bool Editable { get; set; }
    }
}
