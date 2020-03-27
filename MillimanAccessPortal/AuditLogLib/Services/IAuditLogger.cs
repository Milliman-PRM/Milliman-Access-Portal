/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An injectable service interface for audit logging.
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AuditLogLib.Services
{
    public interface IAuditLogger
    {
        void Log(AuditEvent Event);
        void Log(AuditEvent Event, string UserNameArg);
        void Log(AuditEvent Event, string UserNameArg, string SessionIdArg);
        Task<List<AuditEvent>> GetAuditEvents(List<Expression<Func<AuditEvent, bool>>> filters, bool orderDescending = true);
    }
}
