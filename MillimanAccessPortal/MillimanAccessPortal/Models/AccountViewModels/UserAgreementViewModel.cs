/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Model to support user acceptance of the MAP user agreement
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Models;
using System;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class UserAgreementViewModel
    {
        public string AgreementText { get; set; }

        public Guid ValidationId { get; set; }

        public bool IsRenewal { get; set; }

        public string ReturnUrl { get; set; }

        public static explicit operator UserAgreementLogModel(UserAgreementViewModel source)
            => new UserAgreementLogModel { AgreementText = source.AgreementText };
    }
}
