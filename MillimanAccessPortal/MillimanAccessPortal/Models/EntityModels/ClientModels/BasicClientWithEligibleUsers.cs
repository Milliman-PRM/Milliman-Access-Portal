using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ClientModels
{
    public class BasicClientWithEligibleUsers : BasicClientWithStats
    {
        public List<Guid> EligibleUsers { get; set; }
    }
}
