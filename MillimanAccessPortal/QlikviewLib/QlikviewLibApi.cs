using System;
using System.Collections.Generic;
using System.Text;
using MapCommonLib.ContentTypeSpecific;
using QlikviewLib.Internal;

namespace QlikviewLib
{
    public class QlikviewLibApi : ContentTypeSpecificApiBase
    {
        private static string QvServerHostName = "indy-qvtest01.milliman.com";   // TODO Put this in config
        private static string QvServerUriScheme = "http";

        public override UriBuilder GetContentUri()
        {
            string QlikviewWebTicket = QvServerOperations.GetQvWebTicket("Tom");

            UriBuilder QvServerUri = new UriBuilder
            {
                Scheme = QvServerUriScheme,
                Host = QvServerHostName,
                Path = "QvAJAXZfc/Authenticate.aspx",
                // TODO get this right, especially document name
                Query = "type=html&try=/qvajaxzfc/opendoc.htm?document=" + "Mydoc" + "&back=/&webticket=" + QlikviewWebTicket,
            };
            
            return QvServerUri;
        }
    }
}
