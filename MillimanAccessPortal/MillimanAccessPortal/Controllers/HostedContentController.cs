using System;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Models.HostedContentViewModels;
using System.Net.Http.Headers;

namespace MillimanAccessPortal.Controllers
{
    public class HostedContentController : Controller
    {
        // TODO Put these in config
        private static string QvServerHostName = "indy-qvtest01.milliman.com";   // Get this from configuration
        private static string QvServerUriScheme = "http";   // Determine this from configuration

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult WebHostedContent()
        {
            string QlikviewWebTicket = GetQvWebTicket("Tom");  // TODO pass the authenticated end user's id instead

            UriBuilder QvServerUri = new UriBuilder
            {
                Scheme = QvServerUriScheme,
                Host = QvServerHostName,
                Path = "QvAJAXZfc/Authenticate.aspx",
                // TODO get this right, especially document name
                Query = "type=html&try=/qvajaxzfc/opendoc.htm?document=" + "Mydoc" + "&back=/&webticket=" + QlikviewWebTicket,
            };

            WebHostedContentViewModel Model = new WebHostedContentViewModel
            {
                Url = QvServerUri.Uri.ToString(),
            };

            return View(Model);
        }

        /// <summary>
        /// This method will eventually move to a Qlikview focused project of its own
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns></returns>
        private string GetQvWebTicket(string UserId)
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
                Credentials = new NetworkCredential(@"tom.puckett", ""),  // TODO Get the credentials from configuration
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