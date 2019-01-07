using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class UpdateGroupRequestModel
    {
        public Guid GroupId { get; set; }
        public string Name { get; set; }
        public List<Guid> Users { get; set; }
    }
}
