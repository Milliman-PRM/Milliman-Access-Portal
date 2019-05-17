using System;
using System.Collections.Generic;
using System.Text;

namespace AuditLogLib.Models
{
    public class UpdateClientDomainLimitLogModel
    {
        public Guid ClientId { get; set; }
        public int NewDomainLimit { get; set; }
        public int PreviousDomainLimit { get; set; }
        public string DomainListUpdateRequestedByPersonName { get; set; }
        public string DomainListUpdateRequestedReason { get; set; }
    }
}
