/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Define various MVC actions to support system administration.
 * DEVELOPER NOTES:
 *      Most of the code in this controller is conditional logic that is strongly tied to the requirements
 *      of the system admin page.
 *
 *      For each type of entity that can be used as a filter by a system admin, there are two actions:
 *          - [GET] <entities>: Query the database for all <entities>. If other entity IDs are provided in the
 *              query filter, use them to constrain the query in a custom manner based on <entity>.
 *          - [GET] <entity>Detail: Query the database for a specific <entity>, whose PK is supplied in the
 *              query filter. If other entity IDs are provided in the query filter, return a different detail model
 *              in context of the other entities in a custom manner based on <entity>.
 *      For each set of immediate toggles available to system admins, there are two actions:
 *          - [GET]: Return the value of the toggle.
 *          - [POST]: Set the value of the toggle and return this new value.
 */

using AuditLogLib.Event;
using AuditLogLib.Services;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.AccountViewModels;
using MillimanAccessPortal.Models.SystemAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    public class SystemAdminController : Controller
    {
        private readonly AccountController _accountController;
        private readonly IAuditLogger _auditLogger;
        private readonly IAuthorizationService _authService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly StandardQueries _queries;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public SystemAdminController(
            AccountController accountController,
            IAuditLogger auditLogger,
            IAuthorizationService authService,
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory,
            StandardQueries queries,
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager
            )
        {
            _accountController = accountController;
            _auditLogger = auditLogger;
            _authService = authService;
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = loggerFactory.CreateLogger<SystemAdminController>();
            _queries = queries;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        /// <summary>
        /// System Admin landing page
        /// </summary>
        /// <returns>View</returns>
        [HttpGet]
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

        #region Query actions
        /// <summary>
        /// Query for all users that match the supplied filter.
        /// </summary>
        /// <param name="filter">Entity IDs for constraining the query.</param>
        /// <returns>Json</returns>
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
                // Constrain query to users that are admins in the specified profit center
                var userIds = _dbContext.UserRoleInProfitCenter
                    .Where(role => role.ProfitCenterId == filter.ProfitCenterId.Value)
                    .Where(role => role.Role.RoleEnum == RoleEnum.Admin)
                    .Select(role => role.UserId)
                    .ToList();
                query = query.Where(user => userIds.Contains(user.Id));
            }
            if (filter.ClientId.HasValue)
            {
                // Constrain query to users that are members of the specified client
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
        /// <summary>
        /// Query for details for a specified user, optionally in context of another entity
        /// </summary>
        /// <param name="filter">Entity IDs for constraining the query.</param>
        /// <returns>Json</returns>
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
            user = _dbContext.ApplicationUser.SingleOrDefault(u => u.Id == (filter.UserId ?? Guid.Empty));
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

        /// <summary>
        /// Query for all clients that match the supplied filter.
        /// </summary>
        /// <param name="filter">Entity IDs for constraining the query.</param>
        /// <returns>Json</returns>
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
                // Constrain query to clients that have the specified user as a member
                var clientIds = _dbContext.UserClaims
                    .Where(claim => claim.ClaimType == ClaimNames.ClientMembership.ToString())
                    .Where(claim => claim.UserId == filter.UserId.Value)
                    .Select(claim => Guid.Parse(claim.ClaimValue))
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
        /// <summary>
        /// Query for details for a specified client, optionally in context of another entity
        /// </summary>
        /// <param name="filter">Entity IDs for constraining the query.</param>
        /// <returns>Json</returns>
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
            client = _dbContext.Client
                .Include(c => c.ProfitCenter)
                .SingleOrDefault(c => c.Id == (filter.ClientId ?? Guid.Empty));

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

        /// <summary>
        /// Query for all profit centers that match the supplied filter.
        /// </summary>
        /// <param name="filter">Entity IDs for constraining the query.</param>
        /// <returns>Json</returns>
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
        /// <summary>
        /// Query for details for a specified profit center, optionally in context of another entity
        /// </summary>
        /// <param name="filter">Entity IDs for constraining the query.</param>
        /// <returns>Json</returns>
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
            pc = _dbContext.ProfitCenter.SingleOrDefault(c => c.Id == (filter.ProfitCenterId ?? Guid.Empty));
            if (pc == null)
            {
                Response.Headers.Add("Warning", "The specified profit center does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            return Json((ProfitCenterDetail)pc);
        }

        /// <summary>
        /// Query for all root content items that match the supplied filter.
        /// </summary>
        /// <param name="filter">Entity IDs for constraining the query.</param>
        /// <returns>Json</returns>
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
                // Constrain query to root content items that the specified user can access
                var itemIds = _dbContext.UserInSelectionGroup
                    .Where(us => us.UserId == filter.UserId.Value)
                    .Select(us => us.SelectionGroup.RootContentItemId)
                    .ToList();
                query = query.Where(item => itemIds.Contains(item.Id));
            }
            if (filter.ClientId.HasValue)
            {
                // Constrain query to root content items that belong to the specified client
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
        /// <summary>
        /// Query for details for a specified root content item, optionally in context of another entity
        /// </summary>
        /// <param name="filter">Entity IDs for constraining the query.</param>
        /// <returns>Json</returns>
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
            item = _dbContext.RootContentItem
                .Include(i => i.ContentType)
                .SingleOrDefault(i => i.Id == (filter.RootContentItemId ?? Guid.Empty));
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
                var model = (RootContentItemDetailForClient)item;
                model.QueryRelatedEntities(_dbContext, filter.ClientId.Value); 
                return Json(model);
            }
            return BadRequest();
        }
        #endregion

        #region Create actions
        /// <summary>
        /// Create new user with no associations
        /// </summary>
        /// <param name="email">
        /// The address to which the new user email is to be sent. This value is used as
        /// new user's permanent username and initial email address.
        /// </param>
        /// <returns>Json</returns>
        [HttpPost]
        public async Task<ActionResult> CreateUser(string email)
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

            #region Validation
            if (!GlobalFunctions.IsValidEmail(email))
            {
                Response.Headers.Add("Warning", "The specified email address is invalid.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            ApplicationUser user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                Response.Headers.Add("Warning", "User already exists.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            // Creates new user with logins disabled (EmailConfirmed == false) and no password. Password is added in AccountController.EnableAccount()
            IdentityResult createResult;
            (createResult, user) = await _queries.CreateNewAccount(email, email);

            if (createResult.Succeeded && user != null)
            {
                string welcomeText = _configuration["Global:DefaultNewUserWelcomeText"];
                await _accountController.SendNewAccountWelcomeEmail(user, Url, welcomeText);
            }
            else
            {
                string errors = string.Join($", ", createResult.Errors.Select(e => e.Description));
                Response.Headers.Add("Warning", $"Error while creating user ({email}) in database: {errors}");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var userSummary = (UserInfoViewModel)user;

            return Json(userSummary);
        }

        /// <summary>
        /// Create new profit center
        /// </summary>
        /// <param name="profitCenter">The profit center to create</param>
        /// <returns>Json</returns>
        [HttpPost]
        public async Task<ActionResult> CreateProfitCenter(
            [Bind("Name", "ProfitCenterCode", "MillimanOffice", "ContactName", "ContactEmail", "ContactPhone")] ProfitCenter profitCenter)
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

            #region Validation
            if (!ModelState.IsValid)
            {
                Response.Headers.Add("Warning", ModelState.Values.First(v => v.Errors.Any()).Errors.ToString());
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            _dbContext.ProfitCenter.Add(profitCenter);
            _dbContext.SaveChanges();

            return Json(profitCenter);
        }

        /// <summary>
        /// Associate a user with a client. If the user does not exist, create one.
        /// If the user is already a member of the client, do nothing.
        /// </summary>
        /// <param name="email">Email of the user to add.</param>
        /// <param name="clientId">Client of which the user is to become a member.</param>
        /// <returns>Json</returns>
        [HttpPost]
        public async Task<ActionResult> AddUserToClient(string email, long clientId)
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

            #region Validation
            if (!GlobalFunctions.IsValidEmail(email))
            {
                Response.Headers.Add("Warning", "The specified email address is invalid.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (_dbContext.Client.Find(clientId) == null)
            {
                Response.Headers.Add("Warning", "Client does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            // Find the user or create one if it doesn't exist
            ApplicationUser user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Creates new user with logins disabled (EmailConfirmed == false) and no password. Password is added in AccountController.EnableAccount()
                IdentityResult createResult;
                (createResult, user) = await _queries.CreateNewAccount(email, email);

                if (createResult.Succeeded && user != null)
                {
                    string welcomeText = _configuration["Global:DefaultNewUserWelcomeText"];
                    await _accountController.SendNewAccountWelcomeEmail(user, Url, welcomeText);
                }
                else
                {
                    string errors = string.Join($", ", createResult.Errors.Select(e => e.Description));
                    Response.Headers.Add("Warning", $"Error while creating user ({email}) in database: {errors}");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }

            // Add client membership claim for the user if it doesn't already exist
            var clientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), clientId.ToString());
            var existingClaimsForUser = await _userManager.GetClaimsAsync(user);
            var matchingClaims = existingClaimsForUser
                .Where(claim => claim.Type == clientMembershipClaim.Type)
                .Where(claim => claim.Value == clientMembershipClaim.Value);
            if (!matchingClaims.Any())
            {
                await _userManager.AddClaimAsync(user, clientMembershipClaim);
            }

            return Json(user);
        }

        /// <summary>
        /// Make a user a profit center admin. If the user does not exist, create one.
        /// If the user is already an admin on the profit center, do nothing.
        /// </summary>
        /// <param name="email">Email of the user to add.</param>
        /// <param name="profitCenterId">Profit center to which the user is to become an admin.</param>
        /// <returns>Json</returns>
        [HttpPost]
        public async Task<ActionResult> AddUserToProfitCenter(string email, Guid profitCenterId)
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

            #region Validation
            if (!GlobalFunctions.IsValidEmail(email))
            {
                Response.Headers.Add("Warning", "The specified email address is invalid.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            if (_dbContext.ProfitCenter.Find(profitCenterId) == null)
            {
                Response.Headers.Add("Warning", "Profit center does not exist.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            // Find the user or create one if it doesn't exist
            ApplicationUser user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Creates new user with logins disabled (EmailConfirmed == false) and no password. Password is added in AccountController.EnableAccount()
                IdentityResult createResult;
                (createResult, user) = await _queries.CreateNewAccount(email, email);

                if (createResult.Succeeded && user != null)
                {
                    string welcomeText = _configuration["Global:DefaultNewUserWelcomeText"];
                    await _accountController.SendNewAccountWelcomeEmail(user, Url, welcomeText);
                }
                else
                {
                    string errors = string.Join($", ", createResult.Errors.Select(e => e.Description));
                    Response.Headers.Add("Warning", $"Error while creating user ({email}) in database: {errors}");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }

            var alreadyAdmin = _dbContext.UserRoleInProfitCenter
                .Where(r => r.User.Email == email)
                .Where(r => r.ProfitCenterId == profitCenterId)
                .Where(r => r.Role.RoleEnum == RoleEnum.Admin)
                .Any();

            if (!alreadyAdmin)
            {
                _dbContext.UserRoleInProfitCenter.Add(new UserRoleInProfitCenter
                {
                    ProfitCenterId = profitCenterId,
                    RoleId = ApplicationRole.RoleIds[RoleEnum.Admin],
                    UserId = user.Id,
                });
                _dbContext.SaveChanges();
            }

            return Json(user);
        }
        #endregion

        #region Immediate toggle actions
        /// <summary>
        /// Get whether a user has a particular system role or not.
        /// </summary>
        /// <param name="userId">User whose role is to be checked</param>
        /// <param name="role">Role to check</param>
        /// <returns>true if the user has the role; false otherwise</returns>
        [HttpGet]
        public async Task<ActionResult> SystemRole(Guid userId, RoleEnum role)
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
            };
            if (!systemRoles.Contains(role))
            {
                Response.Headers.Add("Warning", "The specified role is not a system role.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            var roleId = ApplicationRole.RoleIds[role];
            var roleExists = _dbContext.UserRoles.Any(ur => ur.UserId == userId && ur.RoleId == roleId);

            return Json(roleExists);
        }
        /// <summary>
        /// Set a specific system role for a user.
        /// </summary>
        /// <param name="userId">User whose role is to be set</param>
        /// <param name="role">Role to set</param>
        /// <param name="value">true to set the role; false to unset the role</param>
        /// <returns>true if the user has the role; false otherwise</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SystemRole(Guid userId, RoleEnum role, bool value)
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
            };
            if (!systemRoles.Contains(role))
            {
                Response.Headers.Add("Warning", "The specified role is not a system role.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            var currentUser = await _queries.GetCurrentApplicationUser(User);
            if (user.Id == currentUser.Id && role == RoleEnum.Admin && !value)
            {
                Response.Headers.Add("Warning", "You cannot unset your own account as system admin.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            var roleId = ApplicationRole.RoleIds[role];
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

                _auditLogger.Log(AuditEventType.SystemRoleRemoved.ToEvent(user, role));
            }
            else
            {
                var roleToAdd = new IdentityUserRole<Guid>
                {
                    UserId = user.Id,
                    RoleId = roleId,
                };
                _dbContext.UserRoles.Add(roleToAdd);
                _dbContext.SaveChanges();

                _auditLogger.Log(AuditEventType.SystemRoleAssigned.ToEvent(user, role));
            }

            return Json(value);
        }

        /// <summary>
        /// Get whether a user is suspended or not.
        /// </summary>
        /// <param name="userId">User whose suspension status is to be checked.</param>
        /// <returns>true if the user is suspended; false otherwise</returns>
        [HttpGet]
        public async Task<ActionResult> UserSuspension(Guid userId)
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

            return Json(user.IsSuspended);
        }
        /// <summary>
        /// Set suspension status for a user.
        /// </summary>
        /// <param name="userId">User whose suspension status is to be set.</param>
        /// <param name="value">true to suspend the user; false to unsuspend the user</param>
        /// <returns>true if the user is suspended; false otherwise</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UserSuspension(Guid userId, bool value)
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

            var currentUser = await _queries.GetCurrentApplicationUser(User);
            if (user.Id == currentUser.Id && value)
            {
                Response.Headers.Add("Warning", "You cannot suspend your own account.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            user.IsSuspended = value;
            _dbContext.ApplicationUser.Update(user);
            _dbContext.SaveChanges();

            _auditLogger.Log(AuditEventType.UserSuspensionUpdate.ToEvent(user, value, ""));

            return Json(user.IsSuspended);
        }
        
        /// <summary>
        /// Get whether a user has a particular client role or not.
        /// </summary>
        /// <param name="userId">User whose role is to be checked</param>
        /// <param name="clientId">Client whose role is to be checked</param>
        /// <param name="role">Role to check</param>
        /// <returns>true if the user has the role in the client; false otherwise</returns>
        [HttpGet]
        public async Task<ActionResult> UserClientRoles(Guid userId, Guid clientId, RoleEnum role)
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

            var userClientAssignments = new Dictionary<RoleEnum, List<RoleEnum>>
            {
                {
                    RoleEnum.Admin, new List<RoleEnum>
                    {
                        RoleEnum.Admin,
                        RoleEnum.UserCreator,
                    }
                },
                { RoleEnum.ContentAccessAdmin, new List<RoleEnum> { RoleEnum.ContentAccessAdmin, } },
                { RoleEnum.ContentPublisher, new List<RoleEnum> { RoleEnum.ContentPublisher, } },
                { RoleEnum.ContentUser, new List<RoleEnum> { RoleEnum.ContentUser, } },
            };
            if (!userClientAssignments.Keys.Contains(role))
            {
                Response.Headers.Add("Warning", "The specified role is not a user-client role.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            var roleQuery = _dbContext.UserRoleInClient
                .Where(ur => ur.UserId == user.Id)
                .Where(ur => ur.ClientId == client.Id);
            var roleExists = userClientAssignments[role]
                .All(a => roleQuery
                    .Any(ur => ur.Role.RoleEnum == a));

            return Json(roleExists);
        }
        /// <summary>
        /// Set a specific client role for a user.
        /// </summary>
        /// <param name="userId">User whose role is to be set</param>
        /// <param name="clientId">Client whose role is to be set</param>
        /// <param name="role">Role to set</param>
        /// <param name="value">true to set the role; false to unset the role</param>
        /// <returns>true if the user has the role in the client; false otherwise</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UserClientRoles(Guid userId, Guid clientId, RoleEnum role, bool value)
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

            // For every valid role, associate a list of roles that will actually be assigned.
            // This supports the existence of "hidden roles" that are always assigned with visible roles.
            var userClientAssignments = new Dictionary<RoleEnum, List<RoleEnum>>
            {
                {
                    RoleEnum.Admin, new List<RoleEnum>
                    {
                        RoleEnum.Admin,
                        RoleEnum.UserCreator,
                    }
                },
                { RoleEnum.ContentAccessAdmin, new List<RoleEnum> { RoleEnum.ContentAccessAdmin, } },
                { RoleEnum.ContentPublisher, new List<RoleEnum> { RoleEnum.ContentPublisher, } },
                { RoleEnum.ContentUser, new List<RoleEnum> { RoleEnum.ContentUser, } },
            };
            if (!userClientAssignments.Keys.Contains(role))
            {
                Response.Headers.Add("Warning", "The specified role is not a user-client role.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            var roleQuery = _dbContext.UserRoleInClient
                .Where(ur => ur.UserId == user.Id)
                .Where(ur => ur.ClientId == client.Id);

            if (!value)
            {
                var rolesToRemove = roleQuery
                    .Where(ur => userClientAssignments[role].Contains(ur.Role.RoleEnum))
                    .ToList();

                foreach (var roleToRemove in rolesToRemove)
                {
                    _dbContext.UserRoleInClient.Remove(roleToRemove);
                }
                _dbContext.SaveChanges();

                _auditLogger.Log(AuditEventType.ClientRoleRemoved.ToEvent(client, user, userClientAssignments[role]));
            }
            else
            {
                // Don't reassign any roles that are already assigned
                var rolesToAdd = userClientAssignments[role]
                    .Except(roleQuery.Select(ur => ur.Role.RoleEnum));

                foreach (var roleToAdd in rolesToAdd)
                {
                    var userRole = new UserRoleInClient
                    {
                        UserId = user.Id,
                        ClientId = client.Id,
                        RoleId = ApplicationRole.RoleIds[roleToAdd],
                    };
                    _dbContext.UserRoleInClient.Add(userRole);
                }
                _dbContext.SaveChanges();

                _auditLogger.Log(AuditEventType.ClientRoleAssigned.ToEvent(client, user, userClientAssignments[role]));
            }

            return Json(value);
        }

        /// <summary>
        /// Get whether a root content item is suspended or not.
        /// </summary>
        /// <param name="rootContentItemId">Root content item whose suspension status is to be checked.</param>
        /// <returns>true is the root content item is suspended; false otherwise</returns>
        [HttpGet]
        public async Task<ActionResult> ContentSuspension(Guid rootContentItemId)
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

            return Json(rootContentItem.IsSuspended);
        }
        /// <summary>
        /// Set suspension status for a root content item.
        /// </summary>
        /// <param name="rootContentItemId">Root content item whose suspension status is to be set.</param>
        /// <param name="value">true to suspend the root content item; false to unsuspend the root content item</param>
        /// <returns>true is the root content item is suspended; false otherwise</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ContentSuspension(Guid rootContentItemId, bool value)
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

            rootContentItem.IsSuspended = value;
            _dbContext.RootContentItem.Update(rootContentItem);
            _dbContext.SaveChanges();

            _auditLogger.Log(AuditEventType.RootContentItemSuspensionUpdate.ToEvent(rootContentItem, value, ""));

            return Json(rootContentItem.IsSuspended);
        }
        #endregion
    }
}
