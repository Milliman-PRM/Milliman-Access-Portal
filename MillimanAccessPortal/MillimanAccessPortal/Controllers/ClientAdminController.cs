/*
 * CODE OWNERS: Tom Puckett, Evan Klein
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
using MillimanAccessPortal.Models.SystemAdmin;
using MillimanAccessPortal.Models.EntityModels.ClientModels;

namespace MillimanAccessPortal.Controllers
{
    public class ClientAdminController : Controller
    {
        private readonly static List<RoleEnum> RolesToManage = new List<RoleEnum>
        {
            RoleEnum.Admin,
            RoleEnum.ContentPublisher,
            RoleEnum.ContentUser,
            RoleEnum.ContentAccessAdmin,
            RoleEnum.FileDropAdmin,
            RoleEnum.FileDropUser
        };

        private readonly ApplicationDbContext DbContext;
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly IMessageQueue MessageQueueService;
        private readonly RoleManager<ApplicationRole> RoleManager;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration ApplicationConfig;
        private readonly AccountController _accountController;
        private readonly ClientAdminQueries _clientAdminQueries;

        public ClientAdminController(
            ApplicationDbContext context,
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            IMessageQueue MessageQueueServiceArg,
            RoleManager<ApplicationRole> RoleManagerArg,
            StandardQueries QueryArg,
            UserManager<ApplicationUser> UserManagerArg,
            IConfiguration ApplicationConfigArg,
            AccountController AccountControllerArg,
            ClientAdminQueries ClientAdminQueriesArg
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
            _clientAdminQueries = ClientAdminQueriesArg;
        }

        // GET: ClientAdmin
        /// <summary>
        /// Action leading to the main landing page for Client administration UI
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            #region Authorization
            // User must have Admin role to at least 1 Client OR to at least 1 ProfitCenter
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            AuthorizationResult Result2 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInProfitCenterRequirement(RoleEnum.Admin));
            if (!Result1.Succeeded && !Result2.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to the Client Admin page.");
                return Unauthorized();
            }
            #endregion

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            #region Authorization
            // User must have Admin role to at least 1 Client or ProfitCenter
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            AuthorizationResult Result2 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInProfitCenterRequirement(RoleEnum.Admin));
            if (!Result1.Succeeded &&
                !Result2.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized as a client admin or profit center admin");
                return Unauthorized();
            }
            #endregion

            var currentUser = await _userManager.GetUserAsync(User);
            var clients = await _clientAdminQueries.GetAuthorizedClientsModelAsync(currentUser);

            return Json(clients);
        }

        // GET: ClientAdmin/AuthorizedProfitCenters
        /// <summary>
        /// Returns the list of profit centers that the user is authorized to administer.
        /// </summary>
        /// <param name="userId">Guid of user whose profit centers we want to return.</param>
        /// <returns>JsonResult of List<AuthorizedProfitCenterModel></returns>
        [HttpGet]
        public async Task<IActionResult> AuthorizedProfitCenters(Guid userId)
        {
            #region Authorization
            // User must have Admin role to at least 1 Client or ProfitCenter
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            AuthorizationResult Result2 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInProfitCenterRequirement(RoleEnum.Admin));
            if (!Result1.Succeeded &&
                !Result2.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized as a client admin or profit center admin");
                return Unauthorized();
            }
            #endregion
            
            var currentUser = await _userManager.GetUserAsync(User);
            var AuthorizedProfitCenterList = await _clientAdminQueries.GetAuthorizedProfitCentersListAsync(currentUser);
            return Json(AuthorizedProfitCenterList);
        }

        // GET: ClientAdmin/ClientFamilyList
        /// <summary>
        /// Returns the list of Client families that the current user has visibility to (defined by GetClientAdminIndexModelForUser(...)
        /// </summary>
        /// <returns>JsonResult or UnauthorizedResult</returns>
        [HttpGet]
        public async Task<IActionResult> ClientFamilyList()
        {
            #region Authorization
            // User must have Admin role to at least 1 Client or ProfitCenter
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin));
            AuthorizationResult Result2 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInProfitCenterRequirement(RoleEnum.Admin));
            if (!Result1.Succeeded &&
                !Result2.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized as a client admin or profit center admin");
                return Unauthorized();
            }
            #endregion

            ClientAdminIndexViewModel ModelToReturn = await ClientAdminIndexViewModel.GetClientAdminIndexModelForUser(await _userManager.GetUserAsync(User), _userManager, DbContext, ApplicationConfig["Global:DefaultNewUserWelcomeText"]);

            return Json(ModelToReturn);
        }

        // GET: ClientAdmin/ClientDetail
        /// <summary>
        /// Returns details for the requested Client.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns>JsonResult or UnauthorizedResult</returns>
        [HttpGet]
        public async Task<IActionResult> ClientDetail(Guid clientId)
        {
            Log.Verbose($"In ClientAdminController.ClientDetail for clientId {clientId}");

            Client ThisClient = await DbContext.Client
                                               .Include(c => c.ProfitCenter)
                                               .FirstOrDefaultAsync(c => c.Id == clientId);

            #region Validation
            if (ThisClient == null)
            {
                Response.Headers.Add("Warning", $"The requested client was not found");
                return NotFound();
            }
            #endregion

            #region Authorization
            // Check current user's authorization to manage the requested Client
            List<Guid> AllRelatedClientsList = (await Queries.GetAllRelatedClientsAsync(ThisClient)).Select(c => c.Id).ToList();

            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInAnySuppliedClientRequirement(RoleEnum.Admin, AllRelatedClientsList));
            if (!Result1.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to administer the requested client");
                return Unauthorized();
            }
            #endregion

            ClientDetailViewModel Model = new ClientDetailViewModel { ClientEntity = ThisClient };
            await Model.GenerateSupportingProperties(DbContext, _userManager, await _userManager.GetUserAsync(User), RoleEnum.Admin, false, ApplicationConfig.GetValue("DisableInactiveUserMonths", 12), ApplicationConfig.GetValue("DisableInactiveUserWarningDays", 14));

            return Json(Model);
        }

        // POST: ClientAdmin/SaveNewUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveNewUser([FromBody]ApplicationUserViewModel Model)
        {
            Log.Verbose($"In ClientAdminController.SaveNewUser action request to add user {(!string.IsNullOrWhiteSpace(Model.Email) ? Model.Email : Model.UserName) ?? "<Unspecified>"} to clientId {Model.MemberOfClientId}");

            ApplicationUser RequestedUser = null;

            #region If user already exists get the record
            if (!string.IsNullOrWhiteSpace(Model.Email))
            {
                RequestedUser = await _userManager.FindByEmailAsync(Model.Email);
            }
            if (RequestedUser == null && !string.IsNullOrWhiteSpace(Model.UserName))
            {
                RequestedUser = await _userManager.FindByNameAsync(Model.UserName);
            }
            #endregion

            bool RequestedUserIsNew = (RequestedUser == null);

            Client RequestedClient = await DbContext.Client.SingleOrDefaultAsync(c => c.Id == Model.MemberOfClientId);
            if (RequestedClient == null)
            {
                Response.Headers.Add("Warning", "The requested Client does not exist");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            #region Authorization
            // If creating a new user, current user must either have global UserCreator role or UserCreator role for requested client
            if (RequestedUserIsNew)
            {
                if (Model.MemberOfClientId == null)
                {
                    AuthorizationResult GlobalUserCreatorResult = await AuthorizationService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.UserCreator));
                    if (!GlobalUserCreatorResult.Succeeded)
                    {
                        Log.Verbose($"In ClientAdminController.SaveNewUser action: authorization failed for user {User.Identity.Name}, for global UserCreator role");
                        // also logged:
                        //   - requested user
                        //   - (there is no requested client)
                        AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.UserCreator));

                        Response.Headers.Add("Warning", "You are not authorized to create a user");
                        return Unauthorized();
                    }
                }
                else
                {
                    AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.UserCreator, Model.MemberOfClientId));
                    if (!Result1.Succeeded)
                    {
                        Log.Verbose($"In ClientAdminController.SaveNewUser action: authorization failed for user {User.Identity.Name}, for client UserCreator role");
                        // also logged:
                        //   - requested user
                        //   - requested client ID
                        AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.UserCreator));

                        Response.Headers.Add("Warning", "You are not authorized to create a user for the requested client");
                        return Unauthorized();
                    }
                }
            }

            // If a client assignment is requested, user must be admin for the requested client
            if (Model.MemberOfClientId != null)
            {
                AuthorizationResult Result2 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, Model.MemberOfClientId));
                if (!Result2.Succeeded)
                {
                    Log.Verbose($"In ClientAdminController.SaveNewUser action: authorization failed for user {User.Identity.Name}, for client Admin role");
                    // also logged:
                    //   - requested user
                    //   - requested clients
                    //   - client ID
                    AuditLogger.Log(AuditEventType.Unauthorized.ToEvent(RoleEnum.UserCreator));

                    Response.Headers.Add("Warning", $"You are not authorized to assign a user to the requested client");
                    return Unauthorized();
                }
            }
            #endregion Authorization

            #region Validation
            if (RequestedUserIsNew)
            {
                // 1. Email must be a valid address
                if (!GlobalFunctions.IsValidEmail(Model.Email))
                {
                    Log.Verbose($"In ClientAdminController.SaveNewUser action: Validation failed, requested new user email is invalid: {Model.Email}");
                    Response.Headers.Add("MapReason", "101");
                    Response.Headers.Add("Warning", $"The provided email address ({Model.Email}) is not valid");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }

                // 2. Make sure the UserName does not exist in the database already as a UserName or Email
                if (RequestedUserIsNew &&
                    DbContext.ApplicationUser.Any(u => u.UserName == Model.UserName || 
                                                       u.Email == Model.UserName))
                {
                    Log.Verbose($"In ClientAdminController.SaveNewUser action: Validation failed, requested new user email {Model.Email} already exists in database as email or username");
                    Response.Headers.Add("MapReason", "103");
                    Response.Headers.Add("Warning", $"The provided user name ({Model.UserName}) already exists in the system");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }
            else
            {
                // For the scenario where only username was provided in the model, need to record email of an existing user account for user below
                Model.Email = RequestedUser.Email;
            }

            // 3. The user's email must match address or domain requirement of the client
            if (!GlobalFunctions.DoesEmailSatisfyClientWhitelists(Model.Email, RequestedClient.AcceptedEmailDomainList, RequestedClient.AcceptedEmailAddressExceptionList))
            {
                Log.Verbose($"In ClientAdminController.SaveNewUser action: Validation failed, requested new user email {Model.Email} not permitted for this client");
                Response.Headers.Add("Warning", $"The requested user email ({Model.Email}) is not permitted for the requested client.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion Validation

            foreach (var role in RolesToManage)
            {
                if (!Model.RoleAssignments.Any(ra => ra.RoleEnum == role))
                {
                    Model.RoleAssignments.Add(new ClientRoleAssignment { RoleEnum = role, IsAssigned = false });
                }
            }

            try
            {
                // Create requested user if not already existing
                if (RequestedUserIsNew)
                {
                    IdentityResult result;
                    // Creates new user with logins disabled (EmailConfirmed == false) and no password. Password is added in AccountController.EnableAccount()
                    (result, RequestedUser) = await Queries.CreateNewAccountAsync(Model.UserName, Model.Email);

                    if (result.Succeeded && RequestedUser != null)
                    {
                        Log.Verbose($"In ClientAdminController.SaveNewUser action: New user account created: UserName {Model.UserName}, email {Model.Email}");
                        // Configurable portion of email body
                        string welcomeText = !string.IsNullOrWhiteSpace(RequestedClient.NewUserWelcomeText)
                            ? RequestedClient.NewUserWelcomeText
                            : ApplicationConfig["Global:DefaultNewUserWelcomeText"];  // could be null, that's ok

                        await _accountController.SendNewAccountWelcomeEmail(RequestedUser, Request.Scheme, Request.Host, welcomeText);
                        Log.Verbose($"In ClientAdminController.SaveNewUser action: For new user UserName {Model.UserName}, welcome email sent");
                    }
                    else
                    {
                        string Errors = string.Join($", ", result.Errors.Select(e => e.Description));
                        Log.Verbose($"In ClientAdminController.SaveNewUser action: New user account creation failed: UserName {Model.UserName}, error(s) {Errors}");
                        Response.Headers.Add("Warning", $"Error while creating user ({Model.UserName}) in database: {Errors}");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                }

                Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), Model.MemberOfClientId.ToString());
                var CurrentClaimsOfRequestedUser = await _userManager.GetClaimsAsync(RequestedUser);
                if (!CurrentClaimsOfRequestedUser.Any(c => c.Type == ThisClientMembershipClaim.Type && c.Value == ThisClientMembershipClaim.Value))
                {
                    await _userManager.AddClaimAsync(RequestedUser, ThisClientMembershipClaim);
                    Log.Verbose($"In ClientAdminController.SaveNewUser action: UserName {RequestedUser.UserName}, added to client {ThisClientMembershipClaim.Value}");
                } else {
                  Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName}: UserName {RequestedUser.UserName} already exists on client {ThisClientMembershipClaim.Value}");
                  Response.Headers.Add("Warning", $"User already exists for this Client.");
                  return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
                await DbContext.SaveChangesAsync();
                AuditLogger.Log(AuditEventType.UserAssignedToClient.ToEvent(RequestedClient, RequestedUser, Model.Reason));

                await UpdateAllUserRolesInClient(new UpdateAllUserRolesInClientRequestModel { ClientId = Model.MemberOfClientId, 
                                                                                              UserId = RequestedUser.Id, 
                                                                                              RoleAssignments = Model.RoleAssignments, 
                                                                                              Reason = Model.Reason });
            }
            catch (Exception e)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(e, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Exception while creating new user \"{Model.UserName}\" or assigning user membership in client(s): [{Model.MemberOfClientId.ToString()}]");
                Log.Error(e, $"In ClientAdminController.SaveNewUser action: {ErrMsg}");
                Response.Headers.Add("Warning", "Failed to complete operation");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            SaveNewClientUserResponseModel model = new SaveNewClientUserResponseModel()
            {
              UserInfo = (UserInfoViewModel) RequestedUser,
              UserRoles = Model.RoleAssignments.ToDictionary(ra => (int) ra.RoleEnum),
            };

            return Json(model);
        }

        /// <summary>
        /// Assigns a requested, existing ApplicationUser to a requested Client.  Requires POST.
        /// </summary>
        /// <param name="Model">type ClientUserAssociationViewModel</param>
        /// <returns>BadRequestObjectResult, UnauthorizedResult, or OkResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUserToClient(ClientUserAssociationViewModel Model)
        {
            Log.Verbose("In ClientAdminController.AssignUserToClient action for model {@ClientUserAssociationViewModel}", Model);

            Client RequestedClient = await DbContext.Client.FindAsync(Model.ClientId);

            #region Preliminary validation - Requested client must exist
            if (RequestedClient == null)
            {
                Log.Debug($"In ClientAdminController.AssignUserToClient action: client not found with requested id {Model.ClientId}");
                return BadRequest("The requested client does not exist");
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new MapAuthorizationRequirementBase[]
                {
                    new RoleInClientRequirement(RoleEnum.Admin, Model.ClientId),
                    new RoleInProfitCenterRequirement(RoleEnum.Admin, RequestedClient.ProfitCenterId),
                });
            if (!Result1.Succeeded)
            {
                Log.Debug($"In ClientAdminController.AssignUserToClient action: authorization check failed, user {Model.UserId}, client {Model.ClientId}");
                return Unauthorized();
            }
            #endregion

            #region Validate the request
            // 1. Requested user must exist
            ApplicationUser RequestedUser = await DbContext.ApplicationUser
                                                           .Where(u => u.Id == Model.UserId)
                                                           .SingleOrDefaultAsync();
            if (RequestedUser == null)
            {
                Log.Debug($"In ClientAdminController.AssignUserToClient action: requested user {Model.UserId} not found");
                return BadRequest("The requested user does not exist");
            }

            // 2. Requested User's email must comply with client email whitelist
            string RequestedUserEmail = RequestedUser.Email.ToUpper();
            if (!GlobalFunctions.IsValidEmail(RequestedUserEmail))
            {
                Log.Debug($"In ClientAdminController.AssignUserToClient action: requested user email address {RequestedUserEmail} not valid");
                Response.Headers.Add("Warning", $"The requested user's email is invalid: ({RequestedUserEmail})");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);  // 412 is Precondition Failed
            }
            string RequestedUserEmailDomain = RequestedUserEmail.Substring(RequestedUserEmail.IndexOf('@') + 1);
            bool DomainMatch = RequestedClient.AcceptedEmailDomainList != null && 
                               RequestedClient.AcceptedEmailDomainList.Select(d=>d.ToUpper()).Contains(RequestedUserEmailDomain);
            bool EmailMatch = RequestedClient.AcceptedEmailAddressExceptionList != null && 
                              RequestedClient.AcceptedEmailAddressExceptionList.Select(d => d.ToUpper()).Contains(RequestedUserEmail);
            if (!EmailMatch && !DomainMatch)
            {
                Log.Debug($"In ClientAdminController.AssignUserToClient action: requested user email address {RequestedUserEmail} not permitted for client {RequestedClient.Id}");
                Response.Headers.Add("Warning", "The requested user's email is not accepted for this client");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), RequestedClient.Id.ToString());

            IList<Claim> CurrentUserClaims = await _userManager.GetClaimsAsync(RequestedUser);
            if (CurrentUserClaims.Any(claim => claim.Type == ThisClientMembershipClaim.Type && 
                                               claim.Value == ThisClientMembershipClaim.Value))
            {
                Log.Verbose($"In ClientAdminController.AssignUserToClient action: requested user {RequestedUserEmail} is already a member of client {RequestedClient.Id}, nothing to do");
                Response.Headers.Add("Warning", "The requested user is already assigned to the requested client");
            }
            else
            {
                IdentityResult ResultOfAddClaim = await _userManager.AddClaimAsync(RequestedUser, ThisClientMembershipClaim);
                if (ResultOfAddClaim != IdentityResult.Success)
                {
                    string ErrMsg = $"Failed to add client claim for user {RequestedUser.UserName}: claim type = {ThisClientMembershipClaim.Type}, claim value = {ThisClientMembershipClaim.Value}";
                    Log.Error($"In ClientAdminController.AssignUserToClient action: {ErrMsg}");
                    return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
                }

                AuditLogger.Log(AuditEventType.UserAssignedToClient.ToEvent(RequestedClient, RequestedUser, Model.Reason));
            }

            ClientDetailViewModel ReturnModel = new ClientDetailViewModel { ClientEntity = RequestedClient };
            await ReturnModel.GenerateSupportingProperties(DbContext, _userManager, await _userManager.GetUserAsync(User), RoleEnum.Admin, false);

            Log.Verbose($"In ClientAdminController.AssignUserToClient action: user {RequestedUserEmail} added client {RequestedClient.Id}");
            return Json(ReturnModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAllUserRolesInClient([FromBody] UpdateAllUserRolesInClientRequestModel model)
        {
            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action for model {{@UpdateAllUserRolesInClientRequestModel}}", model);

            #region Authorization
            if (!(await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, model.ClientId))).Succeeded)
            {
                Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: authorization failed for user {User.Identity.Name}, role Admin, client {model.ClientId}");
                Response.Headers.Add("Warning", $"You are not authorized to manage this client");
                return Unauthorized();
            }
            #endregion

            ApplicationUser RequestedUser = await _userManager.FindByIdAsync(model.UserId.ToString());
            Client RequestedClient = await DbContext.Client.FindAsync(model.ClientId);

            #region Validation
            // requested user must exist
            if (RequestedUser == null)
            {
                Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: requested user ID {model.UserId} not found");
                Response.Headers.Add("Warning", $"The requested user was not found");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // requested client must exist
            if (RequestedClient == null)
            {
                Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: requested client ID {model.ClientId} not found");
                Response.Headers.Add("Warning", $"The requested client was not found");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Requested user must be member of requested client
            Claim ClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), model.ClientId.ToString());
            if (!(await _userManager.GetUsersForClaimAsync(ClientMembershipClaim)).Contains(RequestedUser))
            {
                Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: requested user ID {model.UserId} not a member of client ID {model.ClientId}");
                Response.Headers.Add("Warning", $"The requested user is not associated with the requested client");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Don't remove the last client admin
            if (!model.RoleAssignments.Single(r => r.RoleEnum == RoleEnum.Admin).IsAssigned)
            {
                bool OtherAdminExists = await DbContext.UserRoleInClient.Where(r => r.ClientId == model.ClientId)
                                                                        .Where(r => r.Role.RoleEnum == RoleEnum.Admin)
                                                                        .AnyAsync(r => r.UserId != model.UserId);
                if (!OtherAdminExists)
                {
                    Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: unable to remove requested user {model.UserId} from client {model.ClientId}.  User is the sole client administrator");
                    Response.Headers.Add("Warning", "Cannot remove the last client admin user role from the client");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }
            #endregion

            List<UserRoleInClient> existingAssignedRoles = await DbContext.UserRoleInClient
                                                                          .Include(urc => urc.Role)
                                                                          .Where(urc => urc.ClientId == model.ClientId)
                                                                          .Where(urc => urc.UserId == model.UserId)
                                                                          .ToListAsync();

            foreach (ClientRoleAssignment roleAssignment in model.RoleAssignments)
            {
                // remove existing roles no longer wanted
                List<UserRoleInClient> rolesToRemove = existingAssignedRoles.Where(r => r.Role.RoleEnum == roleAssignment.RoleEnum && !roleAssignment.IsAssigned).ToList();

                // If the current role is Admin, also remove the user's UserCreator role if it exists.
                if (rolesToRemove.Any(r => r.Role.RoleEnum == RoleEnum.Admin)) {
                    rolesToRemove.AddRange(existingAssignedRoles.Where(urc => urc.Role.RoleEnum == RoleEnum.UserCreator));
                }

                foreach (var urc in rolesToRemove)
                {
                    DbContext.UserRoleInClient.Remove(urc);
                    Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: Role {urc.Role.Name} removed for username {RequestedUser.UserName} to client {RequestedClient.Id}");

                    switch (urc.Role.RoleEnum) 
                    {
                        case RoleEnum.FileDropUser:
                            var accountsToReset = await DbContext.SftpAccount
                                                                 .Include(a => a.FileDrop)
                                                                 .Include(a => a.FileDropUserPermissionGroup)
                                                                 .Where(a => a.FileDrop.ClientId == model.ClientId)
                                                                 .Where(a => a.ApplicationUserId == model.UserId)
                                                                 .ToListAsync();

                            accountsToReset.ForEach(a =>
                            {
                                if (a.FileDropUserPermissionGroup != null)
                                {
                                    a.FileDropUserPermissionGroupId = null;
                                    AuditLogger.Log(AuditEventType.AccountRemovedFromPermissionGroup.ToEvent(a, a.FileDropUserPermissionGroup, a.FileDrop));
                                    Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: account {a.UserName} removed from permission group <{a.FileDropUserPermissionGroup.Name}> because FileDropUser role was removed from user {RequestedUser.UserName}");
                                    if (a.FileDropUserPermissionGroup.IsPersonalGroup)
                                    {
                                        AuditLogger.Log(AuditEventType.FileDropPermissionGroupDeleted.ToEvent(a.FileDrop, a.FileDropUserPermissionGroup));
                                        DbContext.FileDropUserPermissionGroup.Remove(a.FileDropUserPermissionGroup);
                                    }
                                }
                            });
                            break;

                        case RoleEnum.ContentPublisher:
                        case RoleEnum.ContentAccessAdmin:
                            var existingRolesInRootContentItem = await DbContext.UserRoleInRootContentItem
                                .Where(r => r.UserId == RequestedUser.Id)
                                .Where(r => r.RootContentItem.ClientId == model.ClientId)
                                .Where(r => r.Role.RoleEnum == urc.Role.RoleEnum)
                                .ToListAsync();
                            DbContext.UserRoleInRootContentItem.RemoveRange(existingRolesInRootContentItem);
                            break;

                        case RoleEnum.ContentUser:
                            var existingSelectionGroupAssignments = await DbContext.UserInSelectionGroup
                                .Where(usg => usg.UserId == RequestedUser.Id)
                                .Where(usg => usg.SelectionGroup.RootContentItem.ClientId == RequestedClient.Id)
                                .ToListAsync();
                            DbContext.UserInSelectionGroup.RemoveRange(existingSelectionGroupAssignments);
                            break;
                    }
                }
                AuditLogger.Log(AuditEventType.ClientRoleRemoved.ToEvent(RequestedClient, RequestedUser, rolesToRemove.Select(r => r.Role.RoleEnum).ToList(), model.Reason));

                // add roles not already assigned
                if (roleAssignment.IsAssigned && !existingAssignedRoles.Any(r => r.Role.RoleEnum == roleAssignment.RoleEnum))
                {
                    var roleRecord = await RoleManager.Roles.SingleAsync(r => r.RoleEnum == roleAssignment.RoleEnum);
                    DbContext.UserRoleInClient.Add(new UserRoleInClient { UserId = RequestedUser.Id, ClientId = RequestedClient.Id, RoleId = roleRecord.Id });

                    Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: Role {roleRecord.Name} added for username {RequestedUser.UserName} to client {RequestedClient.Id}");
                    AuditLogger.Log(AuditEventType.ClientRoleAssigned.ToEvent(RequestedClient, RequestedUser, new List<RoleEnum> { roleRecord.RoleEnum }, model.Reason));

                    switch (roleAssignment.RoleEnum) 
                    {
                        case RoleEnum.Admin:
                            if (!existingAssignedRoles.Any(urc => urc.Role.RoleEnum == RoleEnum.UserCreator))
                            {
                                ApplicationRole UserCreatorRole = await RoleManager.FindByNameAsync(RoleEnum.UserCreator.ToString());
                                DbContext.UserRoleInClient.Add(new UserRoleInClient { UserId = RequestedUser.Id, RoleId = UserCreatorRole.Id, ClientId = RequestedClient.Id });
                                Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: Role {roleRecord.Name} added for username {RequestedUser.UserName} to client {RequestedClient.Id}");
                                AuditLogger.Log(AuditEventType.ClientRoleAssigned.ToEvent(RequestedClient, RequestedUser, new List<RoleEnum> { roleRecord.RoleEnum }, model.Reason));
                            }
                            break;

                        case RoleEnum.ContentPublisher:
                        case RoleEnum.ContentAccessAdmin:
                            foreach (var rootContentItem in await DbContext.RootContentItem
                                                                           .Where(i => i.ClientId == model.ClientId)
                                                                           .ToListAsync())
                            {
                                var existingRolesInRootContentItem = DbContext.UserRoleInRootContentItem
                                                                              .Where(r => r.UserId == RequestedUser.Id)
                                                                              .Where(r => r.RootContentItemId == rootContentItem.Id)
                                                                              .Where(r => r.Role.RoleEnum == roleAssignment.RoleEnum);
                                if (existingRolesInRootContentItem.Count() == 0)
                                {
                                    DbContext.UserRoleInRootContentItem.Add(new UserRoleInRootContentItem
                                    {
                                        UserId = RequestedUser.Id,
                                        RoleId = roleRecord.Id,
                                        RootContentItemId = rootContentItem.Id,
                                    });
                                }
                            }
                            break;
                    }
                }
            }
            await DbContext.SaveChangesAsync();

            #region Build resulting model
            existingAssignedRoles = await DbContext.UserRoleInClient
                                                   .Where(urc => urc.ClientId == model.ClientId)
                                                   .Where(urc => urc.UserId == model.UserId)
                                                   .ToListAsync();

            SetUserRoleInClientResponseModel ReturnModel = new SetUserRoleInClientResponseModel();

            foreach (RoleEnum x in RolesToManage)
            {
                if (x == RoleEnum.UserCreator) continue;  // UserCreator is hidden from the front end
                ReturnModel.Roles.Add((int)x, new AssignedRoleInfo
                {
                    RoleEnum = x,
                    RoleDisplayValue = x.GetDisplayNameString(),
                    IsAssigned = existingAssignedRoles.Any(urc => urc.RoleId == ApplicationRole.RoleIds[x]),
                });
            }
            string AssignedRolenames = string.Join(", ", ReturnModel.Roles.Values.Where(r => r.IsAssigned).Select(r => r.RoleDisplayValue));
            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: result is user {RequestedUser.UserName} has assigned roles <{AssignedRolenames}>");
            #endregion

            ReturnModel.UserId = model.UserId;

            return Json(ReturnModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetUserRoleInClient([FromBody] SetUserRoleInClientRequestModel model)
        {
            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action for model {{@SetUserRoleInClientRequestModel}}", model);

            #region Authorization
            if (!(await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, model.ClientId))).Succeeded)
            {
                Log.Debug($"In ClientAdminController.SetUserRoleInClient action: authorization failed for user {User.Identity.Name}, role Admin, client {model.ClientId}");
                Response.Headers.Add("Warning", $"You are not authorized to manage this client");
                return Unauthorized();
            }
            #endregion

            ApplicationUser RequestedUser = await _userManager.FindByIdAsync(model.UserId.ToString());
            Client RequestedClient = await DbContext.Client.FindAsync(model.ClientId);

            #region Validation
            // requested user must exist
            if (RequestedUser == null)
            {
                Log.Debug($"In ClientAdminController.SetUserRoleInClient action: requested user ID {model.UserId} not found");
                Response.Headers.Add("Warning", $"The requested user was not found");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // requested client must exist
            if (RequestedClient == null)
            {
                Log.Debug($"In ClientAdminController.SetUserRoleInClient action: requested client ID {model.ClientId} not found");
                Response.Headers.Add("Warning", $"The requested client was not found");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Requested user must be member of requested client
            Claim ClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), model.ClientId.ToString());
            if (!(await _userManager.GetUsersForClaimAsync(ClientMembershipClaim)).Contains(RequestedUser))
            {
                Log.Debug($"In ClientAdminController.SetUserRoleInClient action: requested user ID {model.UserId} not a member of client ID {model.ClientId}");
                Response.Headers.Add("Warning", $"The requested user is not associated with the requested client");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // requested role must exist
            ApplicationRole RequestedRole = (await RoleManager.FindByIdAsync(ApplicationRole.RoleIds[model.RoleEnum].ToString()));
            if (RequestedRole == null)
            {
                Log.Debug($"In ClientAdminController.SetUserRoleInClient action: requested role {model.RoleEnum.ToString()} does not exist");
                Response.Headers.Add("Warning", $"The requested role was not found");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Don't remove the last client admin
            if (model.RoleEnum == RoleEnum.Admin && !model.IsAssigned)
            {
                bool OtherAdminExists = await DbContext.UserRoleInClient.Where(r => r.ClientId == model.ClientId)
                                                                        .Where(r => r.Role.RoleEnum == RoleEnum.Admin)
                                                                        .AnyAsync(r => r.UserId != model.UserId);
                if (!OtherAdminExists)
                {
                    Log.Debug($"In ClientAdminController.RemoveUserFromClient action: unable to remove requested user {model.UserId} from client {model.ClientId}.  User is the sole client administrator");
                    Response.Headers.Add("Warning", "Cannot remove the last client admin user role from the client");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }
            #endregion

            IQueryable<UserRoleInClient> ExistingRecordsForUserAndClientQuery = DbContext.UserRoleInClient
                                                                                         .Where(urc => urc.UserId == RequestedUser.Id
                                                                                                    && urc.ClientId == RequestedClient.Id);

            #region perform the requested action
            List<UserRoleInClient> ExistingRecordsForRequestedRole = await ExistingRecordsForUserAndClientQuery.Where(urc => urc.RoleId == RequestedRole.Id).ToListAsync();

            if (model.IsAssigned)
            {
                // Create role assignment, only if it's not already there
                if (ExistingRecordsForRequestedRole.Count == 0)
                {
                    DbContext.UserRoleInClient.Add(new UserRoleInClient { UserId = RequestedUser.Id, RoleId = RequestedRole.Id, ClientId = RequestedClient.Id });
                    if (RequestedRole.RoleEnum == RoleEnum.Admin)
                    {
                        if (await ExistingRecordsForUserAndClientQuery.Where(urc => urc.Role.RoleEnum == RoleEnum.UserCreator).CountAsync() == 0)
                        {
                            ApplicationRole UserCreatorRole = await RoleManager.FindByNameAsync(RoleEnum.UserCreator.ToString());
                            DbContext.UserRoleInClient.Add(new UserRoleInClient { UserId = RequestedUser.Id, RoleId = UserCreatorRole.Id, ClientId = RequestedClient.Id });
                        }
                    }
                    if (RequestedRole.RoleEnum == RoleEnum.ContentAccessAdmin || RequestedRole.RoleEnum == RoleEnum.ContentPublisher)
                    {
                        foreach (var rootContentItem in await DbContext.RootContentItem
                                                                       .Where(i => i.ClientId == model.ClientId)
                                                                       .ToListAsync())
                        {
                            var existingRolesInRootContentItem = DbContext.UserRoleInRootContentItem
                                                                          .Where(r => r.UserId == RequestedUser.Id)
                                                                          .Where(r => r.RootContentItemId == rootContentItem.Id)
                                                                          .Where(r => r.Role.RoleEnum == RequestedRole.RoleEnum);
                            if (existingRolesInRootContentItem.Count() == 0)
                            {
                                DbContext.UserRoleInRootContentItem.Add(new UserRoleInRootContentItem
                                {
                                    UserId = RequestedUser.Id,
                                    RoleId = RequestedRole.Id,
                                    RootContentItemId = rootContentItem.Id,
                                });
                            }
                        }
                    }
                    await DbContext.SaveChangesAsync();

                    Log.Verbose($"In ClientAdminController.SetUserRoleInClient action: Role {RequestedRole.Name} added for username {RequestedUser.UserName} to client {RequestedClient.Id}");
                    AuditLogger.Log(AuditEventType.ClientRoleAssigned.ToEvent(RequestedClient, RequestedUser, new List<RoleEnum> { RequestedRole.RoleEnum }, model.Reason));
                }
            }
            else
            {
                // Remove role.  There should be only one, but act to remove any number
                if (RequestedRole.RoleEnum == RoleEnum.Admin)
                {
                    ExistingRecordsForRequestedRole = await ExistingRecordsForUserAndClientQuery.Where(urc => urc.RoleId == RequestedRole.Id || 
                                                                                                              urc.Role.RoleEnum == RoleEnum.UserCreator)
                                                                                                .Include(urc => urc.Client)
                                                                                                .Include(urc => urc.User)
                                                                                                .Include(urc => urc.Role)
                                                                                                .ToListAsync();
                }
                if (RequestedRole.RoleEnum == RoleEnum.ContentAccessAdmin || RequestedRole.RoleEnum == RoleEnum.ContentPublisher)
                {
                    var existingRolesInRootContentItem = await DbContext.UserRoleInRootContentItem
                        .Where(r => r.UserId == RequestedUser.Id)
                        .Where(r => r.RootContentItem.ClientId == model.ClientId)
                        .Where(r => r.Role.RoleEnum == RequestedRole.RoleEnum)
                        .ToListAsync();
                    DbContext.UserRoleInRootContentItem.RemoveRange(existingRolesInRootContentItem);
                }
                if (RequestedRole.RoleEnum == RoleEnum.ContentUser)
                {
                    var existingSelectionGroupAssignments = await DbContext.UserInSelectionGroup
                        .Where(usg => usg.UserId == RequestedUser.Id)
                        .Where(usg => usg.SelectionGroup.RootContentItem.ClientId == RequestedClient.Id)
                        .ToListAsync();
                    DbContext.RemoveRange(existingSelectionGroupAssignments);
                }
                if (RequestedRole.RoleEnum == RoleEnum.FileDropUser)
                {
                    var accountsToReset = await DbContext.SftpAccount
                                                         .Include(a => a.FileDrop)
                                                         .Include(a => a.FileDropUserPermissionGroup)
                                                         .Where(a => a.FileDrop.ClientId == model.ClientId)
                                                         .Where(a => a.ApplicationUserId == model.UserId)
                                                         .ToListAsync();

                    accountsToReset.ForEach(a =>
                    {
                        if (a.FileDropUserPermissionGroup != null)
                        {
                            AuditLogger.Log(AuditEventType.AccountRemovedFromPermissionGroup.ToEvent(a, a.FileDropUserPermissionGroup, a.FileDrop));
                            if (a.FileDropUserPermissionGroup.IsPersonalGroup)
                            {
                                AuditLogger.Log(AuditEventType.FileDropPermissionGroupDeleted.ToEvent(a.FileDrop, a.FileDropUserPermissionGroup));
                                DbContext.FileDropUserPermissionGroup.Remove(a.FileDropUserPermissionGroup);
                            }
                        }

                        a.FileDropUserPermissionGroupId = null;
                    });
                }

                DbContext.UserRoleInClient.RemoveRange(ExistingRecordsForRequestedRole);
                await DbContext.SaveChangesAsync();

                Log.Verbose($"In ClientAdminController.SetUserRoleInClient action: Role {RequestedRole.Name} removed for username {RequestedUser.UserName} to client {RequestedClient.Id}");
                foreach (var existingRecord in ExistingRecordsForRequestedRole)
                {
                    AuditLogger.Log(AuditEventType.ClientRoleRemoved.ToEvent(existingRecord.Client, existingRecord.User, new List<RoleEnum> { existingRecord.Role.RoleEnum }, model.Reason));
                }
            }
            #endregion

            #region Build resulting model
            ExistingRecordsForRequestedRole = ExistingRecordsForUserAndClientQuery.ToList();

            SetUserRoleInClientResponseModel ReturnModel = new SetUserRoleInClientResponseModel();


            foreach (RoleEnum x in RolesToManage)
            {
                // UserCreator is currently hidden from the front end
                if (x == RoleEnum.UserCreator) continue;
                ReturnModel.Roles.Add((int) x, new AssignedRoleInfo
                {
                    RoleEnum = x,
                    RoleDisplayValue = x.GetDisplayNameString(),
                    IsAssigned = ExistingRecordsForRequestedRole.Any(urc => urc.RoleId == ApplicationRole.RoleIds[x]),
                });
            }
            string AssignedRolenames = string.Join(", ", ReturnModel.Roles.Values.Where(r => r.IsAssigned).Select(r => r.RoleDisplayValue));
            Log.Verbose($"In ClientAdminController.SetUserRoleInClient action: result is user {RequestedUser.UserName} has assigned roles <{AssignedRolenames}>");
            #endregion

            ReturnModel.UserId = model.UserId;

            return Json(ReturnModel);
        }

        /// <summary>
        /// Removes a requested user from a requested Client. Requires POST. 
        /// </summary>
        /// <param name="Model"></param>
        /// <returns>BadRequestObjectResult, UnauthorizedResult, ClientDetailViewModel</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserFromClient([FromBody]ClientUserAssociationViewModel Model, bool AllowZeroAdmins = false)
        {
            Log.Verbose("Entered ClientAdminController.RemoveUserFromClient action with parameters {@ClientUserAssociationViewModel}, {@AllowZeroAdmins}", Model, AllowZeroAdmins);

            Client RequestedClient = await DbContext.Client.FindAsync(Model.ClientId);

            #region Preliminary validation
            // Requested client must exist
            if (RequestedClient == null)
            {
                Log.Debug($"In ClientAdminController.RemoveUserFromClient action: requested client {Model.ClientId} not found");
                return BadRequest("The requested client does not exist");
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, Model.ClientId));
            if (!Result1.Succeeded)
            {
                Log.Debug("In ClientAdminController.RemoveUserFromClient action: authorization failed for {@Model}", Model);
                return Unauthorized();
            }
            #endregion

            #region Validate the request
            // 1. Requested user must exist
            ApplicationUser RequestedUser = await DbContext.ApplicationUser
                                                     .Where(u => u.Id == Model.UserId)
                                                     .SingleOrDefaultAsync();
            if (RequestedUser == null)
            {
                Log.Debug($"In ClientAdminController.RemoveUserFromClient action: requested user {Model.UserId} not found");
                return BadRequest("The requested user does not exist");
            }

            List<IdentityUserClaim<Guid>> UserClaims = await DbContext.UserClaims
                                                                      .Where(uc => uc.ClaimType == "ClientMembership")
                                                                      .Where(uc => uc.ClaimValue == Model.ClientId.ToString())
                                                                      .Where(uc => uc.UserId == Model.UserId)
                                                                      .ToListAsync();

            // 2. Requested user must be currently assigned to the requested client
            if (!UserClaims.Any())
            {
                Log.Debug($"In ClientAdminController.RemoveUserFromClient action: requested user {Model.UserId} not assigned to client {Model.ClientId}, aborting");
                Response.Headers.Add("Warning", "The requested user is not associated with the requested client");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // 3. At least one client admin is required (unless otherwise specified)
            if (!AllowZeroAdmins)
            {
                bool OtherAdminExists = await DbContext.UserRoleInClient.Where(r => r.ClientId == Model.ClientId)
                                                                        .Where(r => r.Role.RoleEnum == RoleEnum.Admin)
                                                                        .AnyAsync(r => r.UserId != Model.UserId);
                if (!OtherAdminExists)
                {
                    Log.Debug($"In ClientAdminController.RemoveUserFromClient action: unable to remove requested user {Model.UserId} from client {Model.ClientId}.  User is the sole client administrator");
                    Response.Headers.Add("Warning", "Cannot remove the only remaining client admin from the client");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }
            #endregion

            // UserClaims is queried above
            List<UserInSelectionGroup> AllSelectionGroupAssignments = await DbContext.UserInSelectionGroup
                                                                                     .Where(u => u.UserId == RequestedUser.Id)
                                                                                     .Where(u => u.SelectionGroup.RootContentItem.ClientId == RequestedClient.Id)
                                                                                     .ToListAsync();
            List<UserRoleInRootContentItem> AllRootContentItemAssignments = await DbContext.UserRoleInRootContentItem
                                                                                           .Where(r => r.UserId == RequestedUser.Id)
                                                                                           .Where(r => r.RootContentItem.ClientId == RequestedClient.Id)
                                                                                           .ToListAsync();
            List<UserRoleInClient> AllClientRoleAssignments = await DbContext.UserRoleInClient
                                                                             .Where(r => r.UserId == RequestedUser.Id)
                                                                             .Where(r => r.ClientId == RequestedClient.Id)
                                                                             .ToListAsync();

            List<SftpAccount> sftpAccounts = await DbContext.SftpAccount
                                                            .Where(a => a.ApplicationUserId == RequestedUser.Id)
                                                            .Where(a => a.FileDrop.ClientId == RequestedClient.Id)
                                                            .ToListAsync();

            List<FileDropUserPermissionGroup> permissionGroups = await DbContext.FileDropUserPermissionGroup
                                                                                .Where(g => g.IsPersonalGroup)
                                                                                .Where(g => g.SftpAccounts.All(a => a.ApplicationUserId == RequestedUser.Id))
                                                                                .Where(g => g.FileDrop.ClientId == RequestedClient.Id)
                                                                                .ToListAsync();

            try
            {
                DbContext.UserInSelectionGroup.RemoveRange(AllSelectionGroupAssignments);
                DbContext.UserRoleInRootContentItem.RemoveRange(AllRootContentItemAssignments);
                DbContext.UserRoleInClient.RemoveRange(AllClientRoleAssignments);
                DbContext.UserClaims.RemoveRange(UserClaims);
                DbContext.SftpAccount.RemoveRange(sftpAccounts);
                DbContext.FileDropUserPermissionGroup.RemoveRange(permissionGroups);

                await DbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(e, $"Failed to remove user {RequestedUser.UserName} from client {RequestedClient.Name}");
                Log.Error($"In ClientAdminController.RemoveUserFromClient action: {ErrMsg}");
                Response.Headers.Add("Warning", "Error processing request.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            Log.Verbose($"In ClientAdminController.RemoveUserFromClient action: user {Model.UserId} removed from client {Model.ClientId}");
            AuditLogger.Log(AuditEventType.UserRemovedFromClient.ToEvent(RequestedClient, RequestedUser, Model.Reason));

            ClientDetailViewModel ReturnModel = new ClientDetailViewModel { ClientEntity = RequestedClient };
            await ReturnModel.GenerateSupportingProperties(DbContext, _userManager, await _userManager.GetUserAsync(User), RoleEnum.Admin, false);

            return Json(ReturnModel);
        }

        // POST: ClientAdmin/SaveNewClient
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        /// <summary>
        /// Saves a new client object
        /// </summary>
        /// <param name="Model">Type Client</param>
        /// <returns>BadRequestObjectResult, UnauthorizedResult, SaveNewClientResponseModel</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNewClient([FromBody] Client Model)
        {
            Log.Verbose("Entered ClientAdminController.SaveNewClient action with parameter {@Client}", Model);

            ApplicationUser CurrentApplicationUser = await _userManager.GetUserAsync(User);

            #region Preliminary Validation
            if (!ModelState.IsValid)
            {
                var firstInvalidKey = ModelState
                    .Keys.First(key => ModelState[key].ValidationState == ModelValidationState.Invalid);
                Response.Headers.Add("Warning", $"{firstInvalidKey}: {ModelState[firstInvalidKey].Errors.First().ErrorMessage}");
                Log.Warning($"In ClientAdminController.SaveNewClient action: invalid model {string.Join(", ", ModelState.SelectMany(s => s.Value.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest();
            }
            if (Model.ParentClientId == Model.Id)
            {
                Log.Warning($"In ClientAdminController.SaveNewClient action: invalid model, client identifies itself as parent client");
                return BadRequest("Client cannot have itself as a parent Client");
            }
            #endregion

            #region Authorization
            if (!Model.ParentClientId.HasValue)
            {
                // Request to create a root client
                AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInProfitCenterRequirement(RoleEnum.Admin, Model.ProfitCenterId));
                if (!Result1.Succeeded)
                {
                    Log.Debug($"In ClientAdminController.SaveNewClient action: authorization to create root client failed for user {User.Identity.Name}, profit center ID {Model.ProfitCenterId}");
                    return Unauthorized();
                }
            }
            else
            {
                // Request to create a child client
                AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new MapAuthorizationRequirementBase[]
                {
                    new RoleInClientRequirement(RoleEnum.Admin, Model.ParentClientId.Value),
                    new RoleInProfitCenterRequirement(RoleEnum.Admin, Model.ProfitCenterId),
                });
                if (!Result1.Succeeded)
                {
                    Log.Debug($"In ClientAdminController.SaveNewClient action: authorization to create child client failed for user {User.Identity.Name}, profit center ID {Model.ProfitCenterId}, parent client {Model.ParentClientId.Value}");
                    return Unauthorized();
                }
            }
            #endregion Authorization

            #region Validation
            // Convert delimited strings bound from the browser to a proper array
            Model.AcceptedEmailDomainList = GetCleanClientEmailWhitelistList(Model.AcceptedEmailDomainList, true);
            Model.AcceptedEmailAddressExceptionList = GetCleanClientEmailWhitelistList(Model.AcceptedEmailAddressExceptionList, false);

            // Valid domain(s) in whitelist
            foreach (string WhiteListedDomain in Model.AcceptedEmailDomainList)
            {
                if (!GlobalFunctions.IsValidEmail("test@" + WhiteListedDomain))
                {
                    Log.Debug($"In ClientAdminController.SaveNewClient action: requested email whitelist domain <{WhiteListedDomain}>is invalid");
                    Response.Headers.Add("Warning", $"An email domain is invalid: ({WhiteListedDomain})");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }

            // Valid email address(es) in whitelist
            foreach (string WhiteListedAddress in Model.AcceptedEmailAddressExceptionList)
            {
                if (!GlobalFunctions.IsValidEmail(WhiteListedAddress))
                {
                    Log.Debug($"In ClientAdminController.SaveNewClient action: requested email whitelist address <{WhiteListedAddress}>is invalid");
                    Response.Headers.Add("Warning", $"An email address is invalid: ({WhiteListedAddress})");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }

            // Parent client must exist if any
            if (Model.ParentClientId.HasValue && !await DbContext.Client.AnyAsync(c => c.Id == Model.ParentClientId.Value))
            {
                Log.Debug($"In ClientAdminController.SaveNewClient action: requested parent client {Model.ParentClientId.Value} not found");
                Response.Headers.Add("Warning", $"The specified parent Client is invalid: ({Model.ParentClientId.Value})");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Name must be unique
            if (await DbContext.Client.AnyAsync(c => c.Name == Model.Name))
            {
                Log.Debug($"In ClientAdminController.SaveNewClient action: requested client name {Model.Name} already in use by another client");
                Response.Headers.Add("Warning", $"The client name already exists for another client: ({Model.Name})");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Apply domain limit
            if (Model.AcceptedEmailDomainList.Except(GlobalFunctions.NonLimitedDomains).Count() > GlobalFunctions.DefaultClientDomainListCountLimit)
            {
                Log.Debug($"In ClientAdminController.SaveNewClient action: number of domains subject to limit ({{@Domains}}> exceeds the default limit of {GlobalFunctions.DefaultClientDomainListCountLimit}, aborting", Model.AcceptedEmailDomainList.Except(GlobalFunctions.NonLimitedDomains));
                Response.Headers.Add("Warning", $"The requested domain list exceeds the default limit");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion Validation

            // Make sure current user is allowed by email or domain whitelist
            if (!GlobalFunctions.DoesEmailSatisfyClientWhitelists(CurrentApplicationUser.Email, Model.AcceptedEmailDomainList, Model.AcceptedEmailAddressExceptionList))
            {
                Model.AcceptedEmailAddressExceptionList.Add(CurrentApplicationUser.Email);
                Log.Verbose($"In ClientAdminController.SaveNewClient action: automatically added current user {CurrentApplicationUser.UserName} to email exception list of new client");
            }

            using (IDbContextTransaction DbTransaction = await DbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // Add the new Client to local context
                    DbContext.Client.Add(Model);
                    await DbContext.SaveChangesAsync();

                    DbContext.UserClaims.Add(new IdentityUserClaim<Guid> { UserId = CurrentApplicationUser.Id, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = Model.Id.ToString() });
                    await DbContext.SaveChangesAsync();

                    // Add current user's role as ClientAdministrator of new Client to local context
                    DbContext.UserRoleInClient.Add(new UserRoleInClient
                    {
                        ClientId = Model.Id,
                        RoleId = (await RoleManager.FindByNameAsync(RoleEnum.Admin.ToString())).Id,
                        UserId = CurrentApplicationUser.Id
                    });
                    DbContext.UserRoleInClient.Add(new UserRoleInClient
                    {
                        ClientId = Model.Id,
                        RoleId = (await RoleManager.FindByNameAsync(RoleEnum.UserCreator.ToString())).Id,
                        UserId = CurrentApplicationUser.Id
                    });
                    await DbContext.SaveChangesAsync();

                    await DbTransaction.CommitAsync();
                }
                catch (Exception e)
                {
                    string ErrMsg = GlobalFunctions.LoggableExceptionString(e, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Failed to store new client \"{Model.Name}\" to database, or assign client administrator role");
                    Log.Error(ErrMsg);
                    Response.Headers.Add("Warning", "Error processing request.");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }

            // Log new client store and ClientAdministrator role authorization events
            Log.Verbose($"In ClientAdminController.SaveNewClient action: client {Model.Id} created and user {CurrentApplicationUser.UserName} assigned Administrator and UserCreator roles");
            AuditLogger.Log(AuditEventType.ClientCreated.ToEvent(Model));
            AuditLogger.Log(AuditEventType.ClientRoleAssigned.ToEvent(Model, CurrentApplicationUser, new List<RoleEnum> { RoleEnum.Admin, RoleEnum.UserCreator }, HitrustReason.NewMapClient.NumericValue));

            var currentUser = await _userManager.GetUserAsync(User);
            var returnModel = await _clientAdminQueries.GetNewClientResponseModelAsync(currentUser, Model.Id);
            return Json(returnModel);
        }

        // POST: ClientAdmin/EditClient
        /// <summary>
        /// Supports: Edit with no change to parent or ProfitCenter, change of parent if no children
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClient([FromBody]Client Model)
        {
            Log.Verbose("Entered ClientAdminController.EditClient action with model {@Client}", Model);

            Client ExistingClientRecord = await DbContext.Client.FindAsync(Model.Id);

            #region Preliminary Validation
            if (Model.Id == null || Model.Id == Guid.Empty)
            {
                Log.Debug("In ClientAdminController.EditClient action: no client ID provided");
                return BadRequest($"Requested Client.Id ({Model.Id}) not valid");
            }

            if (Model.ParentClientId == Model.Id)
            {
                Log.Debug("In ClientAdminController.EditClient action: model client references itself as parent client");
                return BadRequest("Client cannot have itself as a parent Client");
            }

            // Client must exist
            if (ExistingClientRecord == null)
            {
                Log.Warning("In ClientAdminController.EditClient action: referenced client not found");
                return BadRequest("The modified client was not found in the system.");
            }
            #endregion

            #region Authorization
            // 1) Changing Parent is not supported
            if (Model.ParentClientId != ExistingClientRecord.ParentClientId)
            {
                Log.Warning("In ClientAdminController.EditClient action: parent client ID may not be changed, aborting");
                Response.Headers.Add("Warning", "Client may not be moved to a new parent Client");
                return Unauthorized();
            }

            // 2) User must have ClientAdministrator role for the edited Client
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, Model.Id));
            if (!Result1.Succeeded)
            {
                Log.Information($"In ClientAdminController.EditClient action: current user {User.Identity.Name} is not administrator of client {Model.Id}");
                Response.Headers.Add("Warning", $"The requesting user is not a ClientAdministrator for the requested client ({ExistingClientRecord.Name})");
                return Unauthorized();
            }

            // 3) Conditionally handle special cases
            if (Model.ProfitCenterId != ExistingClientRecord.ProfitCenterId)
            {
                // Request to change the Client's ProfitCenter reference
                AuthorizationResult Result2 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInProfitCenterRequirement(RoleEnum.Admin, Model.ProfitCenterId));
                if (!Result2.Succeeded)
                {
                    Log.Information($"In ClientAdminController.EditClient action: profit center change was requested but current user {User.Identity.Name} is not authorized to new profit center {Model.ProfitCenterId}");
                    Response.Headers.Add("Warning", "You are not authorized to assign clients to the specified profit center, authorization failure");
                    return Unauthorized();
                }
            }
            else
            {
                // 
            }
            #endregion Authorization

            #region Validation
            if (!ModelState.IsValid)
            {
                Log.Warning($"In ClientAdminController.EditClient action: invalid model, errors: {string.Join(", ", ModelState.SelectMany(s => s.Value.Errors).Select(e => e.ErrorMessage))}");
                Response.Headers.Add("Warning", ModelState
                    .Values.First(value => value.ValidationState == ModelValidationState.Invalid)
                    .Errors.First().ErrorMessage);
                return BadRequest();
            }

            // Convert delimited strings bound from the browser to a proper List
            Model.AcceptedEmailDomainList = GetCleanClientEmailWhitelistList(Model.AcceptedEmailDomainList, true);
            Model.AcceptedEmailAddressExceptionList = GetCleanClientEmailWhitelistList(Model.AcceptedEmailAddressExceptionList, false);
            
            // Apply domain limit
            if (Model.AcceptedEmailDomainList.Except(GlobalFunctions.NonLimitedDomains).Count() > ExistingClientRecord.DomainListCountLimit)
            {
                Log.Debug($"In ClientAdminController.EditClient action: number of requested domains {{@WhiteListedDomains}} exceeds the configured limit of {ExistingClientRecord.DomainListCountLimit}, aborting", Model.AcceptedEmailDomainList.Except(GlobalFunctions.NonLimitedDomains));
                Response.Headers.Add("Warning", $"The domain list exceeds the configured limit");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Valid domains in domain whitelist
            foreach (string WhiteListedDomain in Model.AcceptedEmailDomainList)
            {
                if (!GlobalFunctions.IsValidEmail("test@" + WhiteListedDomain))
                {
                    Log.Debug($"In ClientAdminController.EditClient action: invalid email domain {WhiteListedDomain} specified in whitelist, aborting");
                    Response.Headers.Add("Warning", $"The domain is invalid: ({WhiteListedDomain})");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }

            // Valid addresses in address whitelist
            foreach (string WhiteListedAddress in Model.AcceptedEmailAddressExceptionList)
            {
                if (!GlobalFunctions.IsValidEmail(WhiteListedAddress))
                {
                    Log.Debug($"In ClientAdminController.EditClient action: invalid email address {WhiteListedAddress} specified in whitelist, aborting");
                    Response.Headers.Add("Warning", $"The exception address is invalid: ({WhiteListedAddress})");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }

            // Parent client (if any) must exist
            if (Model.ParentClientId.HasValue && !await DbContext.Client.AnyAsync(c => c.Id == Model.ParentClientId.Value))
            {
                Log.Warning($"In ClientAdminController.EditClient action: client {Model.Id} references a parent client with ID {Model.ParentClientId.Value} that is not found");
                Response.Headers.Add("Warning", "The specified parent of the client is invalid.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // ProfitCenter must exist
            if (!await DbContext.ProfitCenter.AnyAsync(pc => pc.Id == Model.ProfitCenterId))
            {
                Log.Warning($"In ClientAdminController.EditClient action: referenced profit center with ID {Model.ProfitCenterId} not found");
                Response.Headers.Add("Warning", "The specified ProfitCenter is invalid.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Name must be unique
            if (await DbContext.Client.AnyAsync(c => c.Name == Model.Name && 
                                                     c.Id != Model.Id))
            {
                Log.Debug($"In ClientAdminController.EditClient action: requested client name {Model.Name} already in use");
                Response.Headers.Add("Warning", $"The client name ({Model.Name}) already exists for another client.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion Validation

            // Perform the update
            try
            {
                using (IDbContextTransaction Tx = await DbContext.Database.BeginTransactionAsync())
                {
                    // Remove any user for which the email does not match new domain or address whitelist
                    string existingIdString = ExistingClientRecord.Id.ToString();
                    var AllClientMemberUserIds = await DbContext.UserClaims
                                                                .Where(c => c.ClaimType == "ClientMembership")
                                                                .Where(c => c.ClaimValue == existingIdString)
                                                                .Select(r => r.UserId)
                                                                .ToListAsync();
                    IQueryable<ApplicationUser> AllClientMemberUsers = DbContext.ApplicationUser.Where(u => AllClientMemberUserIds.Contains(u.Id));
                    foreach (ApplicationUser ClientMemberUser in AllClientMemberUsers)
                    {
                        if (!GlobalFunctions.DoesEmailSatisfyClientWhitelists(ClientMemberUser.Email, Model.AcceptedEmailDomainList, Model.AcceptedEmailAddressExceptionList))
                        {
                            // Make sure RemoveUserFromClient() doesn't start using a transaction iternally
                            IActionResult result = await RemoveUserFromClient(new ClientUserAssociationViewModel { UserId = ClientMemberUser.Id, ClientId = Model.Id });

                            if (result.GetType() != typeof(JsonResult))
                            {
                                Log.Information($"In ClientAdminController.EditClient action: failed to remove user from client in response to modified email whitelist");
                                await Tx.RollbackAsync();
                                return result;
                            }
                        }
                    }

                    // Must update the instance that's tracked by EF, can't update the context with the untracked Model object
                    ExistingClientRecord.Name = Model.Name;
                    ExistingClientRecord.ClientCode = Model.ClientCode;
                    ExistingClientRecord.ContactName = Model.ContactName;
                    ExistingClientRecord.ContactTitle = Model.ContactTitle;
                    
                    if (ExistingClientRecord.ContactEmail != Model.ContactEmail && !(ModelState.Keys.Contains("ContactEmail") && ModelState["ContactEmail"].Errors.Any()))
                    {
                        ExistingClientRecord.ContactEmail = Model.ContactEmail;
                    }
                    ExistingClientRecord.ContactPhone = Model.ContactPhone;
                    ExistingClientRecord.ConsultantName = Model.ConsultantName;
                    ExistingClientRecord.ConsultantEmail = Model.ConsultantEmail;
                    ExistingClientRecord.ConsultantOffice = Model.ConsultantOffice;
                    ExistingClientRecord.AcceptedEmailDomainList = Model.AcceptedEmailDomainList;
                    ExistingClientRecord.AcceptedEmailAddressExceptionList = Model.AcceptedEmailAddressExceptionList;
                    //Not supported:  ExistingClientRecord.ParentClientId = Model.ParentClientId;
                    ExistingClientRecord.ProfitCenterId = Model.ProfitCenterId;
                    ExistingClientRecord.NewUserWelcomeText = Model.NewUserWelcomeText;

                    await DbContext.SaveChangesAsync();
                    await Tx.CommitAsync();
                }

                Log.Verbose($"In ClientAdminController.EditClient action: client {ExistingClientRecord.Id} updated");
                AuditLogger.Log(AuditEventType.ClientEdited.ToEvent(Model));
            }
            catch (Exception ex)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Failed to update client {Model.Id} to database");
                Log.Error($"In ClientAdminController.EditClient action: {ErrMsg}");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var clients = await _clientAdminQueries.GetAuthorizedClientsModelAsync(currentUser);

            return Json(clients);
        }

        /// <summary>
        /// Deletes a Client record
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClient([FromBody] Guid Id)
        {
            Log.Verbose($"Entered ClientAdminController.DeleteClient action with client ID {Id}");

            // Query for the existing record to be modified
            Client ExistingClient = await DbContext.Client.FindAsync(Id);

            #region Preliminary validation
            if (ExistingClient == null)
            {
                Log.Verbose($"In ClientAdminController.DeleteClient action: client {Id} not found, aborting");
                Response.Headers.Add("Warning", "Client not found, unable to delete.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            ApplicationUser CurrentUser = await _userManager.GetUserAsync(User);
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new MapAuthorizationRequirementBase[]
                {
                    new RoleInClientRequirement(RoleEnum.Admin, Id),
                    new RoleInProfitCenterRequirement(RoleEnum.Admin, ExistingClient.ProfitCenterId)
                });
            if (!Result1.Succeeded)
            {
                Log.Debug($"In ClientAdminController.DeleteClient action: authorization failure for current user {CurrentUser.UserName}, required roles are client admin and profit center admin");
                Response.Headers.Add("Warning", "You are not authorized to delete this client");
                return Unauthorized();
            }
            #endregion Authorization

            #region Validation
            // Client must not be parent of any other Client
            List<string> Children = await DbContext.Client
                                                   .Where(c => c.ParentClientId == Id)
                                                   .Select(c => c.Name)
                                                   .ToListAsync();
            if (Children.Count > 0)
            {
                Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: requested client {ExistingClient.Id} has child client(s) {string.Join(", ", Children)}, aborting");
                Response.Headers.Add("Warning", $"Can't delete Client {ExistingClient.Name}. The client has child client(s): {string.Join(", ", Children)}");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Client must not have any root content items
            if (await DbContext.RootContentItem.CountAsync(i => i.ClientId == Id) > 0)
            {
                Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: requested client {ExistingClient.Id} has content item(s), aborting");
                Response.Headers.Add("Warning", $"Can't delete client {ExistingClient.Name} because it has content items.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Client must not have any active File Drops
            if (await DbContext.FileDrop.CountAsync(i => i.ClientId == Id) > 0)
            {
              Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: requested client {ExistingClient.Id} has File Drop(s), aborting");
              Response.Headers.Add("Warning", $"Can't delete client {ExistingClient.Name} because it has File Drop(s).");
              return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion Validation

            using (IDbContextTransaction DbTransaction = await DbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // Remove all users from the Client
                    List<ApplicationUser> AllClientUsers = await DbContext.UserClaims
                                                                          .Where(c => c.ClaimType == "ClientMembership")
                                                                          .Where(c => c.ClaimValue == ExistingClient.Id.ToString())
                                                                          .Join(DbContext.ApplicationUser, c => c.UserId, u => u.Id, (c, u) => u)
                                                                          .ToListAsync();

                    foreach (ApplicationUser user in AllClientUsers)
                    {
                        await RemoveUserFromClient(new ClientUserAssociationViewModel { ClientId = ExistingClient.Id, UserId = user.Id }, true);
                    }

                    // Remove the client
                    DbContext.Client.Remove(ExistingClient);

                    await DbContext.SaveChangesAsync();
                    await DbTransaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Failed to delete client from database");
                    Log.Error($"In {ControllerContext.ActionDescriptor.DisplayName} action: {ErrMsg}");
                    Response.Headers.Add("Warning", "Error processing request.");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }

            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: deleted client {ExistingClient.Id}");
            AuditLogger.Log(AuditEventType.ClientDeleted.ToEvent(ExistingClient));

            var currentUser = await _userManager.GetUserAsync(User);
            var clients = await _clientAdminQueries.GetAuthorizedClientsModelAsync(currentUser);

            return Json(clients);
        }

        /// <summary>
        /// Creates a support request to re-enable a disabled user account
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReenableUserAccount([FromBody] RequestReenableUserAccountRequestModel Model)
        {
            Log.Verbose($"Entered {ControllerContext.ActionDescriptor.DisplayName} action with model {@Model}", Model);

            Client ExistingClientRecord = await DbContext.Client.FindAsync(Model.ClientId);
            ApplicationUser RequestedAccount = await DbContext.Users.FindAsync(Model.UserId);

            #region Preliminary Validation
            if (Model.ClientId == null || Model.ClientId == Guid.Empty || (Model.UserId == null || Model.UserId == Guid.Empty))
            {
                Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: no client ID provided");
                return BadRequest("An error has occcurred.");
            }
            if (Model.UserId == null || Model.UserId == Guid.Empty)
            {
                Log.Debug($"In {ControllerContext.ActionDescriptor.DisplayName} action: no user ID provided");
                return BadRequest("An error has occcurred.");
            }

            // Client must exist
            if (ExistingClientRecord == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} action: referenced client not found");
                return BadRequest("The modified client was not found in the system.");
            }
            // Disabled account must exist
            if (RequestedAccount == null)
            {
                Log.Warning($"In {ControllerContext.ActionDescriptor.DisplayName} action: requested user account not found");
                return BadRequest("The requested user account was not foun in the system.");
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, Model.ClientId));
            if (!Result1.Succeeded)
            {
                Log.Information($"In {ControllerContext.ActionDescriptor.DisplayName} action: current user {User.Identity.Name} is not administrator of client {Model.ClientId}");
                Response.Headers.Add("Warning", $"The requesting user is not a ClientAdministrator for the requested client ({ExistingClientRecord.Name})");
                return Unauthorized();
            }
            #endregion

            ApplicationUser Requestor = await _userManager.FindByNameAsync(User.Identity.Name);

            // Email.
            string emailSubject = "Request to re-enable user";
            string emailBody = $"A Client Admin has requested that an existing MAP user account be re-enabled. {Environment.NewLine}{Environment.NewLine}";
            emailBody += $"Requestor: {Requestor.FirstName} {Requestor.LastName} ({Requestor.UserName}) {Environment.NewLine}";
            emailBody += $"Account to re-enable: {RequestedAccount.FirstName} {RequestedAccount.LastName} ({RequestedAccount.UserName}) {Environment.NewLine}";
            emailBody += $"Reason: {ReenableDisabledAccountReason.ReenableReasons.Find(f => f.NumericValue == Model.Reason).Description}";

            string supportEmailAlias = ApplicationConfig.GetValue<string>("SecurityEmailAlias");
            MessageQueueService.QueueEmail(supportEmailAlias, emailSubject, emailBody);

            Log.Verbose($"In {ControllerContext.ActionDescriptor.DisplayName} action: Client Admin {Requestor.Id} requested user {RequestedAccount.Id} be re-enabled. Support email sent.");
            AuditLogger.Log(AuditEventType.ClientAdminRequestedUserAccountReenable.ToEvent(ExistingClientRecord, Requestor, RequestedAccount, Model.Reason));

            return Json(new { });
        }

        /// <summary>
        /// Returns an array of individual whitelist items without nulls, optionally tested for validity as either domain or full email address
        /// </summary>
        /// <param name="inList">0 or more strings that may contain 0 or more email entries or a delimited list</param>
        /// <param name="cleanDomain">If true, strip characters up through '@' from each found element</param>
        /// <returns></returns>
        [NonAction]
        private List<string> GetCleanClientEmailWhitelistList(List<string> inList, bool cleanDomain)
        {
            char[] stringDelimiters = new char[] { ',', ';', ' ' };

            List<string> Result = new List<string>();

            foreach (string element in inList.Where(e => !string.IsNullOrWhiteSpace(e)))  // Normally from model binding there will be exactly 1
            {
                foreach (string goodElement in element.Split(stringDelimiters, StringSplitOptions.RemoveEmptyEntries))
                {
                    Result.Add(goodElement.Trim());
                }
            }

            if (cleanDomain)
            {
                Result = Result.Select(d => d.Contains("@") ? d.Substring(d.LastIndexOf('@') + 1) : d).ToList();
            }

            return Result;
        }

    }
}
