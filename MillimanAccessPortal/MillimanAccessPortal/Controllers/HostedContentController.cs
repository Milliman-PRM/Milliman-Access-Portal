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


namespace MillimanAccessPortal.Controllers
{
    public class HostedContentController : Controller
    {
        // Things provided by the application that this controller should need to use
        private QlikviewConfig QlikviewConfig { get; }  // do not allow set
        private ApplicationDbContext DataContext = null;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly ILogger Logger;
        private readonly IServiceProvider ServiceProvider;

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
            ApplicationDbContext DataContextArg,
            IServiceProvider ServiceProviderArg)
        {
            QlikviewConfig = QlikviewOptionsAccessorArg.Value;
            UserManager = UserManagerArg;
            Logger = LoggerFactoryArg.CreateLogger<HostedContentController>();
            DataContext = DataContextArg;
            ServiceProvider = ServiceProviderArg;
        }

        /// <summary>
        /// Index handler to present the user with links to all authorized content
        /// </summary>
        /// <returns>The view</returns>
        [Authorize]
        public IActionResult Index()
        {
            List<HostedContentViewModel> ModelForView = new StandardQueries(ServiceProvider).GetAuthorizedUserGroupsAndRoles(UserManager.GetUserName(HttpContext.User));

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
                ContentItemUserGroup UserGroupOfRequestedContent = DataContext.ContentItemUserGroup.Where(g => g.Id == Id)
                    .Join(DataContext.UserRoleForContentItemUserGroup, g => g.Id, ur => ur.ContentItemUserGroupId, (g, ur) => new {group=g, userrole=ur })
                    .Where(r => r.userrole.Role.Name == "Content User")
                    .Select(o => o.group)
                    .FirstOrDefault();

                if (UserGroupOfRequestedContent == null)
                {
                    AuditEvent LogObject = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Unauthorized request", null, UserManager.GetUserName(HttpContext.User));
                    LogObject.EventDetailObject = new { GroupIdRequested = Id };
                    AuditStore.Log(LogLevel.Warning, AuditEventId.LoginSuccess, LogObject);
                    return RedirectToAction(nameof(ErrorController.NotAuthorized), "Error", new { RequestedId = Id, ReturnToController = "HostedContent", ReturnToAction = "Index" });
                }

                // Get the ContentType of the RootContentItem of the requested group
                IQueryable<ContentType> Query = DataContext.RootContentItem
                    .Where(item => item.Id == UserGroupOfRequestedContent.RootContentItemId)
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

                    default:
                        // The content type of the requested content is not handled
                        return View("SomeError_View", new object(/*SomeModel*/));  // TODO Get this right
                }

                UriBuilder ContentUri = ContentSpecificHandler.GetContentUri(UserGroupOfRequestedContent, HttpContext, QlikviewConfig);
                RootContentItem Content = DataContext.RootContentItem.Where(r => r.Id == Id).First();

                HostedContentViewModel ResponseModel = new HostedContentViewModel
                {
                    Url = ContentUri.Uri.AbsoluteUri,  // must be absolute because it is used in iframe element
                    UserGroupId = UserGroupOfRequestedContent.Id,
                    ContentName = Content.ContentName,
                    RoleNames = new HashSet<string>(),  // empty
                };

                // Now return the requested content in its view
                switch (TypeOfRequestedContent)
                {
                    case "Qlikview":
                        return View(ResponseModel);

                    default:
                        // Probably can't happen since this is handled above
                        return View("SomeError_View", new object(/*SomeModel*/));  // TODO Get this right
                }
            }
            catch (MapException e)
            {
                string Msg = e.Message + e.StackTrace; // use this and maybe other e.properties
                // The requested user group or associated root content item or content type record could not be found in the database
                return View("SomeError_View", new object(/*SomeModel*/));  // TODO Get this right
            }

        }

    }
}