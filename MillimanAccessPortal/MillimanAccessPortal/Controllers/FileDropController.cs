using AuditLogLib.Event;
using AuditLogLib.Models;
using AuditLogLib.Services;
using MapCommonLib;
using MapCommonLib.ActionFilters;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.Binders;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ContentPublishing;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.Utilities;
using Newtonsoft.Json.Linq;
using PowerBiLib;
using QlikviewLib;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    public class FileDropController : Controller
    {
        private const RoleEnum requiredRole = RoleEnum.FileDropAdmin;

        private readonly IAuditLogger AuditLogger;
        private readonly IConfiguration ApplicationConfig;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ApplicationDbContext _dbContext;
        private readonly StandardQueries _standardQueries;
        private readonly FileDropQueries _fileDropQueries;
        private readonly FileSystemTasks _fileSystemTasks;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Constructor, stores local references to injected service instances
        /// </summary>
        /// <param name="AuditLoggerArg"></param>
        /// <param name="AuthorizationServiceArg"></param>
        /// <param name="ContextArg"></param>
        /// <param name="fileSystemTasks"></param>
        /// <param name="UserManagerArg"></param>
        /// <param name="ApplicationConfigArg"></param>
        public FileDropController(
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ApplicationDbContext ContextArg,
            StandardQueries QueriesArg,
            FileDropQueries fileDropQueriesArg,
            FileSystemTasks fileSystemTasks,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration ApplicationConfigArg
            )
        {
            AuditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            _dbContext = ContextArg;
            _standardQueries = QueriesArg;
            _fileDropQueries = fileDropQueriesArg;
            _fileSystemTasks = fileSystemTasks;
            _userManager = UserManagerArg;
            ApplicationConfig = ApplicationConfigArg;
        }

        /// <summary>
        /// View page in which publication UI is presented
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// GET clients authorized to the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            #region Authorization
            var roleResult = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(requiredRole));
            if (!roleResult.Succeeded)
            {
                Log.Debug($"Failed to authorize action {ControllerContext.ActionDescriptor.DisplayName} for user {User.Identity.Name}");
                Response.Headers.Add("Warning", "You are not authorized to administer File Drop access.");
                return Unauthorized();
            }
            #endregion

            var currentUser = await _standardQueries.GetCurrentApplicationUser(User);
            var clients = _fileDropQueries.GetAuthorizedClientsModel(currentUser);

            return Json(clients);
        }

    }
}
