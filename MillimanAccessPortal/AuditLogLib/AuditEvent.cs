using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace AuditLogLib
{
    public class AuditEvent
    {
        public AuditEvent()
        {
            TimeStamp = DateTime.Now;
        }

        /// <summary>
        /// Convenience method for AuditLogger.Log() callers to construct an AuditEvent object to pass as the state argument
        /// </summary>
        /// <param name="SourceArg"></param>
        /// <param name="DetailObjectArg"></param>
        /// <param name="UserNameArg"></param>
        /// <returns></returns>
        public static AuditEvent New(AuditEventId EventIdArg, object DetailObjectArg = null, [CallerMemberName] string source = "", string UserNameArg = "System", string SessionIdArg = "")
        {
            return new AuditEvent
            {
                TimeStamp = DateTime.Now,
                User = UserNameArg,
                Source = source,
                EventDetailObject = DetailObjectArg,
                Summary = EventIdArg.Name,
                SessionId = SessionIdArg,
                EventId = EventIdArg.Name,
            };
        }

        public long Id { get; set; }

        public DateTime TimeStamp { get; set; }

        public string SessionId { get; set; }

        public string User { get; set; }

        public string Source { get; set; }

        public AuditEventId EventId { get; set; }

        [Column(TypeName = "jsonb")]
        public string EventDetailJsonb { get; set; }

        [NotMapped]
        public object EventDetailObject   // for serialization to jsonb
        {
            get
            {
                return JsonConvert.DeserializeObject(EventDetailJsonb);
            }
            set
            {
                EventDetailJsonb = JsonConvert.SerializeObject(value);
            }
        }
    }
}
