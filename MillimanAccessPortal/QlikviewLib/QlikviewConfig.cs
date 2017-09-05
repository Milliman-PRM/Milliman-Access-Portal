using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapCommonLib;
using System.Net;

namespace QlikviewLib
{
    /// <summary>
    /// Represents configuration parameters required by the application to invoke Qlikview server functionality
    /// </summary>
    public class QlikviewConfig
    {
        public string QvServerHost { get; set; }
        public string QvServerAdminUserAuthenticationDomain { get; set; }
        public string QvServerAdminUserName { get; set; }
        public string QvServerAdminUserPassword { get; set; }

        /// <summary>
        /// Converts config values to a NetworkCredential object, requires an explicit cast. 
        /// </summary>
        /// <param name="C"></param>
        public static explicit operator NetworkCredential(QlikviewConfig C)
        {
            return string.IsNullOrEmpty(C.QvServerAdminUserAuthenticationDomain) ?
                new NetworkCredential(C.QvServerAdminUserName, C.QvServerAdminUserPassword) :
                new NetworkCredential(C.QvServerAdminUserName, C.QvServerAdminUserPassword, C.QvServerAdminUserAuthenticationDomain);
        }
    }
}
