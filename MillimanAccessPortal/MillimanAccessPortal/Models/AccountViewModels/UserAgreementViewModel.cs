/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Model to support user acceptance of the MAP user agreement
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class UserAgreementViewModel
    {
        public string AgreementText { get; set; }

        public bool IsRenewal { get; set; }

        public bool ReturnUrl { get; set; }

    }
}
