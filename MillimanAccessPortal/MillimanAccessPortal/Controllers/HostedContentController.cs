using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Models.HostedContentViewModels;

namespace MillimanAccessPortal.Controllers
{
    public class HostedContentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult WebHostedContent()
        {
            string QlikviewWebTicket = string.Empty;
            string WebTicketRequestXml = string.Empty;   // string.Format("<Global method=\"GetWebTicket\"><UserId>{0}</UserId>{1}</Global>", _userId, _userGroups);
            WebTicketRequestXml = "<Global method = \"GetWebTicket\"><UserId>FRED</UserId></Global>";
            //<GroupList><string></string></GroupList>
            //<GroupsIsNames>true</GroupsIsNames>

            string Response = PostSomething(WebTicketRequestXml);
            /*
             * from sample app
            using (StreamWriter sw = new StreamWriter(client.GetRequestStream()))
                sw.WriteLine(webTicketXml);
            StreamReader sr = new StreamReader(client.GetResponse().GetResponseStream());
            string result = sr.ReadToEnd();

            XDocument doc = XDocument.Parse(result);
            return doc.Root.Element("_retval_").Value;
            */

            WebHostedContentViewModel Model = new WebHostedContentViewModel
            {
                Url = "https://indy-ss01.milliman.com/QvAJAXZfc/AccessPoint.aspx",
                QueryString = "open=&id=QVS%40indy-ss01%7CNoEPHI%2F2016Q4v2-v4.0.4-IMP-Submitted-June2017.qvw&client=Ajax",
            };

            return View(Model);
        }

        private string PostSomething(string Body)
        {
            string Address = "https://indy-ss01.milliman.com/QvAJAXZfc/GetWebTicket.aspx";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Address);
/*            if (!Anonymous)
            {
                if (UserName == "" && Password == "")
                    request.UseDefaultCredentials = true;
                else
                    request.Credentials = new NetworkCredential(UserName, Password);

                request.PreAuthenticate = true;
            }*/
            request.Method = "POST";
            //request.Timeout = 30000;
            request.ContentType = "application/x-www-form-urlencoded";
            //request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            //var buffer = Encoding.UTF8.GetBytes(data);
            var buffer = Encoding.UTF8.GetBytes(Body);
            //request.ContentLength = buffer.Length;
            var dataStream = request.BeginGetRequestStream.GetRequestStream();
            dataStream.Write(buffer, 0, buffer.Length);
            dataStream.Close();

            var response = (HttpWebResponse)request.GetResponse();
            var responseStream = response.GetResponseStream();
            var reader = new StreamReader(responseStream, Encoding.UTF8);
            var result = reader.ReadToEnd();

            reader.Close();
            dataStream.Close();
            response.Close();

            return result;
        }
    }
}