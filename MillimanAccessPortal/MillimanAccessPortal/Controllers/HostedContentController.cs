/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing hosted content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using MillimanAccessPortal.Models.HostedContentViewModels;
using MapCommonLib.ContentTypeSpecific;
using QlikviewLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapCommonLib;
using AuditLogLib;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using MillimanAccessPortal.DataQueries;


namespace MillimanAccessPortal.Controllers
{
    public class HostedContentController : Controller
    {
        // Things provided by the application that this controller should need to use
        private QlikviewConfig QlikviewConfig { get; }  // do not allow set
        private ApplicationDbContext DataContext = null;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly ILogger Logger;
        private readonly StandardQueries Queries;

        /// <summary>
        /// Constructor.  Makes instance copies of injected resources from the application. 
        /// </summary>
        /// <param name="UserManagerArg"></param>
        /// <param name="LoggerFactoryArg"></param>
        /// <param name="DataContextArg"></param>
        /// <param name="QlikviewOptionsAccessorArg"></param>
        public HostedContentController(
            IOptions<QlikviewConfig> QlikviewOptionsAccessorArg,
            UserManager<ApplicationUser> UserManagerArg,
            ILoggerFactory LoggerFactoryArg,
            ApplicationDbContext DataContextArg)
        {
            QlikviewConfig = QlikviewOptionsAccessorArg.Value;
            UserManager = UserManagerArg;
            Logger = LoggerFactoryArg.CreateLogger<HostedContentController>();
            DataContext = DataContextArg;
            Queries = new StandardQueries(DataContext);
        }

        /// <summary>
        /// Index handler to present the user with links to all authorized content
        /// </summary>
        /// <returns>The view</returns>
        [Authorize]
        public IActionResult Index()
        {
            List<HostedContentViewModel> ModelForView = Queries.GetAuthorizedUserGroupsAndRoles(UserManager.GetUserName(HttpContext.User));

            return View(ModelForView);
        }

        /// <summary>
        /// Handles a request to display content that is hosted by a web server. 
        /// </summary>
        /// <param name="Id">The primary key value of the ContentItemUserGroup authorizing this user to the requested content</param>
        /// <returns>A View (and model) that displays the requested content</returns>
        [Authorize]
        public IActionResult WebHostedContent(long Id)
        {
            AuditLogger AuditStore = new AuditLogger();

            try
            {
                // Get the requested (by id) ContentItemUserGroup object
                ContentItemUserGroup AuthorizedUserGroup = Queries.GetUserGroupIfAuthorizedToRole(UserManager.GetUserName(HttpContext.User), Id, RoleEnum.ContentUser);

                if (AuthorizedUserGroup == null)
                {
                    AuditEvent LogObject = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Unauthorized request", null, UserManager.GetUserName(HttpContext.User));
                    LogObject.EventDetailObject = new { GroupIdRequested = Id };
                    AuditStore.Log(LogLevel.Warning, AuditEventId.LoginSuccess, LogObject);

                    TempData["Message"] = $"You are not authorized to view the requested content (#{Id})";
                    TempData["ReturnToController"] = "HostedContent";
                    TempData["ReturnToAction"] = "Index";

                    return RedirectToAction(nameof(ErrorController.NotAuthorized), nameof(ErrorController).Replace("Controller", ""));
                }

                // Get the ContentType of the RootContentItem of the requested group
                IQueryable<ContentType> Query = DataContext.RootContentItem
                    .Where(item => item.Id == AuthorizedUserGroup.RootContentItemId)
                    .Join(DataContext.ContentType, r => r.ContentTypeId, type => type.Id, (r, type) => type);  // result is the ContentType record

                // execute the query
                ContentType RequestedContentType = Query.FirstOrDefault();
                string TypeOfRequestedContent = (RequestedContentType != null) ? RequestedContentType.Name : "Unknown";

                // Instantiate the right content handler class
                ContentTypeSpecificApiBase ContentSpecificHandler = null;
                switch (TypeOfRequestedContent)
                {   // Never break out of this switch without a valid ContentSpecificHandler object
                    case "Qlikview":
                        ContentSpecificHandler = new QlikviewLibApi();
                        break;

                    //case "Another web hosted type":
                    //    ContentSpecificHandler = new AnotherTypeSpecificLib();
                    //    break;

                    default:
                        TempData["Message"] = $"Display of an unsupported ContentType was requested: {TypeOfRequestedContent}";
                        TempData["ReturnToController"] = "HostedContent";
                        TempData["ReturnToAction"] = "Index";
                        return RedirectToAction(nameof(ErrorController.Error), nameof(ErrorController).Replace("Controller", ""));
                }

                UriBuilder ContentUri = ContentSpecificHandler.GetContentUri(AuthorizedUserGroup, HttpContext, QlikviewConfig);
                RootContentItem Content = DataContext.RootContentItem.Where(r => r.Id == Id).First();

                HostedContentViewModel ResponseModel = new HostedContentViewModel
                {
                    Url = ContentUri.Uri.AbsoluteUri,  // must be absolute because it is used in iframe element
                    UserGroupId = AuthorizedUserGroup.Id,
                    ContentName = Content.ContentName,
                    RoleNames = new HashSet<string>(),  // empty
                };

                // Now return the appropriate view for the requested content
                switch (TypeOfRequestedContent)
                {
                    case "Qlikview":
                        return View(ResponseModel);

                    //case "Another web hosted type":
                        //return TheRightThing;

                    default:
                        // Perhaps this can't happen since this case is handled above
                        TempData["Message"] = $"An unsupported ContentType was requested: {TypeOfRequestedContent}";
                        TempData["ReturnToController"] = "HostedContent";
                        TempData["ReturnToAction"] = "Index";
                        return RedirectToAction(nameof(ErrorController.Error), nameof(ErrorController).Replace("Controller", ""));
                }
            }
            catch (MapException e)
            {
                TempData["Message"] = $"{e.Message}<br>{e.StackTrace}";
                TempData["ReturnToController"] = "HostedContent";
                TempData["ReturnToAction"] = "Index";
                return RedirectToAction(nameof(ErrorController.Error), nameof(ErrorController).Replace("Controller", ""));
            }

        }

    }
}