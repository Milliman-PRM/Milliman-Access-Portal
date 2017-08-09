using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuditLogLib
{
    internal class AuditEvent
    {
        internal DateTime TimeStamp { get; set; }

        internal string User { get; set; }

        internal string EventType { get; set; }

        private string EventDetailJson { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        internal string EventDetailObject   // for serialization to jsonb
        {
            get
            {
                // TODO deserialize EventDetailJson
                return EventDetailJson;
            }
            set
            {
                // TODO serialize to EventDetailJson
                EventDetailJson = value;
            }
        }
    }
}
