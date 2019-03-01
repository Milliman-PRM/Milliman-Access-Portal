using MapCommonLib.ActionFilters;
using System;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class CancelReductionRequestModel
    {
        /// <summary>
        /// ID of the selection group for which active reductions will be canceled.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid GroupId { get; set; }
    }
}
