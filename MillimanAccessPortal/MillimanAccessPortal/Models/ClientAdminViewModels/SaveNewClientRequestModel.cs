using MapCommonLib.ActionFilters;
using MapDbContextLib.Identity;
using System;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class SaveNewClientRequestModel
    {
        /// <summary>
        /// New name of the client
        /// </summary>
        [EmitBeforeAfterLog]
        public string Name { get; set; }

        /// <summary>
        /// New code belonging to the client.
        /// </summary>
        [EmitBeforeAfterLog]
        public string ClientCode { get; set; }

        /// <summary>
        /// Name of the primary contact for this client.
        /// </summary>
        [EmitBeforeAfterLog]
        public string ContactName { get; set; }

        /// <summary>
        /// Title of the primary contact for this client.
        /// </summary>
        [EmitBeforeAfterLog]
        public string ContactTitle { get; set; }

        /// <summary>
        /// Email address of the primary contact for this client.
        /// </summary>
        [EmitBeforeAfterLog]
        public string ContactEmail { get; set; }

        /// <summary>
        /// Phone number of the primary contact for this client.
        /// </summary>
        [EmitBeforeAfterLog]
        public string ContactPhone { get; set; }

        /// <summary>
        /// Name of the consultant for this client.
        /// </summary>
        [EmitBeforeAfterLog]
        public string ConsultantName { get; set; }

        /// <summary>
        /// Email belonging to the consultant for this client.
        /// </summary>
        [EmitBeforeAfterLog]
        public string ConsultantEmail { get; set; }

        // Name, ClientCode, ContactName, ContactTitle, ContactEmail, ContactPhone, ConsultantName, ConsultantEmail," +
        // "ConsultantOffice,AcceptedEmailDomainList,AcceptedEmailAddressExceptionList,ParentClientId,ProfitCenterId,NewUserWelcomeText"
    }
}

