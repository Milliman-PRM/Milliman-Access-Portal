/*
 * CODE OWNERS: <At least 2 names.>
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.AccountViewModels;
using MillimanAccessPortal.Models.ContentPublishing;
using MillimanAccessPortal.Models.SystemAdmin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    public class SystemAdminController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StandardQueries _queries;
        private readonly IAuthorizationService _authService;
        private readonly ILogger _logger;
        private readonly IAuditLogger _auditLogger;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public SystemAdminController(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            StandardQueries queries,
            IAuthorizationService authService,
            ILoggerFactory loggerFactory,
            IAuditLogger auditLogger,
            RoleManager<ApplicationRole> roleManager
            )
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _queries = queries;
            _authService = authService;
            _logger = loggerFactory.CreateLogger<SystemAdminController>();
            _auditLogger = auditLogger;
            _roleManager = roleManager;
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
            AuthorizationResult result = await _authService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.Admin));

            if (!result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to the System Admin page.");
                return Unauthorized();
            }
            #endregion

            return View();
        }

        [HttpGet]
        public async Task<ActionResult> Users(QueryFilter filter)
        {
            #region Authorization
            // User must have a global Admin role
            AuthorizationResult result = await _authService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.Admin));

            if (!result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to the System Admin page.");
                return Unauthorized();
            }
            #endregion

            IQueryable<ApplicationUser> query = _dbContext.ApplicationUser;
            #region Filter query
            if (filter.ProfitCenterId.HasValue)
            {
                var userIds = _dbContext.UserRoleInProfitCenter
                    .Where(role => role.ProfitCenterId == filter.ProfitCenterId.Value)
                    .Where(role => role.Role.RoleEnum == RoleEnum.Admin)
                    .Select(role => role.UserId)
                    .ToList();
                query = query.Where(user => userIds.Contains(user.Id));
            }
            if (filter.ClientId.HasValue)
            {
                var userIds = _dbContext.UserClaims
                    .Where(claim => claim.ClaimType == ClaimNames.ClientMembership.ToString())
                    .Where(claim => claim.ClaimValue == filter.ClientId.ToString())
                    .Select(claim => claim.UserId)
                    .ToList();
                query = query.Where(user => userIds.Contains(user.Id));
            }
            #endregion

            var userInfoList = new List<UserInfo>();
            foreach (var user in query)
            {
                var userInfo = (UserInfo)user;
                userInfo.QueryRelatedEntityCounts(_dbContext, filter.ClientId, filter.ProfitCenterId);
                userInfoList.Add(userInfo);
            }

            return Json(userInfoList);
        }

        [HttpGet]
        public async Task<ActionResult> Clients(QueryFilter filter)
        {
            #region Authorization
            // User must have a global Admin role
            AuthorizationResult result = await _authService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.Admin));

            if (!result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to the System Admin page.");
                return Unauthorized();
            }
            #endregion

            IQueryable<Client> query = _dbContext.Client;
            #region Filter query
            if (filter.UserId.HasValue)
            {
                var clientIds = _dbContext.UserClaims
                    .Where(claim => claim.ClaimType == ClaimNames.ClientMembership.ToString())
                    .Where(claim => claim.UserId == filter.UserId.Value)
                    .Select(claim => long.Parse(claim.ClaimValue))
                    .ToList();
                query = query.Where(client => clientIds.Contains(client.Id));
            }
            if (filter.ProfitCenterId.HasValue)
            {
                query = query.Where(client => client.ProfitCenterId == filter.ProfitCenterId.Value);
            }
            #endregion

            var clientInfoList = new List<ClientInfo>();
            foreach (var client in query)
            {
                var clientInfo = (ClientInfo)client;
                clientInfo.QueryRelatedEntityCounts(_dbContext, filter.UserId, filter.ProfitCenterId);
                clientInfoList.Add(clientInfo);
            }

            return Json(clientInfoList);
        }

        [HttpGet]
        public async Task<ActionResult> ProfitCenters(QueryFilter filter)
        {
            #region Authorization
            // User must have a global Admin role
            AuthorizationResult result = await _authService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.Admin));

            if (!result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to the System Admin page.");
                return Unauthorized();
            }
            #endregion

            IQueryable<ProfitCenter> query = _dbContext.ProfitCenter;
            #region Filter query
            #endregion

            var pcInfoList = new List<ProfitCenterInfo>();
            foreach (var pc in query)
            {
                var pcInfo = (ProfitCenterInfo)pc;
                pcInfoList.Add(pcInfo);
            }

            return Json(pcInfoList);
        }

        [HttpGet]
        public async Task<ActionResult> RootContentItems(QueryFilter filter)
        {
            #region Authorization
            // User must have a global Admin role
            AuthorizationResult result = await _authService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.Admin));

            if (!result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to the System Admin page.");
                return Unauthorized();
            }
            #endregion

            IQueryable<RootContentItem> query = _dbContext.RootContentItem;
            var detailFlag = false;
            #region Filter query
            if (filter.UserId.HasValue)
            {
                var itemIds = _dbContext.UserInSelectionGroup
                    .Where(us => us.UserId == filter.UserId.Value)
                    .Select(us => us.SelectionGroup.RootContentItemId)
                    .ToList();
                query = query.Where(item => itemIds.Contains(item.Id));
            }
            if (filter.ClientId.HasValue)
            {
                detailFlag = true;
                query = query.Where(item => item.ClientId == filter.ClientId.Value);
            }
            #endregion

            var itemInfoList = new List<RootContentItemInfo>();
            foreach (var item in query)
            {
                var itemInfo = (RootContentItemInfo)item;
                if (detailFlag)
                {
                    itemInfo.IncludeUsers(_dbContext);
                }
                itemInfoList.Add(itemInfo);
            }

            return Json(itemInfoList);
        }
    }
}
