/*
 * CODE OWNERS: <At least 2 names.>
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;

namespace MillimanAccessPortal.Controllers
{
    public class SystemAdminController : Controller
    {
        private readonly ApplicationDbContext DbContext;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly StandardQueries Queries;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ILogger Logger;
        private readonly IAuditLogger AuditLogger;
        private readonly RoleManager<ApplicationRole> RoleManager;

        public SystemAdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> UserManagerArg,
            StandardQueries QueryArg,
            IAuthorizationService AuthorizationServiceArg,
            ILoggerFactory LoggerFactoryArg,
            IAuditLogger AuditLoggerArg,
            RoleManager<ApplicationRole> RoleManagerArg
            )
        {
            DbContext = context;
            UserManager = UserManagerArg;
            Queries = QueryArg;
            AuthorizationService = AuthorizationServiceArg;
            Logger = LoggerFactoryArg.CreateLogger<ClientAdminController>();
            AuditLogger = AuditLoggerArg;
            RoleManager = RoleManagerArg;
        }

        // GET: SystemAdmin
        /// <summary>
        /// Action leading to the main landing page for System Administration UI
        /// </summary>
        /// <returns></returns>

        public async Task<IActionResult> Index()
        {
            #region Authorization
            // User must have a global Admin role
            AuthorizationResult Result = await AuthorizationService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.Admin));

            if (!Result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to the System Admin page.");
                return Unauthorized();
            }
            #endregion

            return View();
        }
    }
}