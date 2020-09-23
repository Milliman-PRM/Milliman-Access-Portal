using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ClientAccessReview
{
    public class ApproveClientAccessReviewModel
    {
        public Guid ClientId { get; set; }
        public Guid ReviewId { get; set; }
    }
}
