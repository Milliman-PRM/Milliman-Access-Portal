/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An injectable service interface for audit logging.
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using System.Threading.Tasks;

namespace AuditLogLib.Services
{
    public interface IAuditLogger
    {
        void Log(AuditEvent Event);
        void Log(AuditEvent Event, string UserNameArg);
    }
}
