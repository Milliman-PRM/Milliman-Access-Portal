using MapDbContextLib.Identity;
using System;
using System.Runtime.CompilerServices;

namespace AuditLogLib.Event
{
    public abstract class AuditEventTypeBase
    {
        protected readonly int id;
        protected readonly string name;

        public AuditEventTypeBase(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        protected AuditEvent ToEvent(
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerName = "",
            [CallerLineNumber] int callerLine = 0)
        {
            return new AuditEvent
            {
                TimeStampUtc = DateTime.UtcNow,
                EventType = name,
                Source = $"{callerPath} {callerName}:{callerLine}",
            };
        }

        public override string ToString()
        {
            return $"{id}:{name}";
        }
    }
}
