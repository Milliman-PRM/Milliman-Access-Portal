using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuditLogLib
{
    public class AuditEvent
    {
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
