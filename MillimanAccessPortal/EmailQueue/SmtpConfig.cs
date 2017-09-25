/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Thin class providing the expected structure of smtp.json (or its environment-specific derivatives)
 * DEVELOPER NOTES: 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailQueue
{
    public class SmtpConfig
    {
        public string SmtpServer { get; set;}
        public int SmtpPort { get; set; }
        public string SmtpFromAddress { get; set; }
        public string SmtpFromName { get; set; }

    }
}
