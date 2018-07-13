/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing hosted content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Event;
using AuditLogLib.Services;
using MapCommonLib;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.AuthorizedContentViewModels;
using QlikviewLib;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace MillimanAccessPortal.Controllers
{
    public class AuthorizedContentController : Controller
    {
        // Things provided by the application that this controller should need to use
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ApplicationDbContext DataContext;
        private readonly ILogger Logger;
        private readonly QlikviewConfig QlikviewConfig;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> UserManager;

        /// <summary>
        /// Constructor.  Makes instance copies of injected resources from the application. 
        /// </summary>
        /// <param name="UserManagerArg"></param>
        /// <param name="LoggerFactoryArg"></param>
        /// <param name="DataContextArg"></param>
        /// <param name="QlikviewOptionsAccessorArg"></param>
        public AuthorizedContentController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ApplicationDbContext DataContextArg,
            ILoggerFactory LoggerFactoryArg,
            IOptions<QlikviewConfig> QlikviewOptionsAccessorArg,
            StandardQueries QueryArg,
            UserManager<ApplicationUser> UserManagerArg)
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            DataContext = DataContextArg;
            Logger = LoggerFactoryArg.CreateLogger<AuthorizedContentController>();
            QlikviewConfig = QlikviewOptionsAccessorArg.Value;
            Queries = QueryArg;
            UserManager = UserManagerArg;
        }

        /// <summary>
        /// Presents the user with links to all authorized content. This is the application landing page.
        /// </summary>
        /// <returns>The view</returns>
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Content()
        {
            var model = AuthorizedContentViewModel.Build(DataContext, await Queries.GetCurrentApplicationUser(User));

            return Json(model);
        }

        /// <summary>
        /// Handles a request to display content that is hosted by a web server. 
        /// </summary>
        /// <param name="Id">The primary key value of the SelectionGroup authorizing this user to the requested content</param>
        /// <returns>A View (and model) that displays the requested content</returns>
        [Authorize]
        public async Task<IActionResult> WebHostedContent(long Id)
        {
            #region Validation
            SelectionGroup SelGroup = DataContext.SelectionGroup
                                                        .Include(sg => sg.RootContentItem)
                                                            .ThenInclude(rc => rc.ContentType)
                                                        .Where(sg => sg.Id == Id)
                                                        .FirstOrDefault();
            if (SelGroup == null || SelGroup.RootContentItem == null || SelGroup.RootContentItem.ContentType == null)
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
                    new UserInSelectionGroupRequirement(Id),
                    new RoleInRootContentItemRequirement(RoleEnum.ContentUser, SelGroup.RootContentItem.Id),
                });
            if (!Result1.Succeeded)
            {
                AuditEvent LogObject = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Unauthorized request", AuditEventType.Unauthorized, null, UserManager.GetUserName(HttpContext.User), HttpContext.Session.Id);
                LogObject.EventDataObject = new { GroupIdRequested = Id };
                AuditLogger.Log(LogObject);

                Response.Headers.Add("Warning", $"You are not authorized to access the requested content");
                return Unauthorized();
            }
            #endregion

            try
            {
                // Instantiate the right content handler class
                ContentTypeSpecificApiBase ContentSpecificHandler = null;
                switch (SelGroup.RootContentItem.ContentType.TypeEnum)
                {   // Never break out of this switch without a valid ContentSpecificHandler object
                    case ContentTypeEnum.Qlikview:
                        ContentSpecificHandler = new QlikviewLibApi();
                        break;

                    //case ContentTypeEnum.SomeOther":
                    //    ContentSpecificHandler = new AnotherTypeSpecificLib();
                    //    break;

                    default:
                        TempData["Message"] = $"Display of an unsupported ContentType was requested: {SelGroup.RootContentItem.ContentType.Name}";
                        TempData["ReturnToController"] = "AuthorizedContent";
                        TempData["ReturnToAction"] = "Index";
                        return RedirectToAction(nameof(ErrorController.Error), nameof(ErrorController).Replace("Controller", ""));
                }

                UriBuilder ContentUri = await ContentSpecificHandler.GetContentUri(SelGroup, HttpContext, QlikviewConfig);

                AuthorizedContentViewModel ResponseModel = new AuthorizedContentViewModel
                {
                    // Url = ContentUri.Uri.AbsoluteUri,  // must be absolute because it is used in iframe element
                    // UserGroupId = SelGroup.Id,
                    // ContentName = SelGroup.RootContentItem.ContentName,
                };

                // Now return the appropriate view for the requested content
                switch (SelGroup.RootContentItem.ContentType.Name)
                {
                    case "Qlikview":
                        return View(ResponseModel);

                    //case "Another web hosted type":
                        //return TheRightThing;

                    default:
                        // Perhaps this can't happen since this case is handled above
                        TempData["Message"] = $"An unsupported ContentType was requested: {SelGroup.RootContentItem.ContentType.Name}";
                        TempData["ReturnToController"] = "AuthorizedContent";
                        TempData["ReturnToAction"] = "Index";
                        return RedirectToAction(nameof(ErrorController.Error), nameof(ErrorController).Replace("Controller", ""));
                }
            }
            catch (MapException e)
            {
                TempData["Message"] = $"{e.Message}<br>{e.StackTrace}";
                TempData["ReturnToController"] = "AuthorizedContent";
                TempData["ReturnToAction"] = "Index";
                return RedirectToAction(nameof(ErrorController.Error), nameof(ErrorController).Replace("Controller", ""));
            }
        }
    }
}
