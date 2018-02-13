/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Public API of Qlikview functionality, including overrides of MapCommonLib and type specific items
 * DEVELOPER NOTES: This API should typically provide relatively thin API methods and invoke methods from the .internal namespace
 */

using System;
using System.Threading.Tasks;
using MapCommonLib.ContentTypeSpecific;
using QlikviewLib.Internal;
using MapDbContextLib.Context;
using Microsoft.AspNetCore.Http;

namespace QlikviewLib
{
    public class QlikviewLibApi : ContentTypeSpecificApiBase
    {
        public override async Task<UriBuilder> GetContentUri(SelectionGroup GroupEntity, HttpContext Context, object ConfigInfoArg)
        {
            QlikviewConfig ConfigInfo = (QlikviewConfig)ConfigInfoArg;

            string QvServerUriScheme = "https";  // Scheme of the iframe should match scheme of the top page
            string EndUserName = Context.User.Identity.Name;  // TODO Is this needed instead?:    string EndUserName = UserManager.GetUserName(HttpContext.User);

            // TODO Resolve the user naming convention for the QV server.  
            string QlikviewWebTicket = await QvServerOperations.GetQvWebTicket(/*@"Custom\" +*/ EndUserName, ConfigInfo as QlikviewConfig);

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
                // Don't include a query string with '?' in the Path property because the '?' gets UrlEncoded.  
                Scheme = QvServerUriScheme,
                Host = ConfigInfo.QvServerHost,
                Path = "/qvajaxzfc/Authenticate.aspx",
                Query = string.Join("&", QueryStringItems),
            };

            return QvServerUri;
        }
    }
}
