using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapCommonLib;

namespace QlikviewLib
{
    /// <summary>
    /// Represents configuration parameters required by the application to invoke Qlikview server functionality
    /// </summary>
    public class QlikviewConfig
    {
        public string QvServerHost { get; set; }
        public string QvServerUserName { get; set; }
        public string QvServerPassword { get; set; }
    }
}
