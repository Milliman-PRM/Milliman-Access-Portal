using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using System;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class DeleteGroupViewModel
    {
        public Guid GroupId { get; set; }
        public BasicContentItemWithStats ContentItemStats { get; set; }
        public BasicClientWithStats ClientStats { get; set; }
    }
}
