using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using System;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class DeleteGroupResponseModel
    {
        public Guid GroupId { get; set; }
        public BasicContentItemWithCardStats ContentItemStats { get; set; }
    }
}
