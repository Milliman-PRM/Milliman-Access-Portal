/*
 * CODE OWNERS: <At least 2 names.>
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Services;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
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
            query = query.OrderBy(user => user.LastName).ThenBy(user => user.FirstName);

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
        public async Task<ActionResult> UserDetail(QueryFilter filter)
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

            ApplicationUser user = null;
            #region Validation
            if (!filter.UserId.HasValue)
            {
                return BadRequest();
            }
            user = _dbContext.ApplicationUser.SingleOrDefault(u => u.Id == filter.UserId.Value);
            if (user == null)
            {
                Response.Headers.Add("Warning", "The specified user does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            if (filter.ClientId.HasValue)
            {
                return Json((UserDetailForClient)user);
            }
            if (filter.ProfitCenterId.HasValue)
            {
                var model = (UserDetailForProfitCenter)user;
                model.QueryRelatedEntities(_dbContext, filter.ProfitCenterId.Value);
                return Json(model);
            }
            return Json((UserDetail)user);
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
            // since the query won't be returned, order the tree instead of the query

            var clientList = query.Select(c => c.Id).ToList();

            var clientInfoList = new List<ClientInfo>();
            foreach (var client in _dbContext.Client)
            {
                var clientInfo = (ClientInfo)client;
                clientInfo.ParentOnly = !clientList.Contains(clientInfo.Id);
                clientInfoList.Add(clientInfo);
            }

            var clientTree = new BasicTree<ClientInfo>();
            clientTree.Root.Populate(ref clientInfoList);
            clientTree.Root.Prune((ClientInfo ci) => clientList.Contains(ci.Id), (cum, cur) => cum || cur, false);
            clientTree.Root.Apply((ci) =>
            {
                ci?.QueryRelatedEntityCounts(_dbContext, filter.UserId, filter.ProfitCenterId);
                return ci;
            });
            clientTree.Root.OrderInPlaceBy((node) => node.Value.Name);

            return Json(clientTree);
        }
        [HttpGet]
        public async Task<ActionResult> ClientDetail(QueryFilter filter)
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

            Client client = null;
            #region Validation
            if (!filter.ClientId.HasValue)
            {
                return BadRequest();
            }
            client = _dbContext.Client
                .Include(c => c.ProfitCenter)
                .SingleOrDefault(c => c.Id == filter.ClientId.Value);
            if (client == null)
            {
                Response.Headers.Add("Warning", "The specified client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            if (filter.UserId.HasValue)
            {
                return Json((ClientDetailForUser)client);
            }
            if (filter.ProfitCenterId.HasValue)
            {
                var model = (ClientDetailForProfitCenter)client;
                model.QueryRelatedEntities(_dbContext, filter.ProfitCenterId.Value);
                return Json(model);
            }
            return Json((ClientDetail)client);
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
            query = query.OrderBy(pc => pc.Name);

            var pcInfoList = new List<ProfitCenterInfo>();
            foreach (var pc in query)
            {
                var pcInfo = (ProfitCenterInfo)pc;
                pcInfo.QueryRelatedEntityCounts(_dbContext);
                pcInfoList.Add(pcInfo);
            }

            return Json(pcInfoList);
        }
        [HttpGet]
        public async Task<ActionResult> ProfitCenterDetail(QueryFilter filter)
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

            ProfitCenter pc = null;
            #region Validation
            if (!filter.ProfitCenterId.HasValue)
            {
                return BadRequest();
            }
            pc = _dbContext.ProfitCenter.SingleOrDefault(c => c.Id == filter.ProfitCenterId.Value);
            if (pc == null)
            {
                Response.Headers.Add("Warning", "The specified profit center does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            return Json((ProfitCenterDetail)pc);
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
                query = query.Where(item => item.ClientId == filter.ClientId.Value);
            }
            #endregion
            query = query.OrderBy(item => item.ContentName).ThenBy(item => item.ContentType);

            var itemInfoList = new List<RootContentItemInfo>();
            foreach (var item in query)
            {
                var itemInfo = (RootContentItemInfo)item;
                itemInfo.QueryRelatedEntityCounts(_dbContext, filter.UserId);
                itemInfoList.Add(itemInfo);
            }

            return Json(itemInfoList);
        }
        [HttpGet]
        public async Task<ActionResult> RootContentItemDetail(QueryFilter filter)
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

            RootContentItem item = null;
            #region Validation
            if (!filter.RootContentItemId.HasValue)
            {
                return BadRequest();
            }
            item = _dbContext.RootContentItem
                .Include(i => i.ContentType)
                .SingleOrDefault(i => i.Id == filter.RootContentItemId.Value);
            if (item == null)
            {
                Response.Headers.Add("Warning", "The specified root content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            if (filter.UserId.HasValue)
            {
                return Json((RootContentItemDetailForUser)item);
            }
            if (filter.ClientId.HasValue)
            {
                var model = (RootContentDetailForClient)item;
                model.QueryRelatedEntities(_dbContext, filter.ClientId.Value); 
                return Json(model);
            }
            return BadRequest();
        }

        [HttpGet]
        public async Task<ActionResult> SystemRole(long userId, RoleEnum role)
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

            var user = _dbContext.ApplicationUser.SingleOrDefault(u => u.Id == userId);
            #region Validation
            if (user == null)
            {
                Response.Headers.Add("Warning", "The specified user does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var systemRoles = new List<RoleEnum>
            {
                RoleEnum.Admin,
                RoleEnum.UserCreator,
            };
            if (!systemRoles.Contains(role))
            {
                Response.Headers.Add("Warning", "The specified role is not a system role.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            var roleId = (long)role;
            var roleExists = _dbContext.UserRoles.Any(ur => ur.UserId == userId && ur.RoleId == roleId);

            return Json(roleExists);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SystemRole(long userId, RoleEnum role, bool value)
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

            var user = _dbContext.ApplicationUser.SingleOrDefault(u => u.Id == userId);
            #region Validation
            if (user == null)
            {
                Response.Headers.Add("Warning", "The specified user does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var systemRoles = new List<RoleEnum>
            {
                RoleEnum.Admin,
                RoleEnum.UserCreator,
            };
            if (!systemRoles.Contains(role))
            {
                Response.Headers.Add("Warning", "The specified role is not a system role.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            var roleId = (long)role;
            var roleQuery = _dbContext.UserRoles.Where(ur => ur.UserId == userId && ur.RoleId == roleId);
            var roleExists = roleQuery.Any();

            if (roleExists == value)
            {
                // pass
            }
            else if (roleExists)
            {
                var roleToRemove = roleQuery.Single();
                _dbContext.UserRoles.Remove(roleToRemove);
                _dbContext.SaveChanges();
            }
            else
            {
                var roleToAdd = new IdentityUserRole<long>
                {
                    UserId = user.Id,
                    RoleId = roleId,
                };
                _dbContext.UserRoles.Add(roleToAdd);
                _dbContext.SaveChanges();
            }

            return Json(value);
        }

        [HttpGet]
        public async Task<ActionResult> UserSuspension(long userId)
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

            var user = _dbContext.ApplicationUser.SingleOrDefault(u => u.Id == userId);
            #region Validation
            if (user == null)
            {
                Response.Headers.Add("Warning", "The specified user does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            return Json(false);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UserSuspension(long userId, bool value)
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

            var user = _dbContext.ApplicationUser.SingleOrDefault(u => u.Id == userId);
            #region Validation
            if (user == null)
            {
                Response.Headers.Add("Warning", "The specified user does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            if (value)
            {
                Response.Headers.Add("Warning", "User suspension not implemented.");
                return StatusCode(StatusCodes.Status501NotImplemented);
            }

            return Json(false);
        }

        [HttpGet]
        public async Task<ActionResult> UserClientRoles(long userId, long clientId, RoleEnum role)
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

            var user = _dbContext.ApplicationUser.SingleOrDefault(u => u.Id == userId);
            var client = _dbContext.Client.SingleOrDefault(c => c.Id == clientId);
            #region Validation
            if (user == null)
            {
                Response.Headers.Add("Warning", "The specified user does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (client == null)
            {
                Response.Headers.Add("Warning", "The specified client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var userClientRoles = new List<RoleEnum>
            {
                RoleEnum.Admin,
                RoleEnum.ContentAccessAdmin,
                RoleEnum.ContentPublisher,
                RoleEnum.ContentUser,
            };
            if (!userClientRoles.Contains(role))
            {
                Response.Headers.Add("Warning", "The specified role is not a user-client role.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            var roleExists = _dbContext.UserRoleInClient
                .Where(ur => ur.UserId == user.Id)
                .Where(ur => ur.ClientId == client.Id)
                .Where(ur => ur.Role.RoleEnum == role)
                .Any();

            return Json(roleExists);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UserClientRoles(long userId, long clientId, RoleEnum role, bool value)
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

            var user = _dbContext.ApplicationUser.SingleOrDefault(u => u.Id == userId);
            var client = _dbContext.Client.SingleOrDefault(c => c.Id == clientId);
            #region Validation
            if (user == null)
            {
                Response.Headers.Add("Warning", "The specified user does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (client == null)
            {
                Response.Headers.Add("Warning", "The specified client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var userClientRoles = new List<RoleEnum>
            {
                RoleEnum.Admin,
                RoleEnum.ContentAccessAdmin,
                RoleEnum.ContentPublisher,
                RoleEnum.ContentUser,
            };
            if (!userClientRoles.Contains(role))
            {
                Response.Headers.Add("Warning", "The specified role is not a user-client role.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            var roleQuery = _dbContext.UserRoleInClient
                .Where(ur => ur.UserId == user.Id)
                .Where(ur => ur.ClientId == client.Id)
                .Where(ur => ur.Role.RoleEnum == role);
            var roleExists = roleQuery.Any();

            if (roleExists == value)
            {
                // pass
            }
            else if (roleExists)
            {
                var roleToRemove = roleQuery.Single();
                _dbContext.UserRoleInClient.Remove(roleToRemove);
                _dbContext.SaveChanges();
            }
            else
            {
                var roleToAdd = new UserRoleInClient
                {
                    UserId = user.Id,
                    ClientId = client.Id,
                    RoleId = (long)role,
                };
                _dbContext.UserRoleInClient.Add(roleToAdd);
                _dbContext.SaveChanges();
            }

            return Json(value);
        }

        [HttpGet]
        public async Task<ActionResult> ContentSuspension(long rootContentItemId)
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

            var rootContentItem = _dbContext.RootContentItem.SingleOrDefault(i => i.Id == rootContentItemId);
            #region Validation
            if (rootContentItem == null)
            {
                Response.Headers.Add("Warning", "The specified root content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            return Json(false);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ContentSuspension(long rootContentItemId, bool value)
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

            var rootContentItem = _dbContext.RootContentItem.SingleOrDefault(i => i.Id == rootContentItemId);
            #region Validation
            if (rootContentItem == null)
            {
                Response.Headers.Add("Warning", "The specified root content item does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            if (value)
            {
                Response.Headers.Add("Warning", "Content suspension not implemented.");
                return StatusCode(StatusCodes.Status501NotImplemented);
            }

            return Json(false);
        }
    }
}
