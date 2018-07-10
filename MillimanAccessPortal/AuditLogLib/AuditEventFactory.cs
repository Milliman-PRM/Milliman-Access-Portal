using MapDbContextLib.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AuditLogLib
{
    public class AuditEventFactory
    {
        public static AuditEventFactory Instance { get; } = new AuditEventFactory();


        private readonly Dictionary<AuditEventId, Object> _registeredEvents = new Dictionary<AuditEventId, Object>();

        private AuditEventFactory()
        {
            Register(AuditEventId.SelectionGroupCreated, (s) => new
            {
                ClientId = s?.RootContentItem?.Client?.Id,
                RootContentItemId = s?.RootContentItem?.Id,
                SelectionGroupId = s?.Id,
            });
        }

        public void Register(AuditEventId auditEventId)
        {
            
        }

        public void Register<T>(AuditEventId<T> auditEventId, Func<T, Object> func)
        {
            _registeredEvents.Add(auditEventId, func);
        }

        public void Register<T, U>(AuditEventId<T, U> auditEventId, Func<T, U, Object> func)
        {

        }

        public AuditEvent Create<T>(AuditEventId eventId, T entity, string userName = "System", string sessionId = "", [CallerMemberName] string callerName = "")
        {
            var registration = _registeredEvents.SingleOrDefault(e => e.Key.Id == eventId.Id);

            if (registration.Equals(default(KeyValuePair<AuditEventId, Object>)))
            {
                // return generic "Unconfigured audit event" object, or throw
                return null;
            }

            var objectFn = registration.Value as Func<T, Object>;
            var loggableObject = objectFn(entity);

            return new AuditEvent
            {
                TimeStamp = DateTime.Now,
                EventType = registration.Key.Name,
                EventDetailObject = loggableObject,
                User = userName,
                SessionId = sessionId,
                Source = callerName,
            };
        }
    }
}
