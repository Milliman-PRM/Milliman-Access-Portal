using Microsoft.Extensions.Logging;

namespace AuditLogLib
{
    /// <summary>
    /// Sort of enumeration for argument to the AuditLogger.Log(...) EventId argument
    /// </summary>
    public class AuditEventId
    {
        public static readonly int AuditEventBaseId = 1000;
        public static readonly int AuditEventMaxId = AuditEventBaseId + 999;

        public static readonly EventId LoginSuccess = new EventId(AuditEventBaseId + 1, "Login Success");
        public static readonly EventId LoginFailure = new EventId(AuditEventBaseId + 2, "Login Failure");
    }
}
