using System;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class CancelReductionRequestModel
    {
        /// <summary>
        /// ID of the selection group for which active reductions will be canceled.
        /// </summary>
        public Guid GroupId { get; set; }
    }
}
