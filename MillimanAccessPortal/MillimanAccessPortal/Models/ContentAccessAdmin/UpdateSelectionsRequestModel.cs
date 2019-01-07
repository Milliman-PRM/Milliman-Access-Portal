using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class UpdateSelectionsRequestModel
    {
        public Guid GroupId { get; set; }
        public bool IsMaster { get; set; }
        public List<Guid> Selections { get; set; }
    }
}
