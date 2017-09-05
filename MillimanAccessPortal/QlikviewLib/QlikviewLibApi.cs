using System;
using System.Collections.Generic;
using System.Text;
using MapCommonLib.ContentTypeSpecific;
using MapCommonLib;
using QlikviewLib.Internal;
using MapDbContextLib.Context;

namespace QlikviewLib
{
    public class QlikviewLibApi : ContentTypeSpecificApiBase
    {
        private static string QvServerUriScheme = "http";

        public override UriBuilder GetContentUri(ContentItemUserGroup GroupEntity, string UserName, object ConfigInfoArg)
        {
            QlikviewConfig ConfigInfo = (QlikviewConfig)ConfigInfoArg;

            string QlikviewWebTicket = QvServerOperations.GetQvWebTicket(/*@"Custom\" +*/ UserName, ConfigInfo as QlikviewConfig);

            string[] QueryStringItems = new string[]
            {
                $"type=html",
                $"try=/qvajaxzfc/opendoc.htm?document={GroupEntity.ContentInstanceUrl}",  // TODO use the relative document path/name in the following
                $"back=/",  // TODO probably use something other than "/" (such as a proper error page)
                $"webticket={QlikviewWebTicket}",
            };

            UriBuilder QvServerUri = new UriBuilder
            {
                // Note that the UriBuilder manages the insertion of literal '?' before the query string.  
                // You can't include a query string in the Path property because the '?' gets UrlEncoded.  
                Scheme = QvServerUriScheme,
                Host = ConfigInfo.QvServerHost,
                Path = "/qvajaxzfc/Authenticate.aspx",
                Query = string.Join("&", QueryStringItems),
            };

            return QvServerUri;
        }
    }
}
