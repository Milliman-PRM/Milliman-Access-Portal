/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing hosted content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.HostedContentViewModels;
using MapCommonLib.ContentTypeSpecific;
using QlikviewLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapCommonLib;
using AuditLogLib;
using AuditLogLib.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Authorization;


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
        private readonly IAuthorizationService AuthorizationService;
        private readonly IAuditLogger AuditLogger;

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
            StandardQueries QueryArg,
            IAuthorizationService AuthorizationServiceArg,
            IAuditLogger AuditLoggerArg)
        {
            QlikviewConfig = QlikviewOptionsAccessorArg.Value;
            UserManager = UserManagerArg;
            Logger = LoggerFactoryArg.CreateLogger<HostedContentController>();
            DataContext = DataContextArg;
            Queries = QueryArg;
            AuthorizationService = AuthorizationServiceArg;
            AuditLogger = AuditLoggerArg;
        }

        /// <summary>
        /// Presents the user with links to all authorized content.  This is the application landing page.
        /// </summary>
        /// <returns>The view</returns>
        [Authorize]
        public IActionResult Index()
        {
            List<HostedContentViewModel> ModelForView = Queries.GetAssignedUserGroups(UserManager.GetUserName(HttpContext.User));

            return View(ModelForView);
        }

        /// <summary>
        /// Handles a request to display content that is hosted by a web server. 
        /// </summary>
        /// <param name="Id">The primary key value of the ContentItemUserGroup authorizing this user to the requested content</param>
        /// <returns>A View (and model) that displays the requested content</returns>
        [Authorize]
        public async Task<IActionResult> WebHostedContent(long Id)
        {
#region Validation
            ContentItemUserGroup UserGroup = DataContext.ContentItemUserGroup
                                                        .Include(ug => ug.RootContentItem)
                                                            .ThenInclude(rc => rc.ContentType)
                                                        .Where(ug => ug.Id == Id)
                                                        .FirstOrDefault();
            if (UserGroup == null || UserGroup.RootContentItem == null || UserGroup.RootContentItem.ContentType == null)
            {
                string ErrMsg = $"Failed to obtain the requested user group, root content item, or content type";
                Logger.LogError(ErrMsg);

                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
                // something that appropriately returns to a logical next view
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new MapAuthorizationRequirementBase[]
                {
                    new UserInContentItemUserGroupRequirement(Id),
                    new RoleInRootContentItemRequirement(RoleEnum.ContentUser, UserGroup.RootContentItem.Id),
                });
            if (!Result1.Succeeded)
            {
                AuditEvent LogObject = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Unauthorized request", AuditEventId.Unauthorized, null, UserManager.GetUserName(HttpContext.User), HttpContext.Session.Id);
                LogObject.EventDetailObject = new { GroupIdRequested = Id };
                AuditLogger.Log(LogObject);

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
#endregion

            // Instantiate the right content handler class
            ContentTypeSpecificApiBase ContentSpecificHandler = null;
            switch (UserGroup.RootContentItem.ContentType.Name)
            {   // Never break out of this switch without a valid ContentSpecificHandler object
                case "Qlikview":
                    ContentSpecificHandler = new QlikviewLibApi();
                    break;

                //case "Another web hosted type":
                //    ContentSpecificHandler = new AnotherTypeSpecificLib();
                //    break;

                default:
                    TempData["Message"] = $"Display of an unsupported ContentType was requested: {UserGroup.RootContentItem.ContentType.Name}";
                    TempData["ReturnToController"] = "HostedContent";
                    TempData["ReturnToAction"] = "Index";
                    return RedirectToAction(nameof(ErrorController.Error), nameof(ErrorController).Replace("Controller", ""));
            }

            UriBuilder ContentUri = await ContentSpecificHandler.GetContentUri(UserGroup, HttpContext, QlikviewConfig);

            HostedContentViewModel ResponseModel = new HostedContentViewModel
            {
                Url = ContentUri.Uri.AbsoluteUri,  // must be absolute because it is used in iframe element
                UserGroupId = UserGroup.Id,
                ContentName = UserGroup.RootContentItem.ContentName,
            };

            // Now return the appropriate view for the requested content
            switch (UserGroup.RootContentItem.ContentType.Name)
            {
                case "Qlikview":
                    return View(ResponseModel);

                //case "Another web hosted type":
                    //return TheRightThing;

                default:
                    // Perhaps this can't happen since this case is handled above
                    TempData["Message"] = $"An unsupported ContentType was requested: {UserGroup.RootContentItem.ContentType.Name}";
                    TempData["ReturnToController"] = "HostedContent";
                    TempData["ReturnToAction"] = "Index";
                    return RedirectToAction(nameof(ErrorController.Error), nameof(ErrorController).Replace("Controller", ""));
            }
        }

        }

    }
}