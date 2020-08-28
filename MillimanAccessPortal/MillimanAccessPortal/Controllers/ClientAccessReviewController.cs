/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing authorized content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapCommonLib;
using MillimanAccessPortal.DataQueries;
using AuditLogLib;
using AuditLogLib.Event;
using AuditLogLib.Services;
using MillimanAccessPortal.Models.ClientAdminViewModels;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MillimanAccessPortal.Models.AccountViewModels;

namespace MillimanAccessPortal.Controllers
{
    public class ClientAccessReviewController : Controller
    {
        private readonly ApplicationDbContext DbContext;
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly IMessageQueue MessageQueueService;
        private readonly RoleManager<ApplicationRole> RoleManager;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration ApplicationConfig;
        private readonly AccountController _accountController;

        public ClientAccessReviewController(
            ApplicationDbContext context,
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            IMessageQueue MessageQueueServiceArg,
            RoleManager<ApplicationRole> RoleManagerArg,
            StandardQueries QueryArg,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration ApplicationConfigArg,
            AccountController AccountControllerArg
            )
        {
            DbContext = context;
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            MessageQueueService = MessageQueueServiceArg;
            RoleManager = RoleManagerArg;
            Queries = QueryArg;
            _userManager = UserManagerArg;
            ApplicationConfig = ApplicationConfigArg;
            _accountController = AccountControllerArg;
        }

        // GET: ClientAccessReview
        /// <summary>
        /// Action leading to the main landing page for Client administration UI
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            #region Authorization
            // User must have Admin role to at least 1 Client OR to at least 1 ProfitCenter
            AuthorizationResult Result = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!Result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to the Client Access Review page.");
                return Unauthorized();
            }
            #endregion

            return View();
        }

        // GET: ClientAccessReview/ClientFamilyList
        /// <summary>
        /// Returns the list of Client families that the current user has visibility to (defined by GetClientAdminIndexModelForUser(...)
        /// </summary>
        /// <returns>JsonResult or UnauthorizedResult</returns>
        [HttpGet]
        public async Task<IActionResult> ClientFamilyList()
        {
            #region Authorization
            // User must have Admin role to at least 1 Client or ProfitCenter
            AuthorizationResult Result = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!Result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized as a Client Admin");
                return Unauthorized();
            }
            #endregion

            ClientAdminIndexViewModel ModelToReturn = await ClientAdminIndexViewModel.GetClientAdminIndexModelForUser(await _userManager.GetUserAsync(User), _userManager, DbContext, ApplicationConfig["Global:DefaultNewUserWelcomeText"]);

            return Json(ModelToReturn);
        }
    }
}
