using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Models.HostedContentViewModels;
using MapCommonLib.ContentTypeSpecific;
using QlikviewLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapCommonLib;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace MillimanAccessPortal.Controllers
{
    public class HostedContentController : Controller
    {
        private ApplicationDbContext DataContext = null;

        private readonly UserManager<ApplicationUser> UserManager;
        private readonly IOptions<QlikviewConfig> OptionsAccessor;
        private readonly ILogger Logger;

        /// <summary>
        /// Constructor.  Accepts injected resources. 
        /// </summary>
        /// <param name="UserManagerArg"></param>
        /// <param name="LoggerFactoryArg"></param>
        /// <param name="DataContextArg"></param>
        /// <param name="OptionsAccessorArg"></param>
        public HostedContentController(
            UserManager<ApplicationUser> UserManagerArg,
            ILoggerFactory LoggerFactoryArg,
            ApplicationDbContext DataContextArg,
            IOptions<QlikviewConfig> OptionsAccessorArg)
        {
            UserManager = UserManagerArg;
            Logger = LoggerFactoryArg.CreateLogger<HostedContentController>();
            DataContext = DataContextArg;
            OptionsAccessor = OptionsAccessorArg;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Id">The primary key value of the user group for the requested content</param>
        /// <returns></returns>
        public IActionResult WebHostedContent(long Id)
        {
            string TypeOfRequestedContent = "Unknown";
            ContentItemUserGroup UserGroupOfRequestedContent = null;

            try
            {
                UserGroupOfRequestedContent = DataContext.ContentItemUserGroup.Where(g => g.Id == Id).FirstOrDefault();

                // Get the ContentType of the RootContentItem of the requested group
                IQueryable<ContentType> Query = DataContext.RootContentItem
                    .Where(item => item.Id == UserGroupOfRequestedContent.RootContentItemId)
                    .Join(DataContext.ContentType, r => r.ContentTypeId, type => type.Id, (r, type) => type);  // result is the ContentType record

                // execute the query
                ContentType RequestedContentType = Query.FirstOrDefault();
                // TODO need this:   if (RequestedContentType == null) {what?}
                TypeOfRequestedContent = RequestedContentType.Name;
            }
            catch (Exception e)
            {
                string Msg = e.Message;
                // The requested user group or associated root content item or content type record could not be found in the database
                return View("SomeError_View", new object(/*SomeModel*/));
            }

            // Instantiate the right content handler class
            ContentTypeSpecificApiBase ContentSpecificHandler = null;
            switch (TypeOfRequestedContent)
            {
                case "Qlikview":
                    ContentSpecificHandler = new QlikviewLibApi();
                    break;

                default:
                    // The content type of the requested content is not handled
                    return View("SomeError_View", new object(/*SomeModel*/));
            }

            string UserName = UserManager.GetUserName(HttpContext.User);
            UriBuilder ContentUri = null;
            try
            {
                ContentUri = ContentSpecificHandler.GetContentUri(UserGroupOfRequestedContent, UserName, OptionsAccessor.Value);
            }
            catch (MapException e)
            {
                // Some error encountere while building the content reference
                return View("SomeError_View", new object(/*SomeModel*/));
            }

            // Build a model for the resulting view
            WebHostedContentViewModel Model = new WebHostedContentViewModel
            {
                Url = ContentUri.Uri,
            };

            return View(Model);
        }

    }
}