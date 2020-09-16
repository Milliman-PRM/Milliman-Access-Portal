/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Provide client information for display in the system admin detail panel
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.EntityModels.ClientModels
{
    public class ClientDetail
    {
        public Guid Id { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string ClientContactName { get; set; }
        public string ClientContactEmail { get; set; }
        public string ClientContactPhone { get; set; }
        public string ClientContactTitle { get; set; }
        public int DomainListCountLimit { get; set; }
        public List<string> AcceptedEmailDomainList { get; set; }
        public List<string> AcceptedEmailAddressExceptionList { get; set; }
        public ProfitCenter ProfitCenter { get; set; }
        public string Office { get; set; }
        public string ConsultantName { get; set; }
        public string ConsultantEmail { get; set; }

        public static explicit operator ClientDetail(Client client)
        {
            if (client == null)
            {
                return null;
            }

            return new ClientDetail
            {
                Id = client.Id,
                ClientName = client.Name,
                ClientCode = client.ClientCode,
                ClientContactName = client.ContactName,
                ClientContactEmail = client.ContactEmail,
                ClientContactPhone = client.ContactPhone,
                ClientContactTitle = client.ContactTitle,
                DomainListCountLimit = client.DomainListCountLimit,
                AcceptedEmailDomainList = client.AcceptedEmailDomainList,
                AcceptedEmailAddressExceptionList = client.AcceptedEmailAddressExceptionList,
                ProfitCenter = client.ProfitCenter,
                Office = client.ConsultantOffice,
                ConsultantName = client.ConsultantName,
                ConsultantEmail = client.ConsultantEmail,
            };
        }

    }
}
