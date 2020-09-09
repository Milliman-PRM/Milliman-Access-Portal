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
using MillimanAccessPortal.Models.ClientAccessReview;

namespace MillimanAccessPortal.Controllers
{
    public class ClientAccessReviewController : Controller
    {
        private readonly IAuditLogger _auditLogger;
        private readonly IAuthorizationService _authorizationService;
        private readonly IConfiguration _applicationConfig;
        private readonly ContentAccessAdminQueries _accessAdminQueries;
        private readonly ClientAccessReviewQueries _clientAccessReviewQueries;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClientAccessReviewController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ContentAccessAdminQueries accessAdminQueriesArg,
            ClientAccessReviewQueries ClientAccessReviewQueriesArg,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration ApplicationConfigArg
            )
        {
            _auditLogger = AuditLoggerArg;
            _authorizationService = AuthorizationServiceArg;
            _accessAdminQueries = accessAdminQueriesArg;
            _clientAccessReviewQueries = ClientAccessReviewQueriesArg;
            _userManager = UserManagerArg;
            _applicationConfig = ApplicationConfigArg;
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
            AuthorizationResult Result = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!Result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to the Client Access Review page.");
                return Unauthorized();
            }
            #endregion

            return View();
        }

        /// <summary
        /// GET the configured time period values for review warnings
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PageGlobalData()
        {
            #region Authorization
            var roleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to the Client Access Review page.");
                return Unauthorized();
            }
            #endregion

            var GlobalData = new ClientAccessReviewGlobalDataModel
            {
                ClientReviewEarlyWarningDays = _applicationConfig.GetValue<int>("ClientReviewEarlyWarningDays"),
                ClientReviewGracePeriodDays = _applicationConfig.GetValue<int>("ClientReviewGracePeriodDays"),
            };

            return Json(GlobalData);
        }

        /// <summary>
        /// GET clients authorized to the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            #region Authorization
            var roleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to the Client Access Review page.");
                return Unauthorized();
            }
            #endregion

            var currentUser = await _userManager.GetUserAsync(User);
            var model = await _clientAccessReviewQueries.GetClientModelAsync(currentUser);

            return Json(model);
        }

        /// <summary>
        /// GET client 
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ClientSummary(Guid ClientId)
        {
            #region Authorization
            var roleResult = await _authorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to the Client Access Review page.");
                return Unauthorized();
            }
            #endregion

            var currentUser = await _userManager.GetUserAsync(User);
            var model = await _clientAccessReviewQueries.GetClientSummaryAsync(ClientId);

            return Json(model);
        }
    }
}
