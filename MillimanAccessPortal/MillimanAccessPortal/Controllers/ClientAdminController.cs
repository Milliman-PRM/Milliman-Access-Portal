/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing hosted content
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
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Security.Claims;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapCommonLib;
using MillimanAccessPortal.DataQueries;
using AuditLogLib;
using AuditLogLib.Services;
using MillimanAccessPortal.Models.ClientAdminViewModels;
using MillimanAccessPortal.Authorization;

namespace MillimanAccessPortal.Controllers
{
    public class ClientAdminController : Controller
    {
        private readonly static List<RoleEnum> RolesToManage = new List<RoleEnum> { RoleEnum.Admin, RoleEnum.ContentAdmin, RoleEnum.ContentUser, RoleEnum.UserAdmin };

        private readonly ApplicationDbContext DbContext;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly StandardQueries Queries;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ILogger Logger;
        private readonly IAuditLogger AuditLogger;
        private readonly RoleManager<ApplicationRole> RoleManager;

        public ClientAdminController(
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

            ClientAdminIndexViewModel ModelToReturn = await GetClientAdminIndexModelForUser(await Queries.GetCurrentApplicationUser(User));

            return Json(ModelToReturn);
        }

        // GET: ClientAdmin/ClientDetail
        // Intended for access by ajax from Index view
        /// <summary>
        /// Returns the requested Client and lists of eligible and already assigned users associated with a Client. Requires GET. 
        /// </summary>
        /// <param name="id"></param>
        /// <returns>JsonResult or UnauthorizedResult</returns>
        [HttpGet]
        public async Task<IActionResult> ClientDetail(long? id)
        {
            Client ThisClient = DbContext.Client.Include(c => c.ProfitCenter).FirstOrDefault(c => c.Id == id);

            #region Preliminary Validation
            if (ThisClient == null)
            {
                Response.Headers.Add("Warning", $"The requested client was not found");
                return NotFound();
            }
            #endregion

            #region Authorization
            // Check current user's authorization to manage the requested Client
            List<long> AllRelatedClientsList = Queries.GetAllRelatedClients(ThisClient).Select(c => c.Id).ToList();

            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInAnySuppliedClientRequirement(RoleEnum.Admin, AllRelatedClientsList));
            if (!Result1.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized to administer the requested client");
                return Unauthorized();
            }
            #endregion

            ClientDetailViewModel Model = new ClientDetailViewModel();

            Model.ClientEntity = ThisClient;

            Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), ThisClient.Id.ToString());

            // Get the list of users already members of this client
            { // isolate scope
                IList<ApplicationUser> UsersForThisClaim = await UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim);
                Model.AssignedUsers = UsersForThisClaim
                                        .Select(ApUser => (UserInfo)ApUser)  // use the UserInfo type conversion operator
                                        .OrderBy(u => u.LastName)
                                        .ThenBy(u => u.FirstName)
                                        .ToList();
            }

            // Assign the remaining assigned user properties
            foreach (UserInfo UserInfoItem in Model.AssignedUsers)
            {
                UserInfoItem.UserRoles = Queries.GetUserRolesForClient(UserInfoItem.Id, ThisClient.Id);

                // any roles that were not found need to be included with IsAssigned=false
                UserInfoItem.UserRoles.AddRange(RolesToManage.Except(UserInfoItem.UserRoles.Select(ur => ur.RoleEnum)).Select(re =>
                    new AssignedRoleInfo
                    {
                        RoleEnum = re,
                        RoleDisplayValue = ApplicationRole.RoleDisplayNames[re],
                        IsAssigned = false
                    }));

                //List<Client> AuthorizedClients = Queries.GetListOfClientsUserIsAuthorizedToManage(UserManager.GetUserName(HttpContext.User));
            }

            // Get all users currently member of any related Client (any descendant of the root client)
            List<Client> AllRelatedClients = Queries.GetAllRelatedClients(ThisClient);
            List<ApplicationUser> UsersAssignedToClientFamily = new List<ApplicationUser>();
            foreach (Client OneClient in AllRelatedClients)
            {
                ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), OneClient.Id.ToString());
                IList<ApplicationUser> UsersForThisClaim = await UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim);
                UsersAssignedToClientFamily = UsersAssignedToClientFamily.Union(UsersForThisClaim).ToList();
                // TODO Test whether the other overload of .Union() needs to be used with an IEqualityComparer argument.  For this use equality should probably be based on Id only.
            }

            if (ThisClient.AcceptedEmailDomainList != null)
            {
                foreach (string AcceptableDomain in ThisClient.AcceptedEmailDomainList)
                {
                    if (string.IsNullOrWhiteSpace(AcceptableDomain))
                    {
                        continue;
                    }
                    Model.EligibleUsers.AddRange(UsersAssignedToClientFamily.Where(u => u.NormalizedEmail.Contains($"@{AcceptableDomain.ToUpper()}"))
                                                                            .Select(u => (UserInfo)u));
                }
            }

            // Assign the remaining assigned user properties
            foreach (UserInfo UserInfoItem in Model.EligibleUsers)
            {
                UserInfoItem.UserRoles = Queries.GetUserRolesForClient(UserInfoItem.Id, ThisClient.Id);

                // any roles that were not found need to be included with IsAssigned=false
                UserInfoItem.UserRoles.AddRange(RolesToManage.Except(UserInfoItem.UserRoles.Select(ur => ur.RoleEnum)).Select(re => 
                    new AssignedRoleInfo
                    {
                        RoleEnum = re,
                        RoleDisplayValue = ApplicationRole.RoleDisplayNames[re],
                        IsAssigned = false
                    }));
            }

            // Subtract the assigned users from the overall list of eligible users
            Model.EligibleUsers = Model.EligibleUsers
                                       .Except(Model.AssignedUsers, new UserInfoEqualityComparer())
                                       .OrderBy(u => u.LastName)
                                       .ThenBy(u => u.FirstName)
                                       .ToList();

            return Json(Model);
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
                                            .Where(u => u.UserName == Model.UserName)
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
                return StatusCode(StatusCodes.Status412PreconditionFailed);  // 412 is Precondition Failed
            }
            string RequestedUserEmailDomain = RequestedUserEmail.Substring(RequestedUserEmail.IndexOf('@') + 1);
            bool DomainMatch = RequestedClient.AcceptedEmailDomainList != null && 
                               RequestedClient.AcceptedEmailDomainList.Select(d=>d.ToUpper()).Contains(RequestedUserEmailDomain);
            bool EmailMatch = RequestedClient.AcceptedEmailAddressExceptionList != null && 
                              RequestedClient.AcceptedEmailAddressExceptionList.Select(d => d.ToUpper()).Contains(RequestedUserEmail);
            if (!EmailMatch && !DomainMatch)
            {
                Response.Headers.Add("Warning", "The requested user's email is not accepted for this client");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            #endregion

            Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), RequestedClient.Id.ToString());

            IList<Claim> CurrentUserClaims = await UserManager.GetClaimsAsync(RequestedUser);
            if (CurrentUserClaims.Any(claim => claim.Type == ThisClientMembershipClaim.Type && 
                                               claim.Value == ThisClientMembershipClaim.Value))
            {
                Response.Headers.Add("Warning", "The requested user is already assigned to the requested client");
                return await ClientDetail(RequestedClient.Id);
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

                object LogDetails = new { AssignedUserName = RequestedUser.UserName,
                                          AssignedUserId = RequestedUser.Id,
                                          AssignedClient = RequestedClient.Name,
                                          AssignedClientId = RequestedClient.Id};
                AuditLogger.Log(AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "User Assigned to Client", AuditEventId.UserAssignedToClient, LogDetails, User.Identity.Name, HttpContext.Session.Id) );

                return await ClientDetail(RequestedClient.Id);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetUserRoleInClient([Bind("ClientId,UserName")]ClientUserAssociationViewModel ClientUserModel, [Bind("RoleEnum,IsAssigned")]AssignedRoleInfo AssignedRoleInfoArg)
        {
            #region Authorization
            if (!AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, ClientUserModel.ClientId)).Result.Succeeded)
            {
                return Unauthorized();
            }
            #endregion

            #region Validation
            // requested user must exist
            ApplicationUser RequestedUser = await UserManager.FindByNameAsync(ClientUserModel.UserName);
            if (RequestedUser == null)
            {
                Response.Headers.Add("Warning", $"The requested user was not found");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }

            // requested client must exist
            Client RequestedClient = DbContext.Client.Find(ClientUserModel.ClientId);
            if (RequestedClient == null)
            {
                Response.Headers.Add("Warning", $"The requested client was not found");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }

            // Requested user must be member of requested client
            Claim ClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), ClientUserModel.ClientId.ToString());
            if (!UserManager.GetUsersForClaimAsync(ClientMembershipClaim).Result.Contains(RequestedUser))
            {
                Response.Headers.Add("Warning", $"The requested user is not associated with the requested client");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }

            // requested role must exist
            ApplicationRole RequestedRole = RoleManager.FindByIdAsync(((long)AssignedRoleInfoArg.RoleEnum).ToString()).Result;
            if (RequestedRole == null)
            {
                Response.Headers.Add("Warning", $"The requested role was not found");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            #endregion

            IQueryable<UserRoleInClient> ExistingRecordsQuery = DbContext.UserRoleInClient
                                                                         .Where(urc => urc.UserId == RequestedUser.Id
                                                                                    && urc.ClientId == RequestedClient.Id
                                                                                    && urc.RoleId == RequestedRole.Id);

            #region perform the requested action
            List<UserRoleInClient> ExistingRecords = ExistingRecordsQuery.ToList();

            if (AssignedRoleInfoArg.IsAssigned)
            {
                // Create role assignment, only if it's not already there
                if (ExistingRecords.Count == 0)
                {
                    DbContext.UserRoleInClient.Add(new UserRoleInClient { UserId = RequestedUser.Id, RoleId = RequestedRole.Id, ClientId = RequestedClient.Id });
                    DbContext.SaveChanges();
                }
            }
            else
            {
                // Remove role.  There should be only one, but act to remove any number
                DbContext.UserRoleInClient.RemoveRange(ExistingRecords);
                DbContext.SaveChanges();
            }
            #endregion

            #region Build resulting model
            ExistingRecords = ExistingRecordsQuery.ToList();

            List<AssignedRoleInfo> ReturnModel = new List<AssignedRoleInfo>();
            foreach (RoleEnum x in RolesToManage)
            {
                ReturnModel.Add(new AssignedRoleInfo
                {
                    RoleEnum = x,
                    RoleDisplayValue = ApplicationRole.RoleDisplayNames[x],
                    IsAssigned = ExistingRecords.Any(urc => urc.RoleId == (long)x),
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
        public async Task<IActionResult> RemoveUserFromClient(ClientUserAssociationViewModel Model)
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
            ApplicationUser RequestedUser = DbContext.ApplicationUser
                                                     .Where(u => u.UserName == Model.UserName)
                                                     .SingleOrDefault();
            if (RequestedUser == null)
            {
                return BadRequest("The requested user does not exist");
            }

            // 2. RequestedUser must not be assigned to any ContentItemUserGroup of RequestedClient
            //    Deassign groups automatically instead of this?
            IQueryable<ContentItemUserGroup> AllAuthorizedGroupsQuery =
                DbContext.UserInContentItemUserGroup
                         .Include(urc => urc.ContentItemUserGroup)
                         .Where(urc => urc.UserId == RequestedUser.Id)
                         .Select(urc => urc.ContentItemUserGroup);
            if (AllAuthorizedGroupsQuery.Any(group => group.ClientId == RequestedClient.Id))
            {
                Response.Headers.Add("Warning", "The requested user must first be unauthorized to content of the requested client");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            #endregion

            Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), RequestedClient.Id.ToString());

            IList<Claim> ClaimsOfRequestedUser = await UserManager.GetClaimsAsync(RequestedUser);
            if (ClaimsOfRequestedUser.Any(claim => claim.Type == ThisClientMembershipClaim.Type &&
                                                   claim.Value == ThisClientMembershipClaim.Value))
            {
                IdentityResult ResultOfRemoveClaim = await UserManager.RemoveClaimAsync(RequestedUser, ThisClientMembershipClaim);
                if (ResultOfRemoveClaim != IdentityResult.Success)
                {
                    string ErrMsg = $"Failed to remove user {RequestedUser.UserName}: Claim={ThisClientMembershipClaim.Type}.{ThisClientMembershipClaim.Value}";
                    Logger.LogError(ErrMsg);
                    return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
                }

                object LogDetails = new
                {
                    AssignedUserName = RequestedUser.UserName,
                    AssignedUserId = RequestedUser.Id,
                    AssignedClient = RequestedClient.Name,
                    AssignedClientId = RequestedClient.Id
                };
                AuditLogger.Log(AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                                            "User removed from Client", 
                                            AuditEventId.UserRemovedFromClient, 
                                            LogDetails, 
                                            User.Identity.Name, 
                                            HttpContext.Session.Id));

                return await ClientDetail(RequestedClient.Id);
            }
            else
            {
                Response.Headers.Add("Warning", $"User {RequestedUser.UserName} is not assigned to client {RequestedClient.Name}.  No action taken.");
                return await ClientDetail(RequestedClient.Id);
            }
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
                                                 "ConsultantOffice,AcceptedEmailDomainList,ParentClientId,ProfitCenterId")] Client Model)
        // Members intentionally not bound: Id, AcceptedEmailAddressExceptionList
        {
            ApplicationUser CurrentApplicationUser = await Queries.GetCurrentApplicationUser(User);

            #region Preliminary Validation
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid Model");
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
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }
            }

            // Valid email address(es) in whitelist
            foreach (string WhiteListedAddress in Model.AcceptedEmailAddressExceptionList)
            {
                if (!GlobalFunctions.IsValidEmail(WhiteListedAddress))
                {
                    Response.Headers.Add("Warning", $"An email address is invalid: ({WhiteListedAddress})");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }
            }

            // Parent client must exist if any
            if (Model.ParentClientId != null && !DbContext.ClientExists(Model.ParentClientId.Value))
            {
                Response.Headers.Add("Warning", $"The specified parent Client is invalid: ({Model.ParentClientId.Value})");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }

            // Name must be unique
            if (DbContext.Client.Any(c=>c.Name == Model.Name))
            {
                Response.Headers.Add("Warning", $"The client name already exists for another client: ({Model.Name})");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            #endregion Validation

            try
            {
                // Add the new Client to local context
                DbContext.Client.Add(Model);

                // Add current user's role as ClientAdministrator of new Client to local context
                DbContext.UserRoleInClient.Add(new UserRoleInClient
                    {
                        Client = Model,
                        Role = await RoleManager.FindByNameAsync(RoleEnum.Admin.ToString()),
                        UserId = CurrentApplicationUser.Id
                    });

                // Store to database
                DbContext.SaveChanges();

                await UserManager.AddClaimAsync(CurrentApplicationUser, new Claim(ClaimNames.ClientMembership.ToString(), Model.Id.ToString()));

                // Log new client store and ClientAdministrator role authorization events
                object LogDetails = new { ClientId = Model.Id, ClientName = Model.Name, };
                AuditLogger.Log(AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "New Client Saved", AuditEventId.NewClientSaved, LogDetails, User.Identity.Name, HttpContext.Session.Id));

                LogDetails = new { ClientId = Model.Id, ClientName = Model.Name, User = User.Identity.Name, Role = RoleEnum.Admin.ToString() };
                AuditLogger.Log(AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Client Administrator role assigned", AuditEventId.ClientRoleAssigned, LogDetails, User.Identity.Name, HttpContext.Session.Id));
            }
            catch (Exception e)
            {
                string ErrMsg = $"Failed to store new client \"{Model.Name}\" to database, or assign client administrator role";
                while (e != null)
                {
                    ErrMsg += $"\r\n{e.Message}";
                    e = e.InnerException;
                }
                Logger.LogError(ErrMsg);
                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
            }

            ClientAdminIndexViewModel ModelToReturn = await GetClientAdminIndexModelForUser(CurrentApplicationUser);
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
                                              "ConsultantOffice,AcceptedEmailDomainList,AcceptedEmailAddressExceptionList,ParentClientId,ProfitCenterId")] Client Model)
        {
            #region Preliminary Validation
            if (Model.Id <= 0)
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
            // Convert delimited strings bound from the browser to a proper array
            Model.AcceptedEmailDomainList = GetCleanClientEmailWhitelistArray(Model.AcceptedEmailDomainList, true);
            Model.AcceptedEmailAddressExceptionList = GetCleanClientEmailWhitelistArray(Model.AcceptedEmailAddressExceptionList, false);
            
            // Valid domains in domain whitelist
            foreach (string WhiteListedDomain in Model.AcceptedEmailDomainList)
            {
                if (!GlobalFunctions.IsValidEmail("test@" + WhiteListedDomain))
                {
                    Response.Headers.Add("Warning", $"The domain is invalid: ({WhiteListedDomain})");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }
            }

            // Valid addresses in address whitelist
            foreach (string WhiteListedAddress in Model.AcceptedEmailAddressExceptionList)
            {
                if (!GlobalFunctions.IsValidEmail(WhiteListedAddress))
                {
                    Response.Headers.Add("Warning", $"The exception address is invalid: ({WhiteListedAddress})");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }
            }

            // Parent client must exist (if any)
            if (Model.ParentClientId != null && !DbContext.ClientExists(Model.ParentClientId.Value))
            {
                Response.Headers.Add("Warning", "The specified parent of the client is invalid.");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }

            // ProfitCenter must exist
            if (!DbContext.ProfitCenter.Any(pc => pc.Id == Model.ProfitCenterId))
            {
                Response.Headers.Add("Warning", "The specified ProfitCenter is invalid.");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }

            // Name must be unique
            if (DbContext.Client.Any(c => c.Name == Model.Name && 
                                          c.Id != Model.Id))
            {
                Response.Headers.Add("Warning", $"The client name ({Model.Name}) already exists for another client.");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            #endregion Validation

            // Perform the update
            try
            {
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

                DbContext.Client.Update(ExistingClientRecord);
                DbContext.SaveChanges();

                object LogDetails = new { ClientId = Model.Id, ClientName = Model.Name, };
                AuditLogger.Log(AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Client Edited", AuditEventId.ClientEdited, LogDetails, User.Identity.Name, HttpContext.Session.Id));
            }
            catch (Exception ex)
            {
                string ErrMsg = $"Failed to update client {Model.Id} to database";
                Logger.LogError(ErrMsg + $":\r\n{ ex.Message}\r\n{ ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
            }

            ClientAdminIndexViewModel ModelToReturn = await GetClientAdminIndexModelForUser(await Queries.GetCurrentApplicationUser(User));
            ModelToReturn.RelevantClientId = ExistingClientRecord.Id;

            return Json(ModelToReturn);
        }

        // DELETE: ClientAdmin/Delete/5
        //public async Task<IActionResult> DeleteClient(long Id)
        [HttpDelete]
        public async Task<IActionResult> DeleteClient(long? Id, string Password)
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
                    new RoleInClientRequirement(RoleEnum.Admin, Id.Value),
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
            List<string> Children = DbContext.Client.Where(c => c.ParentClientId == Id.Value).Select(c => c.Name).ToList();
            if (Children.Count > 0)
            {
                Response.Headers.Add("Warning", $"Can't delete Client {ExistingClient.Name}. The client has child client(s): {string.Join(", ", Children)}");
                return StatusCode(StatusCodes.Status412PreconditionFailed);  // 412 is Precondition Failed
            }
            #endregion Validation

            try
            {
                // Only the primary key is needed for delete
                DbContext.Client.Remove(ExistingClient);
                DbContext.SaveChanges();

                object LogDetails = new { ClientId = Id.Value };
                AuditLogger.Log(AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Client Deleted", AuditEventId.ClientDeleted, LogDetails, User.Identity.Name, HttpContext.Session.Id));
            }
            catch (Exception ex)
            {
                string ErrMsg = $"Failed to delete client from database";
                Logger.LogError(ErrMsg + $":\r\n{ ex.Message}\r\n{ ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
            }

            ClientAdminIndexViewModel ModelToReturn = await GetClientAdminIndexModelForUser(await Queries.GetCurrentApplicationUser(User));

            return Json(ModelToReturn);
        }

        /// <summary>
        /// Create and return the 2 lists: 1-Clients and 2-ProfitCenters associated with the provided ApplicationUser
        /// </summary>
        /// <param name="CurrentUser">Must be populated with Id.  Best if returned from EF query</param>
        /// <returns></returns>
        [NonAction]
        private async Task<ClientAdminIndexViewModel> GetClientAdminIndexModelForUser(ApplicationUser CurrentUser)
        {
            #region Validation
            if (CurrentUser == null)
            {
                return null;
            }
            #endregion

            // Instantiate working variables
            ClientAdminIndexViewModel ModelToReturn = new ClientAdminIndexViewModel();

            // Add all appropriate client trees
            List<Client> AllRootClients = Queries.GetAllRootClients();  // list to memory so utilization is fast and no lingering transaction
            foreach (Client RootClient in AllRootClients.OrderBy(c => c.Name))
            {
                ClientAndChildrenModel ClientModel = await Queries.GetDescendentFamilyOfClient(RootClient, CurrentUser, RoleEnum.Admin, true, true);
                if (ClientModel.IsThisOrAnyChildManageable())
                {
                    ModelToReturn.ClientTree.Add(ClientModel);
                }
            }

            // Add all authorized ProfitCenters
            // Iterate over all ProfitCenterManager authorizations for the current user
            foreach (var AuthorizedProfitCenter in DbContext.UserRoleInProfitCenter
                                                            .Include(urpc => urpc.Role)
                                                            .Include(urpc => urpc.ProfitCenter)
                                                            .Where(urpc => urpc.Role.RoleEnum == RoleEnum.Admin
                                                                        && urpc.UserId == CurrentUser.Id)
                                                            .Distinct()
                                                            .Select(urpc => urpc.ProfitCenter))
            {
                ModelToReturn.AuthorizedProfitCenterList.Add(new AuthorizedProfitCenterModel(AuthorizedProfitCenter));
            }

            return ModelToReturn;
        }

        /// <summary>
        /// Returns a clean array without null elements, optionally tested for validity as either domain or full email address
        /// </summary>
        /// <param name="InArray"></param>
        /// <param name="CleanDomain">If true, strip characters up through '@' from each found element</param>
        /// <returns></returns>
        private string[] GetCleanClientEmailWhitelistArray(string[] InArray, bool CleanDomain)
        {
            char[] StringDelimiters = new char[] { ',', ';', ' ' };

            string[] Result = new string[0];

            foreach (string Element in InArray)  // Normally from model binding there will be exactly 1
            {
                if (!string.IsNullOrWhiteSpace(Element))  // Model binding passes null when nothing provided
                {
                    foreach (string GoodElement in InArray[0].Split(StringDelimiters, StringSplitOptions.RemoveEmptyEntries))
                    {
                        Result = Result.Append(GoodElement).ToArray();
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
