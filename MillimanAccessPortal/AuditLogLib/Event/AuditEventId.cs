using MapDbContextLib.Identity;
using System;
using System.Runtime.CompilerServices;

namespace AuditLogLib.Event
{
    public abstract class AuditEventIdBase
    {
        protected readonly int id;
        protected readonly string name;

        public AuditEventIdBase(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        protected AuditEvent GenerateEvent(ApplicationUser user, string sessionId,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerName = "",
            [CallerLineNumber] int callerLine = 0)
        {
            return new AuditEvent
            {
                TimeStampUtc = DateTime.Now,
                EventType = name,
                User = user.ToString(),
                SessionId = sessionId,
                Source = $"{callerPath} {callerName}:{callerLine}",
            };
        }

        public override string ToString()
        {
            return $"{id}:{name}";
        }
    }

    // Generics represent what entities are required to log this event
    public sealed class AuditEventId : AuditEventIdBase
    {
        private readonly Func<Object> fn;

        public AuditEventId(int id, string name, Func<object> fn = null) : base(id, name) {
            this.fn = fn ?? new Func<object>(() => new { });
        }

        new public AuditEvent GenerateEvent(ApplicationUser user, string sessionId,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerName = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = base.GenerateEvent(user, sessionId, callerPath, callerName, callerLine);
            auditEvent.EventDataObject = fn();

            return auditEvent;
        }
    }

    public sealed class AuditEventId<T> : AuditEventIdBase
    {
        private readonly Func<T, object> fn;

        public AuditEventId(int id, string name, Func<T, object> fn) : base(id, name) {
            this.fn = fn;
        }

        public AuditEvent GenerateEvent(ApplicationUser user, string sessionId,
            T entity,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerName = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = base.GenerateEvent(user, sessionId, callerPath, callerName, callerLine);
            auditEvent.EventDataObject = fn(entity);

            return auditEvent;
        }
    }

    public sealed class AuditEventId<T, U> : AuditEventIdBase
    {
        private readonly Func<T, U, object> fn;

        public AuditEventId(int id, string name, Func<T, U, object> fn) : base(id, name) {
            this.fn = fn;
        }

        public AuditEvent GenerateEvent(ApplicationUser user, string sessionId,
            T firstEntity, U secondEntity,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerName = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = base.GenerateEvent(user, sessionId, callerPath, callerName, callerLine);
            auditEvent.EventDataObject = fn(firstEntity, secondEntity);

            return auditEvent;
        }
    }

    public sealed class AuditEventId<T, U, V> : AuditEventIdBase
    {
        private readonly Func<T, U, V, object> fn;

        public AuditEventId(int id, string name, Func<T, U, V, object> fn) : base(id, name) {
            this.fn = fn;
        }

        public AuditEvent GenerateEvent(ApplicationUser user, string sessionId,
            T firstEntity, U secondEntity, V thirdEntity,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerName = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = base.GenerateEvent(user, sessionId, callerPath, callerName, callerLine);
            auditEvent.EventDataObject = fn(firstEntity, secondEntity, thirdEntity);

            return auditEvent;
        }
    }
}
