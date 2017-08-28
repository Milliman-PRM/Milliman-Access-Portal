using System;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QlikviewLib.Internal
{
    internal class QvServerOperations
    {
        private static string QvServerHostName = "indy-qvtest01.milliman.com";   // TODO Put this in config
        private static string QvServerUriScheme = "http";

        internal static string GetQvWebTicket(string UserId)
        {
            UriBuilder QvServerUri = new UriBuilder
            {
                Scheme = QvServerUriScheme,
                Host = QvServerHostName,
                Path = "QVAJAXZFC/getwebticket.aspx",
            };

            string RequestBodyString = string.Format("<Global method=\"GetWebTicket\"><UserId>{0}</UserId></Global>", UserId);
            Uri x = QvServerUri.Uri;

            var Handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(@"tom.puckett", "Indiana.5235"),  // TODO Get the credentials from configuration
            };

            HttpClient client = new HttpClient(Handler);
            StringContent RequestContent = new StringContent(RequestBodyString);
            HttpResponseMessage ResponseMsg = null;
            try
            {
                ResponseMsg = client.PostAsync(QvServerUri.Uri, RequestContent).Result;
            }
            catch
            {
                // TODO log something
                return string.Empty;
            }

            string ResponseBody = string.Empty;
            using (var ResponseStream = ResponseMsg.Content.ReadAsStreamAsync().Result)
            {
                using (var ResponseReader = new StreamReader(ResponseStream, Encoding.UTF8))
                {
                    ResponseBody = ResponseReader.ReadToEnd();
                }
            }

            XDocument doc = XDocument.Parse(ResponseBody);
            string Ticket = doc.Root.Element("_retval_").Value;

            return Ticket;
        }

    }
}
