using AuditLogLib.Event;

namespace AuditLogLib.Services
{
    public interface IAuditLogger
    {
        void Log(AuditEvent Event);
    }
}
