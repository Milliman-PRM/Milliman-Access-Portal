using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapCommonLib;
using AuditLogLib;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Security.Claims;
using MillimanAccessPortal.Models.ClientAdminViewModels;
using MillimanAccessPortal.Authorization;

namespace MillimanAccessPortal.Controllers
{
    public class ClientAdminController : Controller
    {
        private readonly ApplicationDbContext DbContext;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly IServiceProvider ServiceProvider;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ILogger Logger;

        public ClientAdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> UserManagerArg,
            IServiceProvider ServiceProviderArg,
            IAuthorizationService AuthorizationServiceArg,
            ILoggerFactory LoggerFactoryArg
            )
        {
            DbContext = context;
            UserManager = UserManagerArg;
            ServiceProvider = ServiceProviderArg;
            AuthorizationService = AuthorizationServiceArg;
            Logger = LoggerFactoryArg.CreateLogger<AccountController>();
        }

        // GET: ClientAdmin
        /// <summary>
        /// Action leading to the main landing page for Client administration UI
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        // GET: ClientAdmin/ClientFamilyList
        // Intended for access by ajax from Index view
        [HttpGet]
        public IActionResult ClientFamilyList()
        {
            if (!AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement { RoleEnum = RoleEnum.ClientAdministrator }).Result)
            {
                return Unauthorized();
            }

            long CurrentUserId = GetCurrentUser().Id;

            List<ClientAndChildrenViewModel> ModelToReturn = new List<ClientAndChildrenViewModel>();

            List<Client> AllRootClients = new StandardQueries(ServiceProvider).GetAllRootClients();
            foreach (Client C in AllRootClients.OrderBy(c=>c.Name))
            {
                ClientAndChildrenViewModel ClientModel = new StandardQueries(ServiceProvider).GetDescendentFamilyOfClient(C, CurrentUserId, true);
                if (ClientModel.IsThisOrAnyChildManageable())
                {
                    ModelToReturn.Add(ClientModel);
                }
            }

            return Json(ModelToReturn);
        }

        // GET: ClientAdmin/ClientUserLists
        // Intended for access by ajax from Index view
        [HttpGet]
        public IActionResult ClientUserLists(long? id)
        {
            Client ThisClient = DbContext.Client.SingleOrDefaultAsync(m => m.Id == id).Result;
            if (ThisClient == null)
            {
                return NotFound();
            }

            // Check current user's authorization to manage the requested Client
            if (!AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement { RoleEnum = RoleEnum.ClientAdministrator, ClientId = ThisClient.Id }).Result)
            {
                return Unauthorized();
                // or:
                // return NotFound();
            }

            ClientUserListsViewModel Model = new ClientUserListsViewModel();

            Claim ThisClientMembershipClaim = new Claim("ClientMembership", ThisClient.Name);

            // Get the list of users already assigned to this client
            Model.AssignedUsers = UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim)
                                             .Result  // because the preceding call is async
                                             .Select(ApUser => (UserInfo)ApUser)
                                             .OrderBy(u => u.LastName)
                                             .ThenBy(u => u.FirstName)
                                             .ToList();
            // Assign the remaining assigned user properties
            foreach (UserInfo Item in Model.AssignedUsers)
            {
                Item.UserRoles = new StandardQueries(ServiceProvider).GetUserRolesForClient(Item.Id, ThisClient.Id);
            }

            // Get all users currently member of any related Client (any descendant of the root client)
            List<Client> AllRelatedClients = new StandardQueries(ServiceProvider).GetAllRelatedClients(ThisClient);
            List<ApplicationUser> UsersAssignedToClientFamily = new List<ApplicationUser>();
            foreach (Client OneClient in AllRelatedClients)
            {
                ThisClientMembershipClaim = new Claim("ClientMembership", OneClient.Name);
                UsersAssignedToClientFamily = UsersAssignedToClientFamily.Union(UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim).Result).ToList();
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
            foreach (UserInfo Item in Model.EligibleUsers)
            {
                Item.UserRoles = new StandardQueries(ServiceProvider).GetUserRolesForClient(Item.Id, ThisClient.Id);
            }

            // Subtract the assigned users from the overall list of eligible users
            Model.EligibleUsers = Model.EligibleUsers
                                       .Except(Model.AssignedUsers, new UserInfoEqualityComparer())
                                       .OrderBy(u => u.LastName)
                                       .ThenBy(u => u.FirstName)
                                       .ToList();

            return Json(Model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignUserToClient(ClientUserAssociationViewModel Model)
        {
            // Authorization check
            if (!AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement { RoleEnum = RoleEnum.ClientAdministrator, ClientId = Model.ClientId }).Result)
            {
                return Unauthorized();
            }

            #region Validate the request
            // 1. Requested client must exist
            Client RequestedClient = DbContext
                                     .Client
                                     .Where(c => c.Id == Model.ClientId)
                                     .SingleOrDefault();
            if (RequestedClient == null)
            {
                return BadRequest("The requested client does not exist");
            }

            // 2. Requested user must exist
            ApplicationUser RequestedUser = DbContext
                                            .ApplicationUser
                                            .Where(u => u.UserName == Model.UserName)
                                            .SingleOrDefault();
            if (RequestedUser == null)
            {
                return BadRequest("The requested user does not exist");
            }

            // 3. Requested User's email must comply with client email whitelist
            string RequestedUserEmail = RequestedUser.NormalizedEmail.ToUpper();
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

            Claim ThisClientMembershipClaim = new Claim("ClientMembership", RequestedClient.Name);

            if (UserManager.GetClaimsAsync(RequestedUser).Result.Any(claim => claim.Type == ThisClientMembershipClaim.Type && 
                                                                              claim.Value == ThisClientMembershipClaim.Value))
            {
                return Ok("The requested user is already assigned to the requested client");
            }
            else
            {
                IdentityResult ResultOfAddClaim = UserManager.AddClaimAsync(RequestedUser, ThisClientMembershipClaim).Result;
                if (ResultOfAddClaim != IdentityResult.Success)
                {
                    string ErrMsg = $"Failed to add claim for user {RequestedUser.UserName}: Claim={ThisClientMembershipClaim.Type}.{ThisClientMembershipClaim.Value}";
                    Logger.LogError(ErrMsg);
                    return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
                }

                AuditLogger AuditLog = new AuditLogger();
                object LogDetails = new { AssignedUserName = RequestedUser.UserName,
                                          AssignedUserId = RequestedUser.Id,
                                          AssignedClient = RequestedClient.Name,
                                          AssignedClientId = RequestedClient.Id};
                AuditLog.Log(AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "User Assigned to Client", AuditEventId.UserAssignedToClient, LogDetails, User.Identity.Name, HttpContext.Session.Id) );
                return Ok();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveUserFromClient(ClientUserAssociationViewModel Model)
        {
            // Authorization check
            if (!AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement { RoleEnum = RoleEnum.ClientAdministrator, ClientId = Model.ClientId }).Result)
            {
                return Unauthorized();
            }

            #region Validate the request
            // 1. Requested client must exist
            Client RequestedClient = DbContext
                                     .Client
                                     .Where(c => c.Id == Model.ClientId)
                                     .SingleOrDefault();
            if (RequestedClient == null)
            {
                return BadRequest("The requested client does not exist");
            }

            // 2. Requested user must exist
            ApplicationUser RequestedUser = DbContext.ApplicationUser
                                                     .Where(u => u.UserName == Model.UserName)
                                                     .SingleOrDefault();
            if (RequestedUser == null)
            {
                return BadRequest("The requested user does not exist");
            }

            // 3. RequestedUser must not be assigned to any ContentItemUserGroup of RequestedClient
            //    Deassign groups automatically instead of this?
            IQueryable<ContentItemUserGroup> AllAuthorizedGroupsQuery =
                DbContext.UserRoleForContentItemUserGroup
                         .Include(urc => urc.ContentItemUserGroup)
                         .Where(urc => urc.UserId == RequestedUser.Id)
                         .Select(urc => urc.ContentItemUserGroup);
            if (AllAuthorizedGroupsQuery.Any(group => group.ClientId == RequestedClient.Id))
            {
                Response.Headers.Add("Warning", "The requested user must first be unauthorized to content of the requested client");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            #endregion

            Claim ThisClientMembershipClaim = new Claim("ClientMembership", RequestedClient.Name);

            if (UserManager.GetClaimsAsync(RequestedUser).Result.Any(claim => claim.Type == ThisClientMembershipClaim.Type &&
                                                                              claim.Value == ThisClientMembershipClaim.Value))
            {
                IdentityResult ResultOfRemoveClaim = UserManager.RemoveClaimAsync(RequestedUser, ThisClientMembershipClaim).Result;
                if (ResultOfRemoveClaim != IdentityResult.Success)
                {
                    string ErrMsg = $"Failed to remove user {RequestedUser.UserName}: Claim={ThisClientMembershipClaim.Type}.{ThisClientMembershipClaim.Value}";
                    Logger.LogError(ErrMsg);
                    return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
                }

                AuditLogger AuditLog = new AuditLogger();
                object LogDetails = new
                {
                    AssignedUserName = RequestedUser.UserName,
                    AssignedUserId = RequestedUser.Id,
                    AssignedClient = RequestedClient.Name,
                    AssignedClientId = RequestedClient.Id
                };
                AuditLog.Log(AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}",
                                            "User removed from Client", 
                                            AuditEventId.UserRemovedFromClient, 
                                            LogDetails, 
                                            User.Identity.Name, 
                                            HttpContext.Session.Id));
                return Ok();
            }
            else
            {
                return Ok("The requested user is not assigned to the requested client.  No action taken.");
            }
        }

        // POST: ClientAdmin/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(long id, [Bind("Id,Name,AcceptedEmailDomainList,ParentClientId")] Client client)
        public IActionResult SaveNewClient([Bind("Name,ClientCode,ContactName,ContactTitle,ContactEmail,ContactPhone,ConsultantName,ConsultantEmail," +
                                                 "ConsultantOffice,ProfitCenter,AcceptedEmailDomainList,ParentClientId")] Client Model)
        // Members not bound: Id,AcceptedEmailAddressExceptionList
        {
            #region Authorization
            if (!AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement { RoleEnum = RoleEnum.ClientAdministrator}).Result)
            {
                return Unauthorized();
            }

            if (Model.ParentClientId != null && !AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement { RoleEnum = RoleEnum.ClientAdministrator, ClientId = Model.ParentClientId.Value }).Result)
            {
                return Unauthorized();
            }
            #endregion Authorization

            #region Validation
            // Valid domains in whitelist
            foreach (string WhiteListedDomain in Model.AcceptedEmailDomainList)
            {
                if (!GlobalFunctions.IsValidEmail("test@" + WhiteListedDomain))
                {
                    Response.Headers.Add("Warning", $"The domain is invalid: ({WhiteListedDomain})");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);  // 412 is Precondition Failed
                }
            }

            // Parent client must exist if any
            if (Model.ParentClientId != null && !ClientExists(Model.ParentClientId.Value))
            {
                Response.Headers.Add("Warning", $"The specified parent Client is invalid: ({Model.ParentClientId.Value})");
                return StatusCode(StatusCodes.Status412PreconditionFailed);  // 412 is Precondition Failed
            }

            // Name must be unique
            if (DbContext.Client.Any(c=>c.Name == Model.Name))
            {
                Response.Headers.Add("Warning", $"The client name already exists for another client: ({Model.Name})");
                return StatusCode(StatusCodes.Status412PreconditionFailed);  // 412 is Precondition Failed
            }
            #endregion Validation

            try
            {
                DbContext.Client.Add(Model);
                DbContext.SaveChanges();
            }
            catch
            {
                string ErrMsg = $"Failed to store validated new Client to database";
                Logger.LogError(ErrMsg);
                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
            }

            return Ok();
        }

        // POST: ClientAdmin/Edit
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditClient([Bind("Id,Name,ClientCode,ContactName,ContactTitle,ContactEmail,ContactPhone,ConsultantName,ConsultantEmail," +
                                              "ConsultantOffice,ProfitCenter,AcceptedEmailDomainList,AcceptedEmailAddressExceptionList,ParentClientId")] Client Model)
        //public async Task<IActionResult> EditClient([Bind("Id,Name,ClientCode,ContactName,ContactTitle,ContactEmail,ContactPhone,ConsultantName,ConsultantEmail," +
        //                                                  "ConsultantOffice,ProfitCenter,AcceptedEmailDomainList,AcceptedEmailAddressExceptionList,ParentClientId")] Client Model)
        {
            if (Model.Id <= 0)
            {
                return BadRequest();
            }

            #region Authorization
            if (!AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement { RoleEnum = RoleEnum.ClientAdministrator, ClientId = Model.Id }).Result)
            {
                return Unauthorized();
            }
            #endregion Authorization

            #region Validation
            // Valid domains in whitelist
            foreach (string WhiteListedDomain in Model.AcceptedEmailDomainList)
            {
                if (!GlobalFunctions.IsValidEmail("test@" + WhiteListedDomain))
                {
                    Response.Headers.Add("Warning", $"The domain is invalid: ({WhiteListedDomain})");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);  // 412 is Precondition Failed
                }
            }

            // Valid addresses in whitelist
            foreach (string WhiteListedAddress in Model.AcceptedEmailAddressExceptionList)
            {
                if (!GlobalFunctions.IsValidEmail(WhiteListedAddress))
                {
                    Response.Headers.Add("Warning", $"The exception address is invalid: ({WhiteListedAddress})");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);  // 412 is Precondition Failed
                }
            }

            // Parent client must exist if any
            if (Model.ParentClientId != null && !ClientExists(Model.ParentClientId.Value))
            {
                Response.Headers.Add("Warning", $"The specified parent Client is invalid: ({Model.ParentClientId.Value})");
                return StatusCode(StatusCodes.Status412PreconditionFailed);  // 412 is Precondition Failed
            }

            // Name must be unique
            if (DbContext.Client.Any(c => c.Name == Model.Name && 
                                          c.Id != Model.Id))
            {
                Response.Headers.Add("Warning", $"The client name ({Model.Name}) already exists for another client");
                return StatusCode(StatusCodes.Status412PreconditionFailed);  // 412 is Precondition Failed
            }
            #endregion Validation

            try
            {
                DbContext.Client.Update(Model);
                DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                string ErrMsg = $"Failed to update client to database";
                Logger.LogError(ErrMsg + $":\r\n{ ex.Message}\r\n{ ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
            }

            return Ok();
        }

        // GET: ClientAdmin/Delete/5
        //public async Task<IActionResult> DeleteClient(long Id)
        public IActionResult DeleteClient(long? Id)
        {
            if (Id == null || Id.Value <=0)
            {
                return BadRequest();
            }

            #region Authorization
            if (!AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement { RoleEnum = RoleEnum.ClientAdministrator, ClientId = Id.Value }).Result)
            {
                return Unauthorized();
            }
            #endregion Authorization

            #region Validation
            // Client must not be parent of any other Client // TODO consider what would be an acceptable way of handling this automatically
            // Name must be unique
            List<long> Children = DbContext.Client.Where(c => c.ParentClientId == Id.Value).Select(c => c.Id).ToList();
            if (Children.Count > 0)
            {
                Response.Headers.Add("Warning", $"The client is the parent of client(s): {string.Join(", ", Children)}");
                return StatusCode(StatusCodes.Status412PreconditionFailed);  // 412 is Precondition Failed
            }
            #endregion Validation

            try
            {
                // Only the primary key is needed for delete
                DbContext.Client.Remove(new Client { Id = Id.Value });
                DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                string ErrMsg = $"Failed to delete client from database";
                Logger.LogError(ErrMsg + $":\r\n{ ex.Message}\r\n{ ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
            }

            return Ok();
        }

        [NonAction]
        private bool ClientExists(long id)
        {
            return DbContext.Client.Any(e => e.Id == id);
        }

        [NonAction]
        private ApplicationUser GetCurrentUser()
        {
            return UserManager.GetUserAsync(HttpContext.User).Result;
        }
    }
}
