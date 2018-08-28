using MapDbContextLib.Context;
using System;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class ClientDetail
    {
        public Guid Id { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string ClientContactName { get; set; }
        public string ClientContactEmail { get; set; }
        public string ClientContactPhone { get; set; }
        public string ProfitCenter { get; set; }
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
                ProfitCenter = client.ProfitCenter?.Name,
                Office = client.ConsultantOffice,
                ConsultantName = client.ConsultantName,
                ConsultantEmail = client.ConsultantEmail,
            };
        }

    }
}
