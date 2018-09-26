/*
* CODE OWNERS: Tom Puckett
* OBJECTIVE: For internal use by this class library only.  Some methods relating to interface with a Qlikview server.  
* DEVELOPER NOTES: No publicly consumable API elements should be implemented here. For internal use only. 
*/

using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using MapCommonLib;

namespace QlikviewLib.Internal
{
    internal class QvServerOperations
    {
        internal static async Task<string> GetQvWebTicket(string UserId, QlikviewConfig QvConfig)
        {
            UriBuilder QvServerUri = new UriBuilder
            {
                Scheme = "https",  // Do we need to support choice of http and https in the event that a certificate issue causes failure
                Host = QvConfig.QvServerHost,
                Path = "qvajaxzfc/getwebticket.aspx",
            };

            string RequestBodyString = $"<Global method=\"GetWebTicket\"><UserId>{UserId}</UserId></Global>";
            Uri x = QvServerUri.Uri;

            var Handler = new HttpClientHandler
            {
                Credentials = (NetworkCredential) QvConfig,  // conversion operator defined in class QlikviewConfig
            };

            HttpClient client = new HttpClient(Handler);
            StringContent RequestContent = new StringContent(RequestBodyString);
            HttpResponseMessage ResponseMsg = null;
            try
            {
                ResponseMsg = await client.PostAsync(QvServerUri.Uri, RequestContent);
            }
            catch (Exception e)
            {
                throw new MapException($"Exception from PostAsync() while calling GetWebTicket.aspx from {QvServerUri.Uri.AbsoluteUri}{Environment.NewLine}Message: {e.Message}");
            }

            string ResponseBody = await ResponseMsg.Content.ReadAsStringAsync();

            if (!ResponseMsg.IsSuccessStatusCode)
            {
                throw new MapException($"Failed to obtain Qlikview web ticket from: {QvServerUri.Uri.AbsoluteUri}{Environment.NewLine}" +
                                       $"HTTP status: {(int)ResponseMsg.StatusCode}{Environment.NewLine}" +
                                       $"response body: {ResponseBody}");
            }

            string Ticket = string.Empty;
            try
            {
                XDocument doc = XDocument.Parse(ResponseBody);
                Ticket = doc.Root.Element("_retval_").Value;
            }
            catch
            {
                throw new MapException(string.Format("Qlikview web ticket not found in server response"));
            }

            return Ticket;
        }

    }
}
