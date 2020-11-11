/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An injectable service interface for audit logging.
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using AuditLogLib.Models;
using MapDbContextLib.Context;
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
        Task<List<ActivityEventModel>> GetAuditEventsAsync(List<Expression<Func<AuditEvent, bool>>> serverFilters, ApplicationDbContext db, bool orderDescending, List<Expression<Func<AuditEvent, bool>>> clientFilters = null, int limit = -1);
    }
}
