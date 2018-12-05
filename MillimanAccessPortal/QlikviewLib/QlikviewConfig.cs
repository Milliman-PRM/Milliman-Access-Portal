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
        /// URL of the QlikView Management Service (QMS) API on the content publishing server
        /// </summary>
        public string QdsQmsApiUrl { get; set; }

        /// <summary>
        /// URL of the QMS API on the QlikView Server
        /// </summary>
        public string QvsQmsApiUrl { get; set; }

        /// <summary>
        /// Directory name or path to append to the front of the document path (the root QlikView document directory as configured in QlikView Server)
        /// </summary>
        public string QvServerContentUriSubfolder { get; set; } = "";

        /// <summary>
        /// Semicolon separated list of username domains that receive named CAL assignment
        /// </summary>
        public string QvNamedCalDomainList { get; set; } = "";

        /// <summary>
        /// Semicolon separated list of usernames that receive named CAL assignment
        /// </summary>
        public string QvNamedCalUsernameList { get; set; } = "";

        /// <summary>
        /// Cast operator, returns a NetworkCredential object based on an object of this type; requires explicit cast. 
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
