using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ClientModels
{
    public class BasicClientWithEligibleUsers : BasicClientWithCardStats
    {
        /// <summary>
        /// Users that are eligible to be assigned to a selection group in this client
        /// </summary>
        public List<Guid> EligibleUsers { get; set; }
    }
}
