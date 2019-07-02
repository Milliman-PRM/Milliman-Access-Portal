/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A request model for updates to a Client entity in SystemAdminController
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    /// <summary>
    /// Request model for updating a client from SystemAdmin UI
    /// </summary>
    public class ClientUpdate
    {
        public Guid ClientId { get; set; }
        public DomainLimitUpdate DomainLimitChange { get; set; }

        public class DomainLimitUpdate
        {
            public int NewDomainLimit { get; set; }
            public string DomainLimitReason { get; set; }
            public string DomainLimitRequestedByPersonName { get; set; }
        }

        public UpdateClientDomainLimitLogModel BuildAuditLogEventData(int previousLimit, string clientName)
        {
            return new UpdateClientDomainLimitLogModel
            {
                ClientId = ClientId,
                ClientName = clientName,
                NewDomainLimit = DomainLimitChange.NewDomainLimit,
                PreviousDomainLimit = previousLimit,
                DomainListUpdateRequestedByPersonName = DomainLimitChange.DomainLimitRequestedByPersonName,
                DomainListUpdateRequestedReason = DomainLimitChange.DomainLimitReason,
            };
        }
    }
}
