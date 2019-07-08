using System;
using System.Collections.Generic;
using System.Text;

namespace AuditLogLib.Models
{
    public class UserAgreementLogModel
    {
        public Guid ValidationId { get; set; }
        public string AgreementText { get; set; }
    }
}
