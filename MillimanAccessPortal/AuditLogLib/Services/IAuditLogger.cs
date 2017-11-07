using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AuditLogLib.Services
{
    public interface IAuditLogger
    {
        void Log(AuditEvent Event);
    }
}
