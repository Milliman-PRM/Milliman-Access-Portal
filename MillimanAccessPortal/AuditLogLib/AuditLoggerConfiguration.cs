using System;

namespace AuditLogLib
{
    public class AuditLoggerConfiguration
    {
        public string AuditLogConnectionString { get; set; }
        public string ErrorLogRootFolder { get; set; }
    }
}
