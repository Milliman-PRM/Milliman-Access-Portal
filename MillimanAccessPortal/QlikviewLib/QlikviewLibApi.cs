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

            string QlikviewWebTicket = QvServerOperations.GetQvWebTicket(UserName, ConfigInfo as QlikviewConfig);

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
                Host = ConfigInfo.QvServerHost,
                Path = "/qvajaxzfc/Authenticate.aspx",
                Query = string.Join("&", QueryStringItems),
            };

            return QvServerUri;
        }
    }
}
