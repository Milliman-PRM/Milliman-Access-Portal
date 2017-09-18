using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class SmtpConfig
    {
        public string SmtpServer { get; set;}
        public int SmtpPort { get; set; }
        public string SmtpFromAddress { get; set; }
        public string SmtpFromName { get; set; }

    }
}
