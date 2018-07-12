using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuditLogLib.Event
{
    public class AuditEvent
    {
        public static AuditEvent New(object a = null, object b = null, object c = null, object d = null, object e = null, object f = null)
        {
            return new AuditEvent { };
        }

        public long Id { get; set; }

        public DateTime TimeStampUtc { get; set; }

        public string SessionId { get; set; }

        public string User { get; set; }

        public string Source { get; set; }

        public string EventType { get; set; }

        [Column(TypeName = "jsonb")]
        public string EventData { get; set; }

        [NotMapped]
        public object EventDataObject
        {
            get => JsonConvert.DeserializeObject(EventData);
            set => EventData = JsonConvert.SerializeObject(value);
        }
    }
}
