using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AuditLogLib
{
    public class AuditLoggerProvider : ILoggerProvider
    {
        AuditLoggerConfiguration Config = null;

        public AuditLoggerProvider(AuditLoggerConfiguration ConfigArg)
        {
            // TODO Think about whether any configurable behaviors should be incorporated based on content of ConfigArg
            Config = ConfigArg;
        }

        public ILogger CreateLogger(string CategoryName)
        {
            return new AuditLogger(Config);
        }

        public void Dispose()
        {
        }
    }
}
