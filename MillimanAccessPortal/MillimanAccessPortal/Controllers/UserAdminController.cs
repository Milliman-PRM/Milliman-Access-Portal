/*
 * CODE OWNERS: Ben Wyatt, Tom Puckett
 * OBJECTIVE: Provide all application logic for user administration
 * DEVELOPER NOTES: 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using AuditLogLib;
using AuditLogLib.Services;
using MapCommonLib;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.UserAdminViewModels;
using MillimanAccessPortal.Models.ClientAdminViewModels;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Controllers
{
    public class UserAdminController : Controller
    {
        private readonly ApplicationDbContext DbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly MessageQueueServices MessageQueueService;
        private readonly ILogger _logger;
        private readonly StandardQueries Queries;

        public UserAdminController(
            UserManager<ApplicationUser> userManager,
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            MessageQueueServices MessageQueueServiceArg,
            ILoggerFactory LoggerFactoryArg,
            ApplicationDbContext DbContextArg,
            StandardQueries QueriesArg
            )
        {
            _userManager = userManager;
            _auditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            MessageQueueService = MessageQueueServiceArg;
            _logger = LoggerFactoryArg.CreateLogger<UserAdminController>();
            DbContext = DbContextArg;
            Queries = QueriesArg;
        }

        // GET: UserAdmin
        public async Task<ActionResult> Index()
        {
            Task<AuthorizationResult> Task1 = AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.UserAdmin, null));
            Task<AuthorizationResult> Task2 = AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.UserAdmin, null));
            AuthorizationResult Result1 = await Task1;
            AuthorizationResult Result2 = await Task2;
            if (!Result1.Succeeded && !Result2.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized as a user admin.");
                return Unauthorized();
            }

            List<ApplicationUser> AllUsers = _userManager.Users.ToList();
            List<ApplicationUserViewModel> Model = new List<ApplicationUserViewModel>();
            foreach (ApplicationUser U in AllUsers)
            {
                Model.Add(await ApplicationUserViewModel.New(U, _userManager));
            }

            return View(Model);
        }

        // GET: UserAdmin/ClientFamilyList
        /// <summary>
        /// Returns the list of Client families that the current user has visibility to (defined by GetUserAdminClientListModel(...)
        /// </summary>
        /// <returns>JsonResult or UnauthorizedResult</returns>
        [HttpGet]
        public async Task<ActionResult> ClientFamilyList()
        {
            #region Authorization
            // User must have UserAdmin role to at least 1 Client
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.UserAdmin, null));
            if (!Result1.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized as a user admin");
                return Unauthorized();
            }
            #endregion

            UserAdminClientFamilyListViewModel Model = await GetUserAdminClientListModel(await Queries.GetCurrentApplicationUser(User));

            return Json(Model);
        }

        /// <summary>
        /// A utility method to provide a model for the client list in the application's UserAdmin page
        /// </summary>
        /// <param name="CurrentUser"></param>
        /// <returns></returns>
        [NonAction]  // maybe move this elsewhere, (e.g. the model class itself?)
        private async Task<UserAdminClientFamilyListViewModel> GetUserAdminClientListModel(ApplicationUser CurrentUser)
        {
            #region Validation
            if (CurrentUser == null)
            {
                return null;
            }
            #endregion            
            
            // Instantiate working variables
            UserAdminClientFamilyListViewModel ModelToReturn = new UserAdminClientFamilyListViewModel();

            // Add all appropriate client trees
            List<Client> AllRootClients = Queries.GetAllRootClients();  // list to memory so utilization is fast and no lingering transaction
            foreach (Client RootClient in AllRootClients.OrderBy(c => c.Name))
            {
                ClientAndChildrenModel ClientModel = new ClientAndChildrenModel(RootClient);
                await ClientModel.GenerateSupportingProperties(DbContext, _userManager, await Queries.GetCurrentApplicationUser(User), RoleEnum.Admin, false);
                //ClientAndChildrenModel ClientModel = await Queries.GetDescendentFamilyOfClient(RootClient, CurrentUser, RoleEnum.UserAdmin, false, true);
                if (ClientModel.IsThisOrAnyChildManageable())
                {
                    ModelToReturn.ClientTree.Add(ClientModel);
                }
            }

            return ModelToReturn;
        }


        
        // GET: UserAdmin/Details/5
        public async Task<ActionResult> Details(string id)
        {
            ApplicationUser RequestedUser = await _userManager.FindByIdAsync(id);

            ViewData["isSystemAdmin"] = await _userManager.IsInRoleAsync(RequestedUser, RoleEnum.Admin.ToString());
            return View(RequestedUser);
        }

        // GET: UserAdmin/Create
        public ActionResult Create()
        {
            // NEXT TODO return a ViewModel with the list of clients the current user is authorized to admin
            return View();
        }

        // POST: UserAdmin/SaveNewUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task <ActionResult> SaveNewUser([Bind("UserName,Email,FirstName,LastName,PhoneNumber,Employer,MemberOfClientIdArray")]ApplicationUserViewModel Model)
        {
            #region If user already exists get the record
            ApplicationUser RequestedUser = DbContext.ApplicationUser
                                                        .FirstOrDefault(u => u.UserName == Model.UserName 
                                                                        || u.Email == Model.Email);
            #endregion

            #region Authorization
            // If creating a new user, current user must either have global UserCreator role or UserCreator role for each requested client
            if (RequestedUser == null)
            {
                if (Model.MemberOfClientIdArray.Length == 0)
                {
                    AuthorizationResult GlobalUserCreatorResult = await AuthorizationService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.UserCreator));
                    if (!GlobalUserCreatorResult.Succeeded)
                    {
                        var AssignedClientDetailObject = new { RequestedUser = Model.UserName, RequiredRole = RoleEnum.UserCreator.ToString(), RequestedClientIds = string.Join(",", Model.MemberOfClientIdArray) };
                        AuditEvent AuthorizationFailedEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Request to create user without required role", AuditEventId.Unauthorized, AssignedClientDetailObject, User.Identity.Name, HttpContext.Session.Id);
                        _auditLogger.Log(AuthorizationFailedEvent);

                        Response.Headers.Add("Warning", "You are not authorized to create a user");
                        return Unauthorized();
                    }
                }
                else
                {
                    foreach (var RequestedClientId in Model.MemberOfClientIdArray)
                    {
                        AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.UserCreator, RequestedClientId));
                        if (!Result1.Succeeded)
                        {
                            var AssignedClientDetailObject = new { RequestedUser = Model.UserName, RequiredRole = RoleEnum.UserCreator.ToString(), RequestedClientIds = string.Join(",", Model.MemberOfClientIdArray), ClientNotAuthorized = RequestedClientId };
                            AuditEvent AuthorizationFailedEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Request to create user for specific client without required role", AuditEventId.Unauthorized, AssignedClientDetailObject, User.Identity.Name, HttpContext.Session.Id);
                            _auditLogger.Log(AuthorizationFailedEvent);

                            Response.Headers.Add("Warning", "You are not authorized to create a user for a requested client");
                            return Unauthorized();
                        }
                    }
                }
            }

            // If 1+ client assignment is requested, user must be admin for the requested client
            foreach (var RequestedClientId in Model.MemberOfClientIdArray)
            {
                AuthorizationResult Result2 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.Admin, RequestedClientId));
                if (!Result2.Succeeded)
                {
                    var AssignedClientDetailObject = new { RequestedUser = Model.UserName, RequiredRole = RoleEnum.Admin.ToString(), RequestedClientIds = string.Join(",", Model.MemberOfClientIdArray), ClientNotAuthorized = RequestedClientId };

                    AuditEvent AuthorizationFailedEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Request to associate a user with unauthorized client(s)", AuditEventId.Unauthorized, AssignedClientDetailObject, User.Identity.Name, HttpContext.Session.Id);
                    Response.Headers.Add("Warning", $"You are not authorized to assign a user to the requested client(s) ({AssignedClientDetailObject.RequestedClientIds})");
                    _auditLogger.Log(AuthorizationFailedEvent);

                    return Unauthorized();
                }
            }
            #endregion Authorization

            #region Validation
            // 1. Email must be a valid address
            if (!GlobalFunctions.IsValidEmail(Model.Email))
            {
                Response.Headers.Add("MapReason", "101");
                Response.Headers.Add("Warning", $"The provided email address ({Model.Email}) is not valid");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }

            // 2. Make sure the UserName does not exist in the database already as a UserName or Email
            if (RequestedUser == null &&
                (DbContext.ApplicationUser.Any(u => u.UserName == Model.UserName) || 
                    DbContext.ApplicationUser.Any(u => u.Email == Model.UserName)))
            {
                Response.Headers.Add("MapReason", "103");
                Response.Headers.Add("Warning", $"The provided user name ({Model.UserName}) already exists in the system");
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            #endregion Validation

            // Because db operations in this transaction use both UserManager and DbContext directly, the transaction must be created from the same 
            // instance of the context that both share.  Because the instance variable refers to the instance that is injected by MVC, this is true.  
            using (IDbContextTransaction DbTransaction = DbContext.Database.BeginTransaction())
            {
                try
                {
                    // Create requested user if not already existing
                    if (RequestedUser == null)
                    {
                        RequestedUser = new ApplicationUser
                        {
                            UserName = Model.UserName,
                            Email = Model.Email,
                            LastName = Model.LastName,
                            FirstName = Model.FirstName,
                            PhoneNumber = Model.PhoneNumber,
                            Employer = Model.Employer,
                            // Maintain this function's parameter bind list to match the fields being used here
                        };

                        await _userManager.CreateAsync(RequestedUser);
                    }

                    foreach (var ClientId in Model.MemberOfClientIdArray)
                    {
                        IdentityUserClaim<long> ThisClientMembershipClaim = new IdentityUserClaim<long> { ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = ClientId.ToString(), UserId = RequestedUser.Id };
                        if (!DbContext.UserClaims.Any(uc => uc.ClaimType == ThisClientMembershipClaim.ClaimType
                                                         && uc.ClaimValue == ThisClientMembershipClaim.ClaimValue
                                                         && uc.UserId == RequestedUser.Id))
                        {
                            DbContext.UserClaims.Add(ThisClientMembershipClaim);
                        }
                    }
                    DbContext.SaveChanges();

                    DbTransaction.Commit();
                }
                catch (Exception e)
                {
                    string ErrMsg = $"Exception while creating new user \"{Model.UserName}\" or assigning user membership in client(s): [{string.Join(",", Model.MemberOfClientIdArray)}]";
                    while (e != null)
                    {
                        ErrMsg += $"\r\n{e.Message}";
                        e = e.InnerException;
                    }
                    _logger.LogError(ErrMsg);

                    Response.Headers.Add("Warning", $"Failed to complete operation");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }

            // UserCreated Audit log
            var CreatedUserDetailObject = new { NewUserName = Model.UserName, Email = Model.Email, };
            AuditEvent UserCreatedEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "New user created", AuditEventId.UserAccountCreated, CreatedUserDetailObject, User.Identity.Name, HttpContext.Session.Id);
            _auditLogger.Log(UserCreatedEvent);

            // Client membership assignment Audit log
            foreach (var ClientId in Model.MemberOfClientIdArray)
            {
                var AssignedClientDetailObject = new { NewUserName = Model.UserName, ClientId = ClientId, RequestedClientIds = string.Join(",", Model.MemberOfClientIdArray), };
                AuditEvent UserAssignedEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "New user assigned to client", AuditEventId.UserAssignedToClient, AssignedClientDetailObject, User.Identity.Name, HttpContext.Session.Id);
                _auditLogger.Log(UserAssignedEvent);
            }

            // TODO: Send proper welcome email w/ link to set initial password
            MessageQueueService.QueueEmail(Model.Email, "Welcome to Milliman blah blah", "Message text");

            Response.Headers.Add("Warning", $"The requested user was successfully saved");
            return Ok("New User saved successfully");
        }

        [HttpGet]
        public async Task<IActionResult> GetContentForClient(long RequestedClientId)
        {
            #region Authorization
            AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.UserAdmin, RequestedClientId));
            if (!Result1.Succeeded)
            {
                var AssignedClientDetailObject = new { RequestedClientId };
                AuditEvent LogEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Request to get client details without required role", AuditEventId.Unauthorized, AssignedClientDetailObject, User.Identity.Name, HttpContext.Session.Id);
                _auditLogger.Log(LogEvent);

                Response.Headers.Add("Warning", "You are not authorized to view details of the selected client");
                return Unauthorized();
            }
            #endregion

            #region Validation
            // I would validate existence of the client record, but the authorization test would have already failed above
            #endregion

            UserAdminClientDetailViewModel ReturnModel = UserAdminClientDetailViewModel.GetModel(RequestedClientId, DbContext);

            return Json(ReturnModel);
        }

        /// <summary>
        /// Reusable private method to execute new user insertion
        /// </summary>
        /// <param name="Model"></param>
        /// <returns>The complete user record on success, null on failure</returns>
        [NonAction]
        private async Task<ApplicationUser> InsertUser(ApplicationUserViewModel Model)
        {
            // Authorization not required, this is a private non-action for internal use
            ApplicationUser NewUser = new ApplicationUser
            {
                UserName = Model.UserName,
                Email = Model.Email,
                LastName = Model.LastName,
                FirstName = Model.FirstName,
                PhoneNumber = Model.PhoneNumber,
                Employer = Model.Employer,
            };

            // Save new user to the database
            var Result = await _userManager.CreateAsync(NewUser);

            return (Result.Succeeded) ? NewUser : null;
        }

        // POST: UserAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ApplicationUserViewModel Model)
        {
            try
            {
                #region Authorization
                // TODO Is this the required role to authorize this action
                AuthorizationResult Result1 = await AuthorizationService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.Admin));
                if (!Result1.Succeeded)
                {
                    return Unauthorized();
                }
                #endregion

                // Get the existing user record from db
                ApplicationUser ExistingUser = await _userManager.FindByIdAsync(Model.UserName);

                #region Validation  // Validate field by field
                // UserName
                if (Model.UserName != ExistingUser.UserName)
                {
                    if (await _userManager.FindByEmailAsync(Model.UserName) != null || 
                        await _userManager.FindByLoginAsync("", Model.UserName) != null)
                    {
                        Response.Headers.Add("MapReason", "101");
                        Response.Headers.Add("Warning", $"The provided user name ({Model.UserName}) already exists in the system");
                        return StatusCode(StatusCodes.Status412PreconditionFailed);
                    }

                    ExistingUser.UserName = Model.UserName;
                }

                // Email
                if (Model.Email != ExistingUser.Email)
                {
                    // Make sure the Email is Valid format
                    if (!GlobalFunctions.IsValidEmail(Model.Email))
                    {
                        Response.Headers.Add("MapReason", "102");
                        Response.Headers.Add("Warning", $"The provided email address ({Model.Email}) is not valid");
                        return StatusCode(StatusCodes.Status412PreconditionFailed);
                    }

                    // Make sure the Email does not exist in the database already as an Email or UserName
                    if (await _userManager.FindByEmailAsync(Model.Email) != null || 
                        await _userManager.FindByLoginAsync("", Model.Email) != null)
                    {
                        Response.Headers.Add("MapReason", "103");
                        Response.Headers.Add("Warning", $"The provided email address ({Model.Email}) already exists in the system");
                        return StatusCode(StatusCodes.Status412PreconditionFailed);
                    }

                    ExistingUser.Email = Model.Email;
                }

                if (Model.LastName != ExistingUser.LastName)
                {
                    // no test
                    ExistingUser.LastName = Model.LastName;
                }

                if (Model.FirstName != ExistingUser.FirstName)
                {
                    // no test
                    ExistingUser.FirstName = Model.FirstName;
                }

                if (Model.PhoneNumber != ExistingUser.PhoneNumber)
                {
                    // no test
                    ExistingUser.PhoneNumber = Model.PhoneNumber;
                }

                if (Model.Employer != ExistingUser.Employer)
                {
                    // no test
                    ExistingUser.Employer = Model.Employer;
                }
                #endregion

                await _userManager.UpdateAsync(ExistingUser);

                // Process Super User checkbox
                if (await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(HttpContext.User), RoleEnum.Admin.ToString()))
                {
                    // The checkbox returns "true,false" or "false,true" if you change the value. The first one is the new value, so we need to grab it.

                    // TODO need to alter the model for this

                    /* 
                    bool IsSuperUser = Convert.ToBoolean(collection["IsSystemAdmin"].ToString().Split(',')[0]);

                    if (IsSuperUser && !(await _userManager.IsInRoleAsync(ExistingUser, ApplicationRole.MapRoles[RoleEnum.SystemAdmin])))
                    {
                        await _userManager.AddToRoleAsync(ExistingUser, ApplicationRole.MapRoles[RoleEnum.SystemAdmin]);
                    }
                    else if (!IsSuperUser && (await _userManager.IsInRoleAsync(ExistingUser, ApplicationRole.MapRoles[RoleEnum.SystemAdmin])))
                    {
                        await _userManager.RemoveFromRoleAsync(ExistingUser, ApplicationRole.MapRoles[RoleEnum.SystemAdmin]);
                    }

                    */
                }

                return RedirectToAction("Index");
            }
            catch
            {
                // TODO: Add more robust error handling & display an error message to the user
                // TODO Modify the View to accept this model type
                return View(Model);
            }
        }

        // TODO: Add & implement action to disable (not delete) a user account
    }
}