using System;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SuspendGroupRequestModel
    {
        public Guid GroupId { get; set; }
        public bool IsSuspended { get; set; }
    }
}
