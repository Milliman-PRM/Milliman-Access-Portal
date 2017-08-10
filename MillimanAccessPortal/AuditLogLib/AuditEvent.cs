using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace AuditLogLib
{
    public class AuditEvent
    {
        public long Id { get; set; }

        public DateTime TimeStamp { get; set; }

        public string User { get; set; }

        public string SourceApplication { get; set; }

        public string EventType { get; set; }

        [Column(TypeName = "jsonb")]
        public string EventDetailJsonb { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        internal object EventDetailObject   // for serialization to jsonb
        {
            get
            {
                // TODO deserialize EventDetailJson
                return JsonConvert.DeserializeObject(EventDetailJsonb);
            }
            set
            {
                // TODO serialize to EventDetailJson
                EventDetailJsonb = JsonConvert.SerializeObject(value);
            }
        }
    }
}
