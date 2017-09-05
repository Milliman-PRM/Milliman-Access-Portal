using System;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapCommonLib;

namespace QlikviewLib.Internal
{
    internal class QvServerOperations
    {
        private static string QvServerUriScheme = "http";

        internal static string GetQvWebTicket(string UserId, QlikviewConfig QvConfig)
        {
            UriBuilder QvServerUri = new UriBuilder
            {
                Scheme = QvServerUriScheme,
                Host = QvConfig.QvServerHost,
                Path = "qvajaxzfc/getwebticket.aspx",
            };

            string RequestBodyString = string.Format("<Global method=\"GetWebTicket\"><UserId>{0}</UserId></Global>", UserId);
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
                ResponseMsg = client.PostAsync(QvServerUri.Uri, RequestContent).Result;
            }
            catch (Exception e)
            {
                throw new MapException(string.Format("Exception from PostAsync() while calling GetWebTicket.aspx from {0}\r\nMessage: {1}", QvServerUri.Uri.AbsoluteUri, e.Message));
            }

            string ResponseBody = ResponseMsg.Content.ReadAsStringAsync().Result;

            if (!ResponseMsg.IsSuccessStatusCode)
            {
                throw new MapException(string.Format("Failed to obtain Qlikview web ticket from {0},\r\nHTTP status {1},\r\nresponse body: {2}", QvServerUri.Uri.AbsoluteUri, (int)ResponseMsg.StatusCode, ResponseBody));
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
