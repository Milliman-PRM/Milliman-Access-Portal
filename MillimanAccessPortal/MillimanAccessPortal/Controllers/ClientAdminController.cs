/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing authorized content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
using AuditLogLib.Services;
using MillimanAccessPortal.Models.ClientAdminViewModels;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using MillimanAccessPortal.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using AuditLogLib.Event;

namespace MillimanAccessPortal.Controllers
{
    public class ClientAdminController : Controller
    {
        private readonly static List<RoleEnum> RolesToManage = new List<RoleEnum> { RoleEnum.Admin, RoleEnum.ContentPublisher, RoleEnum.ContentUser, RoleEnum.ContentAccessAdmin };

        private readonly ApplicationDbContext DbContext;
        private readonly IAuditLogger AuditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ILogger Logger;
        private readonly IMessageQueue MessageQueueService;
        private readonly RoleManager<ApplicationRole> RoleManager;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly IConfiguration ApplicationConfig;
        private readonly AccountController _accountController;

        public ClientAdminController(
            ApplicationDbContext context,
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ILoggerFactory LoggerFactoryArg,
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
            Logger = LoggerFactoryArg.CreateLogger<ClientAdminController>();
            MessageQueueService = MessageQueueServiceArg;
            RoleManager = RoleManagerArg;
            Queries = QueryArg;
            UserManager = UserManagerArg;
            ApplicationConfig = ApplicationConfigArg;
            _accountController = AccountControllerArg;
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
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, null));
            AuthorizationResult Result2 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInProfitCenterRequirement(RoleEnum.Admin, null));
            if (!Result1.Succeeded && !Result2.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to the Client Admin page.");
                return Unauthorized();
            }
            #endregion

            return View();
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
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, null));
            AuthorizationResult Result2 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInProfitCenterRequirement(RoleEnum.Admin, null));
            if (!Result1.Succeeded &&
                !Result2.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized as a client admin or profit center admin");
                return Unauthorized();
            }
            #endregion

            ClientAdminIndexViewModel ModelToReturn = await ClientAdminIndexViewModel.GetClientAdminIndexModelForUser(await Queries.GetCurrentApplicationUser(User), UserManager, DbContext, ApplicationConfig["Global:DefaultNewUserWelcomeText"]);

            return Json(ModelToReturn);
        }

        // GET: ClientAdmin/ClientDetail
        // Intended for access by ajax from Index view
        /// <summary>
        /// Returns the requested Client and lists of eligible and already assigned users associated with a Client. Requires GET. 
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns>JsonResult or UnauthorizedResult</returns>
        [HttpGet]
        public async Task<IActionResult> ClientDetail(Guid clientId)
        {
            Client ThisClient = DbContext.Client.Include(c => c.ProfitCenter).FirstOrDefault(c => c.Id == clientId);

            #region Validation
            if (ThisClient == null)
            {
                Response.Headers.Add("Warning", $"The requested client was not found");
                return NotFound();
            }
            #endregion

            #region Authorization
            // Check current user's authorization to manage the requested Client
            List<Guid> AllRelatedClientsList = Queries.GetAllRelatedClients(ThisClient).Select(c => c.Id).ToList();

            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInAnySuppliedClientRequirement(RoleEnum.Admin, AllRelatedClientsList));
            if (!Result1.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to administer the requested client");
                return Unauthorized();
            }
            #endregion

            ClientDetailViewModel Model = new ClientDetailViewModel { ClientEntity = ThisClient };
            await Model.GenerateSupportingProperties(DbContext, UserManager, await Queries.GetCurrentApplicationUser(User), RoleEnum.Admin, false);

            return Json(Model);
        }

        // POST: ClientAdmin/SaveNewUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveNewUser([Bind("UserName,Email,MemberOfClientId")]ApplicationUserViewModel Model)
        {
            ApplicationUser RequestedUser = null;

            #region If user already exists get the record
            if (!string.IsNullOrWhiteSpace(Model.Email))
            {
                RequestedUser = await UserManager.FindByEmailAsync(Model.Email);
            }
            if (RequestedUser == null && !string.IsNullOrWhiteSpace(Model.UserName))
            {
                RequestedUser = await UserManager.FindByNameAsync(Model.UserName);
            }
            
            bool RequestedUserIsNew = (RequestedUser == null);

            Client RequestedClient = DbContext.Client.SingleOrDefault(c => c.Id == Model.MemberOfClientId);
            if (RequestedClient == null)
            {
                Response.Headers.Add("Warning", "The requested Client does not exist");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            #region Authorization
            // If creating a new user, current user must either have global UserCreator role or UserCreator role for requested client
            if (RequestedUserIsNew)
            {
                if (Model.MemberOfClientId == null)
                {
                    AuthorizationResult GlobalUserCreatorResult = await AuthorizationService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.UserCreator));
                    if (!GlobalUserCreatorResult.Succeeded)
                    {
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
                    Response.Headers.Add("MapReason", "101");
                    Response.Headers.Add("Warning", $"The provided email address ({Model.Email}) is not valid");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }

                // 2. Make sure the UserName does not exist in the database already as a UserName or Email
                if (RequestedUserIsNew &&
                    (DbContext.ApplicationUser.Any(u => u.UserName == Model.UserName) ||
                        DbContext.ApplicationUser.Any(u => u.Email == Model.UserName)))
                {
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
            if (!Queries.DoesEmailSatisfyClientWhitelists(Model.Email, RequestedClient.AcceptedEmailDomainList, RequestedClient.AcceptedEmailAddressExceptionList))
            {
                // TODO consider in the future, prompt to edit the whitelist
                Response.Headers.Add("Warning", $"The requested user email ({Model.Email}) is not permitted for the requested client.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion Validation

            try
            {
                // Create requested user if not already existing
                if (RequestedUserIsNew)
                {
                    IdentityResult result;
                    // Creates new user with logins disabled (EmailConfirmed == false) and no password. Password is added in AccountController.EnableAccount()
                    (result, RequestedUser) = await Queries.CreateNewAccount(Model.UserName, Model.Email);

                    if (result.Succeeded && RequestedUser != null)
                    {
                        // Configurable portion of email body
                        string welcomeText = !string.IsNullOrWhiteSpace(RequestedClient.NewUserWelcomeText)
                            ? RequestedClient.NewUserWelcomeText
                            : ApplicationConfig["Global:DefaultNewUserWelcomeText"];  // could be null, that's ok

                        await _accountController.SendNewAccountWelcomeEmail(RequestedUser, Url, welcomeText);
                    }
                    else
                    {
                        string Errors = string.Join($", ", result.Errors.Select(e => e.Description));
                        Response.Headers.Add("Warning", $"Error while creating user ({Model.UserName}) in database: {Errors}");
                        return StatusCode(StatusCodes.Status422UnprocessableEntity);
                    }
                }

                Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), Model.MemberOfClientId.ToString());
                var CurrentClaimsOfRequestedUser = await UserManager.GetClaimsAsync(RequestedUser);
                if (!CurrentClaimsOfRequestedUser.Any(c => c.Type == ThisClientMembershipClaim.Type && c.Value == ThisClientMembershipClaim.Value))
                {
                    await UserManager.AddClaimAsync(RequestedUser, ThisClientMembershipClaim);
                }

                DbContext.SaveChanges();

                AuditLogger.Log(AuditEventType.UserAssignedToClient.ToEvent(RequestedClient, RequestedUser));
            }
            catch (Exception e)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(e, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Exception while creating new user \"{Model.UserName}\" or assigning user membership in client(s): [{Model.MemberOfClientId.ToString()}]");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", "Failed to complete operation");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            Response.Headers.Add("Warning", $"The requested user was successfully saved");
            return Ok("New User saved successfully");
        }

        /// <summary>
        /// Assigns a requested ApplicationUser to a requested Client.  Requires POST.
        /// </summary>
        /// <param name="Model">type ClientUserAssociationViewModel</param>
        /// <returns>BadRequestObjectResult, UnauthorizedResult, or OkResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUserToClient(ClientUserAssociationViewModel Model)
        {
            Client RequestedClient = DbContext.Client.Find(Model.ClientId);

            #region Preliminary validation - Requested client must exist
            if (RequestedClient == null)
            {
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
                return Unauthorized();
            }
            #endregion

            #region Validate the request
            // 1. Requested user must exist
            ApplicationUser RequestedUser = DbContext
                                            .ApplicationUser
                                            .Where(u => u.Id == Model.UserId)
                                            .SingleOrDefault();
            if (RequestedUser == null)
            {
                return BadRequest("The requested user does not exist");
            }

            // 2. Requested User's email must comply with client email whitelist
            string RequestedUserEmail = RequestedUser.Email.ToUpper();
            if (!GlobalFunctions.IsValidEmail(RequestedUserEmail))
            {
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
                Response.Headers.Add("Warning", "The requested user's email is not accepted for this client");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), RequestedClient.Id.ToString());

            IList<Claim> CurrentUserClaims = await UserManager.GetClaimsAsync(RequestedUser);
            if (CurrentUserClaims.Any(claim => claim.Type == ThisClientMembershipClaim.Type && 
                                               claim.Value == ThisClientMembershipClaim.Value))
            {
                Response.Headers.Add("Warning", "The requested user is already assigned to the requested client");
            }
            else
            {
                IdentityResult ResultOfAddClaim = await UserManager.AddClaimAsync(RequestedUser, ThisClientMembershipClaim);
                if (ResultOfAddClaim != IdentityResult.Success)
                {
                    string ErrMsg = $"Failed to add claim for user {RequestedUser.UserName}: Claim={ThisClientMembershipClaim.Type}.{ThisClientMembershipClaim.Value}";
                    Logger.LogError(ErrMsg);
                    return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
                }

                AuditLogger.Log(AuditEventType.UserAssignedToClient.ToEvent(RequestedClient, RequestedUser));
            }

            ClientDetailViewModel ReturnModel = new ClientDetailViewModel { ClientEntity = RequestedClient };
            await ReturnModel.GenerateSupportingProperties(DbContext, UserManager, await Queries.GetCurrentApplicationUser(User), RoleEnum.Admin, false);

            return Json(ReturnModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetUserRoleInClient([Bind("ClientId,UserId")]ClientUserAssociationViewModel ClientUserModel, [Bind("RoleEnum,IsAssigned")]AssignedRoleInfo AssignedRoleInfoArg)
        {
            #region Authorization
            if (!AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, ClientUserModel.ClientId)).Result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to manage this client");
                return Unauthorized();
            }
            #endregion

            #region Validation
            // requested user must exist
            ApplicationUser RequestedUser = await UserManager.FindByIdAsync(ClientUserModel.UserId.ToString());
            if (RequestedUser == null)
            {
                Response.Headers.Add("Warning", $"The requested user was not found");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // requested client must exist
            Client RequestedClient = DbContext.Client.Find(ClientUserModel.ClientId);
            if (RequestedClient == null)
            {
                Response.Headers.Add("Warning", $"The requested client was not found");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Requested user must be member of requested client
            Claim ClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), ClientUserModel.ClientId.ToString());
            if (!UserManager.GetUsersForClaimAsync(ClientMembershipClaim).Result.Contains(RequestedUser))
            {
                Response.Headers.Add("Warning", $"The requested user is not associated with the requested client");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // requested role must exist
            ApplicationRole RequestedRole = RoleManager.FindByIdAsync(ApplicationRole.RoleIds[AssignedRoleInfoArg.RoleEnum].ToString()).Result;
            if (RequestedRole == null)
            {
                Response.Headers.Add("Warning", $"The requested role was not found");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion

            IQueryable<UserRoleInClient> ExistingRecordsQuery = DbContext.UserRoleInClient
                                                                         .Where(urc => urc.UserId == RequestedUser.Id
                                                                                    && urc.ClientId == RequestedClient.Id);

            #region perform the requested action
            List<UserRoleInClient> ExistingRecords = ExistingRecordsQuery.Where(urc => urc.RoleId == RequestedRole.Id).ToList();

            if (AssignedRoleInfoArg.IsAssigned)
            {
                // Create role assignment, only if it's not already there
                if (ExistingRecords.Count == 0)
                {
                    DbContext.UserRoleInClient.Add(new UserRoleInClient { UserId = RequestedUser.Id, RoleId = RequestedRole.Id, ClientId = RequestedClient.Id });
                    if (RequestedRole.RoleEnum == RoleEnum.Admin)
                    {
                        if (ExistingRecordsQuery.Where(urc => urc.Role.RoleEnum == RoleEnum.UserCreator).Count() == 0)
                        {
                            ApplicationRole UserCreatorRole = await RoleManager.FindByNameAsync(RoleEnum.UserCreator.ToString());
                            DbContext.UserRoleInClient.Add(new UserRoleInClient { UserId = RequestedUser.Id, RoleId = UserCreatorRole.Id, ClientId = RequestedClient.Id });
                        }
                    }
                    if (RequestedRole.RoleEnum == RoleEnum.ContentAccessAdmin || RequestedRole.RoleEnum == RoleEnum.ContentPublisher)
                    {
                        foreach (var rootContentItem in DbContext.RootContentItem.Where(i => i.ClientId == ClientUserModel.ClientId))
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
                    DbContext.SaveChanges();
                    AuditLogger.Log(AuditEventType.ClientRoleAssigned.ToEvent(RequestedClient, RequestedUser, new List<RoleEnum> { RequestedRole.RoleEnum }));
                }
            }
            else
            {
                // Remove role.  There should be only one, but act to remove any number
                if (RequestedRole.RoleEnum == RoleEnum.Admin)
                {
                    ExistingRecords = ExistingRecordsQuery.Where(urc => (urc.RoleId == RequestedRole.Id) || (urc.Role.RoleEnum == RoleEnum.UserCreator)).ToList();
                }
                if (RequestedRole.RoleEnum == RoleEnum.ContentAccessAdmin || RequestedRole.RoleEnum == RoleEnum.ContentPublisher)
                {
                    var existingRolesInRootContentItem = DbContext.UserRoleInRootContentItem
                        .Where(r => r.UserId == RequestedUser.Id)
                        .Where(r => r.RootContentItem.ClientId == ClientUserModel.ClientId)
                        .Where(r => r.Role.RoleEnum == RequestedRole.RoleEnum)
                        .ToList();
                    DbContext.UserRoleInRootContentItem.RemoveRange(existingRolesInRootContentItem);
                }
                DbContext.UserRoleInClient.RemoveRange(ExistingRecords);
                DbContext.SaveChanges();

                foreach (var existingRecord in ExistingRecords)
                {
                    AuditLogger.Log(AuditEventType.ClientRoleRemoved.ToEvent(existingRecord.Client, existingRecord.User, new List<RoleEnum> { existingRecord.Role.RoleEnum }));
                }
            }
            #endregion

            #region Build resulting model
            ExistingRecords = ExistingRecordsQuery.ToList();

            List<AssignedRoleInfo> ReturnModel = new List<AssignedRoleInfo>();
            foreach (RoleEnum x in RolesToManage)
            {
                // UserCreator is currently hidden from the front end
                if (x == RoleEnum.UserCreator) continue;
                ReturnModel.Add(new AssignedRoleInfo
                {
                    RoleEnum = x,
                    RoleDisplayValue = ApplicationRole.RoleDisplayNames[x],
                    IsAssigned = ExistingRecords.Any(urc => urc.RoleId == ApplicationRole.RoleIds[x]),
                });
            }
            #endregion

            return Json(ReturnModel);
        }

        /// <summary>
        /// Removes a requested user from a requested Client. Requires POST. 
        /// </summary>
        /// <param name="Model"></param>
        /// <returns>BadRequestObjectResult, UnauthorizedResult, OkResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserFromClient(ClientUserAssociationViewModel Model, bool AllowZeroAdmins = false)
        {
            Client RequestedClient = DbContext.Client.Find(Model.ClientId);

            #region Preliminary validation
            // Requested client must exist
            if (RequestedClient == null)
            {
                return BadRequest("The requested client does not exist");
            }
            #endregion

            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, Model.ClientId));
            if (!Result1.Succeeded)
            {
                return Unauthorized();
            }
            #endregion

            #region Validate the request
            // 1. Requested user must exist
            ApplicationUser RequestedUser = DbContext.ApplicationUser
                                                     .Where(u => u.Id == Model.UserId)
                                                     .SingleOrDefault();
            if (RequestedUser == null)
            {
                return BadRequest("The requested user does not exist");
            }

            List<IdentityUserClaim<Guid>> UserClaims = DbContext.UserClaims
                                                                .Where(uc => uc.ClaimType == "ClientMembership")
                                                                .Where(uc => uc.ClaimValue == Model.ClientId.ToString())
                                                                .Where(uc => uc.UserId == Model.UserId)
                                                                .ToList();

            // 2. Requested user must be currently assigned to the requested client
            if (!UserClaims.Any())
            {
                Response.Headers.Add("Warning", "The requested user is not associated with the requested client");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // 3. At least one client admin is required (unless otherwise specified)
            if (!AllowZeroAdmins)
            {
                bool OtherAdminExists = DbContext.UserRoleInClient.Where(r => r.ClientId == Model.ClientId)
                                                                  .Where(r => r.Role.RoleEnum == RoleEnum.Admin)
                                                                  .Any(r => r.UserId != Model.UserId);
                if (!OtherAdminExists)
                {
                    Response.Headers.Add("Warning", "Cannot remove the only remaining client admin from the client");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }
            #endregion

            // UserClaims is queried above
            List<UserInSelectionGroup> AllSelectionGroupAssignments = DbContext.UserInSelectionGroup
                .Where(u => u.UserId == RequestedUser.Id)
                .Where(u => u.SelectionGroup.RootContentItem.ClientId == RequestedClient.Id)
                .ToList();
            List<UserRoleInRootContentItem> AllRootContentItemAssignments = DbContext.UserRoleInRootContentItem
                .Where(r => r.UserId == RequestedUser.Id)
                .Where(r => r.RootContentItem.ClientId == RequestedClient.Id)
                .ToList();
            List<UserRoleInClient> AllClientRoleAssignments = DbContext.UserRoleInClient
                .Where(r => r.UserId == RequestedUser.Id)
                .Where(r => r.ClientId == RequestedClient.Id)
                .ToList();

            try
            {
                DbContext.UserInSelectionGroup.RemoveRange(AllSelectionGroupAssignments);
                DbContext.UserRoleInRootContentItem.RemoveRange(AllRootContentItemAssignments);
                DbContext.UserRoleInClient.RemoveRange(AllClientRoleAssignments);
                DbContext.UserClaims.RemoveRange(UserClaims);

                DbContext.SaveChanges();
            }
            catch (Exception e)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(e, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Failed to remove user {RequestedUser.UserName} from client {RequestedClient.Name}");
                Logger.LogError(ErrMsg);
                Response.Headers.Add("Warning", "Error processing request.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            AuditLogger.Log(AuditEventType.UserRemovedFromClient.ToEvent(RequestedClient, RequestedUser));

            ClientDetailViewModel ReturnModel = new ClientDetailViewModel { ClientEntity = RequestedClient };
            await ReturnModel.GenerateSupportingProperties(DbContext, UserManager, await Queries.GetCurrentApplicationUser(User), RoleEnum.Admin, false);

            return Json(ReturnModel);
        }

        // POST: ClientAdmin/SaveNewClient
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        /// <summary>
        /// Saves a new client object
        /// </summary>
        /// <param name="Model">Type Client</param>
        /// <returns>BadRequestObjectResult, UnauthorizedResult, </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNewClient([Bind("Name,ClientCode,ContactName,ContactTitle,ContactEmail,ContactPhone,ConsultantName,ConsultantEmail," +
                                                 "ConsultantOffice,AcceptedEmailDomainList,ParentClientId,ProfitCenterId,NewUserWelcomeText")] Client Model)
        // Members intentionally not bound: Id, AcceptedEmailAddressExceptionList
        {
            ApplicationUser CurrentApplicationUser = await Queries.GetCurrentApplicationUser(User);

            #region Preliminary Validation
            if (!ModelState.IsValid)
            {
                var firstInvalidKey = ModelState
                    .Keys.First(key => ModelState[key].ValidationState == ModelValidationState.Invalid);
                Response.Headers.Add("Warning", $"{firstInvalidKey}: {ModelState[firstInvalidKey].Errors.First().ErrorMessage}");
                return BadRequest();
            }
            if (Model.ParentClientId == Model.Id)
            {
                return BadRequest("Client cannot have itself as a parent Client");
            }
            #endregion

            #region Authorization
            if (Model.ParentClientId == null)
            {
                // Request to create a root client
                AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInProfitCenterRequirement(RoleEnum.Admin, Model.ProfitCenterId));
                if (!Result1.Succeeded)
                {
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
                    return Unauthorized();
                }
            }
            #endregion Authorization

            #region Validation
            // Convert delimited strings bound from the browser to a proper array
            Model.AcceptedEmailDomainList = GetCleanClientEmailWhitelistArray(Model.AcceptedEmailDomainList, true);
            Model.AcceptedEmailAddressExceptionList = GetCleanClientEmailWhitelistArray(Model.AcceptedEmailAddressExceptionList, false);

            // Valid domain(s) in whitelist
            foreach (string WhiteListedDomain in Model.AcceptedEmailDomainList)
            {
                if (!GlobalFunctions.IsValidEmail("test@" + WhiteListedDomain))
                {
                    Response.Headers.Add("Warning", $"An email domain is invalid: ({WhiteListedDomain})");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }

            // Valid email address(es) in whitelist
            foreach (string WhiteListedAddress in Model.AcceptedEmailAddressExceptionList)
            {
                if (!GlobalFunctions.IsValidEmail(WhiteListedAddress))
                {
                    Response.Headers.Add("Warning", $"An email address is invalid: ({WhiteListedAddress})");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }

            // Parent client must exist if any
            if (Model.ParentClientId != null && !DbContext.ClientExists(Model.ParentClientId.Value))
            {
                Response.Headers.Add("Warning", $"The specified parent Client is invalid: ({Model.ParentClientId.Value})");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Name must be unique
            if (DbContext.Client.Any(c=>c.Name == Model.Name))
            {
                Response.Headers.Add("Warning", $"The client name already exists for another client: ({Model.Name})");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion Validation

            // Make sure current user is allowed by email or domain whitelist
            if (!Queries.DoesEmailSatisfyClientWhitelists(CurrentApplicationUser.Email, Model.AcceptedEmailDomainList, Model.AcceptedEmailAddressExceptionList))
            {
                // TODO can use a simpler expression after the whitelist fields change to List from array. 
                Model.AcceptedEmailAddressExceptionList = Model.AcceptedEmailAddressExceptionList.Append(CurrentApplicationUser.Email).ToArray();
            }

            using (IDbContextTransaction DbTransaction = DbContext.Database.BeginTransaction())
            {
                try
                {
                    // Add the new Client to local context
                    DbContext.Client.Add(Model);
                    DbContext.SaveChanges();

                    DbContext.UserClaims.Add(new IdentityUserClaim<Guid> { UserId = CurrentApplicationUser.Id, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = Model.Id.ToString() });
                    DbContext.SaveChanges();

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
                    DbContext.SaveChanges();

                    DbTransaction.Commit();
                }
                catch (Exception e)
                {
                    string ErrMsg = GlobalFunctions.LoggableExceptionString(e, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Failed to store new client \"{Model.Name}\" to database, or assign client administrator role");
                    Logger.LogError(ErrMsg);
                    Response.Headers.Add("Warning", "Error processing request.");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }

            // Log new client store and ClientAdministrator role authorization events
            AuditLogger.Log(AuditEventType.ClientCreated.ToEvent(Model));
            AuditLogger.Log(AuditEventType.ClientRoleAssigned.ToEvent(Model, CurrentApplicationUser, new List<RoleEnum> { RoleEnum.Admin, RoleEnum.UserCreator }));

            ClientAdminIndexViewModel ModelToReturn = await ClientAdminIndexViewModel.GetClientAdminIndexModelForUser(CurrentApplicationUser, UserManager, DbContext, ApplicationConfig["Global:DefaultNewUserWelcomeText"]);
            ModelToReturn.RelevantClientId = Model.Id;

            return Json(ModelToReturn);
        }

        // POST: ClientAdmin/EditClient
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        /// <summary>
        /// Supports: Edit with no change to parent or ProfitCenter, change of parent if no children
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClient([Bind("Id,Name,ClientCode,ContactName,ContactTitle,ContactEmail,ContactPhone,ConsultantName,ConsultantEmail," +
                                              "ConsultantOffice,AcceptedEmailDomainList,AcceptedEmailAddressExceptionList,ParentClientId,ProfitCenterId,NewUserWelcomeText")] Client Model)
        {
            #region Preliminary Validation
            if (Model.Id == null || Model.Id == Guid.Empty)
            {
                return BadRequest($"Requested Client.Id ({Model.Id}) not valid");
            }

            if (Model.ParentClientId == Model.Id)
            {
                return BadRequest("Client cannot have itself as a parent Client");
            }

            // Query for the existing record to be modified
            Client ExistingClientRecord = DbContext.Client.Find(Model.Id);

            // Client must exist
            if (ExistingClientRecord == null)
            {
                return BadRequest("The modified client was not found in the system.");
            }
            #endregion

            #region Authorization
            // 1) Changing Parent is not supported
            if (Model.ParentClientId != ExistingClientRecord.ParentClientId)
            {
                Response.Headers.Add("Warning", "Client may not be moved to a new parent Client");
                return Unauthorized();
            }

            // 2) User must have ClientAdministrator role for the edited Client
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, Model.Id));
            if (!Result1.Succeeded)
            {
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
                Response.Headers.Add("Warning", ModelState
                    .Values.First(value => value.ValidationState == ModelValidationState.Invalid)
                    .Errors.First().ErrorMessage);
                return BadRequest();
            }

            // Convert delimited strings bound from the browser to a proper array
            Model.AcceptedEmailDomainList = GetCleanClientEmailWhitelistArray(Model.AcceptedEmailDomainList, true);
            Model.AcceptedEmailAddressExceptionList = GetCleanClientEmailWhitelistArray(Model.AcceptedEmailAddressExceptionList, false);
            
            // Valid domains in domain whitelist
            foreach (string WhiteListedDomain in Model.AcceptedEmailDomainList)
            {
                if (!GlobalFunctions.IsValidEmail("test@" + WhiteListedDomain))
                {
                    Response.Headers.Add("Warning", $"The domain is invalid: ({WhiteListedDomain})");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }

            // Valid addresses in address whitelist
            foreach (string WhiteListedAddress in Model.AcceptedEmailAddressExceptionList)
            {
                if (!GlobalFunctions.IsValidEmail(WhiteListedAddress))
                {
                    Response.Headers.Add("Warning", $"The exception address is invalid: ({WhiteListedAddress})");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }
            }

            // Parent client must exist (if any)
            if (Model.ParentClientId != null && !DbContext.ClientExists(Model.ParentClientId.Value))
            {
                Response.Headers.Add("Warning", "The specified parent of the client is invalid.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // ProfitCenter must exist
            if (!DbContext.ProfitCenter.Any(pc => pc.Id == Model.ProfitCenterId))
            {
                Response.Headers.Add("Warning", "The specified ProfitCenter is invalid.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }

            // Name must be unique
            if (DbContext.Client.Any(c => c.Name == Model.Name && 
                                          c.Id != Model.Id))
            {
                Response.Headers.Add("Warning", $"The client name ({Model.Name}) already exists for another client.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion Validation

            // Perform the update
            try
            {
                using (IDbContextTransaction Tx = DbContext.Database.BeginTransaction())
                {
                    // Remove any user for which the email does not match new domain or address whitelist
                    var AllClientMemberUserIds = DbContext.UserClaims
                        .Where(c => c.ClaimType == "ClientMembership")
                        .Where(c => c.ClaimValue == ExistingClientRecord.Id.ToString())
                        .Select(r => r.UserId)
                        .ToList();
                    IQueryable<ApplicationUser> AllClientMemberUsers = DbContext.ApplicationUser.Where(u => AllClientMemberUserIds.Contains(u.Id));
                    foreach (ApplicationUser ClientMemberUser in AllClientMemberUsers)
                    {
                        if (!Queries.DoesEmailSatisfyClientWhitelists(ClientMemberUser.Email, Model.AcceptedEmailDomainList, Model.AcceptedEmailAddressExceptionList))
                        {
                            // Make sure RemoveUserFromClient() doesn't start using a transaction iternally
                            IActionResult result = await RemoveUserFromClient(new ClientUserAssociationViewModel { UserId = ClientMemberUser.Id, ClientId = Model.Id });

                            if (result.GetType() != typeof(JsonResult))
                            {
                                Tx.Rollback();
                                return result;
                            }
                        }
                    }

                    // Must update the instance that's tracked by EF, can't update the context with the untracked Model object
                    ExistingClientRecord.Name = Model.Name;
                    ExistingClientRecord.ClientCode = Model.ClientCode;
                    ExistingClientRecord.ContactName = Model.ContactName;
                    ExistingClientRecord.ContactTitle = Model.ContactTitle;
                    ExistingClientRecord.ContactEmail = Model.ContactEmail;
                    ExistingClientRecord.ContactPhone = Model.ContactPhone;
                    ExistingClientRecord.ConsultantName = Model.ConsultantName;
                    ExistingClientRecord.ConsultantEmail = Model.ConsultantEmail;
                    ExistingClientRecord.ConsultantOffice = Model.ConsultantOffice;
                    ExistingClientRecord.AcceptedEmailDomainList = Model.AcceptedEmailDomainList;
                    ExistingClientRecord.AcceptedEmailAddressExceptionList = Model.AcceptedEmailAddressExceptionList;
                    //Not supported:  ExistingClientRecord.ParentClientId = Model.ParentClientId;
                    ExistingClientRecord.ProfitCenterId = Model.ProfitCenterId;
                    ExistingClientRecord.NewUserWelcomeText = Model.NewUserWelcomeText;

                    DbContext.Client.Update(ExistingClientRecord);
                    DbContext.SaveChanges();
                    Tx.Commit();
                }

                AuditLogger.Log(AuditEventType.ClientEdited.ToEvent(Model));
            }
            catch (Exception ex)
            {
                string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Failed to update client {Model.Id} to database");
                Logger.LogError(ErrMsg);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            ClientAdminIndexViewModel ModelToReturn = await ClientAdminIndexViewModel.GetClientAdminIndexModelForUser(await Queries.GetCurrentApplicationUser(User), UserManager, DbContext, ApplicationConfig["Global:DefaultNewUserWelcomeText"]);
            ModelToReturn.RelevantClientId = ExistingClientRecord.Id;

            return Json(ModelToReturn);
        }

        /// <summary>
        /// Deletes a Client record
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClient(Guid Id, string Password)
        {
            // Query for the existing record to be modified
            Client ExistingClient = DbContext.Client.Find(Id);

            #region Preliminary validation
            if (ExistingClient == null)
            {
                Response.Headers.Add("Warning", "Client not found, unable to delete.");
                return BadRequest();
            }
            #endregion

            #region Authorization
            if (!await UserManager.CheckPasswordAsync(await Queries.GetCurrentApplicationUser(User), Password))
            {
                Response.Headers.Add("Warning", "Incorrect password");
                return Unauthorized();
            }

            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new MapAuthorizationRequirementBase[]
                {
                    new RoleInClientRequirement(RoleEnum.Admin, Id),
                    new RoleInProfitCenterRequirement(RoleEnum.Admin, ExistingClient.ProfitCenterId)
                });
            if (!Result1.Succeeded)
            {
                Response.Headers.Add("Warning", "You are not authorized to delete this client");
                return Unauthorized();
            }
            #endregion Authorization

            #region Validation
            // Client must not be parent of any other Client
            List<string> Children = DbContext.Client.Where(c => c.ParentClientId == Id).Select(c => c.Name).ToList();
            if (Children.Count > 0)
            {
                Response.Headers.Add("Warning", $"Can't delete Client {ExistingClient.Name}. The client has child client(s): {string.Join(", ", Children)}");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);  // 412 is Precondition Failed
            }

            // Client must not have any root content items
            var ItemCount = DbContext.RootContentItem
                .Where(i => i.ClientId == Id)
                .Count();
            if (ItemCount > 0)
            {
                Response.Headers.Add("Warning", $"Can't delete client {ExistingClient.Name} because it has root content items.");
                return StatusCode(StatusCodes.Status422UnprocessableEntity);
            }
            #endregion Validation

            using (IDbContextTransaction DbTransaction = DbContext.Database.BeginTransaction())
            {
                try
                {
                    // Remove all users from the Client
                    List<ApplicationUser> AllClientUsers = DbContext.UserClaims
                        .Where(c => c.ClaimType == "ClientMembership")
                        .Where(c => c.ClaimValue == ExistingClient.Id.ToString())
                        .Join(DbContext.ApplicationUser, c => c.UserId, u => u.Id, (c, u) => u)
                        .ToList();

                    foreach (ApplicationUser user in AllClientUsers)
                    {
                        await RemoveUserFromClient(new ClientUserAssociationViewModel { ClientId = ExistingClient.Id, UserId = user.Id }, true);
                    }

                    // Remove the client
                    DbContext.Client.Remove(ExistingClient);

                    DbContext.SaveChanges();
                    DbTransaction.Commit();
                }
                catch (Exception ex)
                {
                    string ErrMsg = GlobalFunctions.LoggableExceptionString(ex, $"In {this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}(): Failed to delete client from database");
                    Logger.LogError(ErrMsg);
                    Response.Headers.Add("Warning", "Error processing request.");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }

            AuditLogger.Log(AuditEventType.ClientDeleted.ToEvent(ExistingClient));

            ClientAdminIndexViewModel ModelToReturn = await ClientAdminIndexViewModel.GetClientAdminIndexModelForUser(await Queries.GetCurrentApplicationUser(User), UserManager, DbContext, ApplicationConfig["Global:DefaultNewUserWelcomeText"]);

            return Json(ModelToReturn);
        }

        /// <summary>
        /// Returns an array of individual whitelist items without nulls, optionally tested for validity as either domain or full email address
        /// </summary>
        /// <param name="InArray">0 or more strings that may contain 0 or more email entries or a delimited list</param>
        /// <param name="CleanDomain">If true, strip characters up through '@' from each found element</param>
        /// <returns></returns>
        [NonAction]
        private string[] GetCleanClientEmailWhitelistArray(string[] InArray, bool CleanDomain)
        {
            char[] StringDelimiters = new char[] { ',', ';', ' ' };

            string[] Result = new string[0];

            foreach (string Element in InArray)  // Normally from model binding there will be exactly 1
            {
                if (!string.IsNullOrWhiteSpace(Element))  // Model binding passes null when nothing provided
                {
                    foreach (string GoodElement in Element.Split(StringDelimiters, StringSplitOptions.RemoveEmptyEntries))
                    {
                        Result = Result.Append(GoodElement.Trim()).ToArray();
                    }
                }
            }

            if (CleanDomain)
            {
                Result = Result.Select(d => d.Contains("@") ? d.Substring(d.LastIndexOf('@') + 1) : d).ToArray();
            }

            return Result;
        }

    }
}
