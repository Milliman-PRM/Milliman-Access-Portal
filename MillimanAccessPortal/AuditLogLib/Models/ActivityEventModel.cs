/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Contains fields that are returned to users who request bulk FileDrop activity logs
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using System;

namespace AuditLogLib.Models
{
    public class ActivityEventModel
    {
        public DateTime TimeStampUtc { get; set; }

        public int EventCode { get; set; }

        public string EventType { get; set; }

        public string UserName { get; set; }

        public string FullName { get; set; }

        public string EventData { get; set; }

        public static ActivityEventModel Generate(AuditEvent evt, Names names)
        {
            return new ActivityEventModel
            {
                TimeStampUtc = evt.TimeStampUtc,
                EventCode = evt.EventCode,
                EventType = evt.EventType,
                UserName = evt.User,
                FullName = $"{names.FirstName} {names.LastName}",
                EventData = evt.EventData,
            };
        }

        public class Names
        {
            public string UserName { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;

            public static Names Empty => new Names();
        }
    }
}
