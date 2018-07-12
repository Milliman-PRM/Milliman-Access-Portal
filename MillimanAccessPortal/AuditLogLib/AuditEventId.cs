using System;

namespace AuditLogLib
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
    }

    public sealed class AuditEventId<T> : AuditEventIdBase
    {
        private readonly Func<T, object> fn;

        public AuditEventId(int id, string name, Func<T, object> fn) : base(id, name) {
            this.fn = fn;
        }
    }

    public sealed class AuditEventId<T, U> : AuditEventIdBase
    {
        private readonly Func<T, U, object> fn;

        public AuditEventId(int id, string name, Func<T, U, object> fn) : base(id, name) {
            this.fn = fn;
        }
    }

    public sealed class AuditEventId<T, U, V> : AuditEventIdBase
    {
        private readonly Func<T, U, V, object> fn;

        public AuditEventId(int id, string name, Func<T, U, V, object> fn) : base(id, name) {
            this.fn = fn;
        }
    }
}
