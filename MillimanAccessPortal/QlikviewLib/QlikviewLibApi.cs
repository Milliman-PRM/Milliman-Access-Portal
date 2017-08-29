using System;
using System.Collections.Generic;
using System.Text;
using MapCommonLib.ContentTypeSpecific;
using QlikviewLib.Internal;
using MapDbContextLib.Context;

namespace QlikviewLib
{
    public class QlikviewLibApi : ContentTypeSpecificApiBase
    {
        private static string QvServerHostName = "prm.milliman.com";   // TODO Put this in config
        //private static string QvServerHostName = "indy-qvtest01.milliman.com";   // TODO Put this in config
        private static string QvServerUriScheme = "http";

        public override UriBuilder GetContentUri(ContentItemUserGroup GroupEntity, string UserName)
        {
            string QlikviewWebTicket = QvServerOperations.GetQvWebTicket(UserName);

            string[] QueryStringItems = new string[]
            {
                string.Format("type=html"),
                // TODO use the relative document path/name in the following
                string.Format("try=/qvajaxzfc/opendoc.htm?document={0}", GroupEntity.ContentInstanceUrl),
                string.Format("back=/"),
                string.Format("webticket={0}", QlikviewWebTicket),
            };

            UriBuilder QvServerUri = new UriBuilder
            {
                Scheme = QvServerUriScheme,
                Host = QvServerHostName,
                Path = "QvAJAXZfc/Authenticate.aspx",
                Query = string.Join("&", QueryStringItems),
            };
            
            return QvServerUri;
        }
    }
}
