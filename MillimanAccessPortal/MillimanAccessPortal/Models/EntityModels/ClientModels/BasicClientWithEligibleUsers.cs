using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ClientModels
{
    public class BasicClientWithEligibleUsers : BasicClientWithCardStats
    {
        public List<Guid> EligibleUsers { get; set; }
    }
}
