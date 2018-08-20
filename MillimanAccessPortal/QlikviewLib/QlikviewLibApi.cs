/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Public API of Qlikview functionality, including overrides of MapCommonLib and type specific items
 * DEVELOPER NOTES: This API should typically provide relatively thin API methods and invoke methods from the .internal namespace
 */

using System;
using System.IO;
using System.Threading.Tasks;
using MapCommonLib.ContentTypeSpecific;
using QlikviewLib.Internal;

namespace QlikviewLib
{
    public class QlikviewLibApi : ContentTypeSpecificApiBase
    {
        public override async Task<UriBuilder> GetContentUri(string SelectionGroupUrl, string UserName, object ConfigInfoArg)
        {
            QlikviewConfig ConfigInfo = (QlikviewConfig)ConfigInfoArg;
            string ContentUrl = string.IsNullOrWhiteSpace(ConfigInfo.QvServerContentUriRootPath) ? 
                Path.Combine(ConfigInfo.QvServerContentUriRootPath, SelectionGroupUrl) : SelectionGroupUrl;

            string QvServerUriScheme = "https";  // Scheme of the iframe should match scheme of the top page

            // TODO Resolve the user naming convention for the QV server.  
            string QlikviewWebTicket = await QvServerOperations.GetQvWebTicket(/*@"Custom\" +*/ UserName, ConfigInfo as QlikviewConfig);

            string[] QueryStringItems = new string[]
            {
                $"type=html",
                $"try=/qvajaxzfc/opendoc.htm?document={ContentUrl}",  // TODO use the relative document path/name in the following
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
