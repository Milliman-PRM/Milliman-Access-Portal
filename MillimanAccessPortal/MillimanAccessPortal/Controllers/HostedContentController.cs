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
using MapCommonLib.ContentTypeSpecific;
using QlikviewLib;

namespace MillimanAccessPortal.Controllers
{
    public class HostedContentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult WebHostedContent(int ContentPk)
        {
            ContentTypeSpecificApiBase ContentSpecificHandler = null;

            ContentType TypeOfContent = ContentType.Qlikview; // TODO query for the content type based on ContentPk
            switch (TypeOfContent)
            {
                case ContentType.Qlikview:
                    ContentSpecificHandler = new QlikviewLibApi();
                    break;

                default:
                    return View("SomeError_View", new object(/*SomeModel*/));
            }

            UriBuilder ContentUri = ContentSpecificHandler.GetContentUri();

            WebHostedContentViewModel Model = new WebHostedContentViewModel
            {
                Url = ContentUri.Uri,
            };

            return View(Model);
        }

    }
}