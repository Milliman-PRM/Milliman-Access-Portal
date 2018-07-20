using System;

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

        protected AuditEvent ToEvent(string callerName, string callerPath, int callerLine)
        {
            return new AuditEvent
            {
                TimeStampUtc = DateTime.UtcNow,
                EventType = name,
                Source = $"{callerPath}:{callerLine} {callerName}",
            };
        }

        public override string ToString()
        {
            return $"{id}:{name}";
        }
    }
}
