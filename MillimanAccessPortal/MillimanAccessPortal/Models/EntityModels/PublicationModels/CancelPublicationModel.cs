using MillimanAccessPortal.Models.ContentPublishing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.EntityModels.PublicationModels
{
    /// <summary>
    /// Data returned for a publication status poll
    /// </summary>
    public class CancelPublicationModel
    {
        public StatusResponseModel StatusResponseModel { get; set; }
        public RootContentItemDetail RootContentItemDetail { get; set; }
    }
}
