using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class Client
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public List<string> AcceptedEmailDomainList { get; set; }

        [ForeignKey("ParentClient")]
        public long? ParentClientId { get; set; }

        public Client ParentClient { get; set; }
    }
}
