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
using Microsoft.EntityFrameworkCore;
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
        public ActionResult Index()
        {
            if (!AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.UserAdmin, null)).Result.Succeeded &&
                !AuthorizationService.AuthorizeAsync(User, null, new RoleInRootContentItemRequirement(RoleEnum.UserAdmin, null)).Result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized as a user admin.");
                return Unauthorized();
            }

            List<ApplicationUserViewModel> Model = _userManager.Users.ToList()
                .Select(u => new ApplicationUserViewModel(u, _userManager)).ToList();

            return View(Model);
        }

        // GET: UserAdmin/ClientFamilyList
        /// <summary>
        /// Returns the list of Client families that the current user has visibility to (defined by GetUserAdminClientListModel(...)
        /// </summary>
        /// <returns>JsonResult or UnauthorizedResult</returns>
        [HttpGet]
        public ActionResult ClientFamilyList()
        {
            #region Authorization
            // User must have UserAdmin role to at least 1 Client
            if (!AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.UserAdmin, null)).Result.Succeeded)
            {
                Response.Headers.Add("Warning", $"You are not authorized as a user admin");
                return Unauthorized();
            }
            #endregion

            UserAdminClientFamilyListViewModel Model = GetUserAdminClientListModel(Queries.GetCurrentApplicationUser(User));

            return Json(Model);
        }

        /// <summary>
        /// A utility method to provide a model for the client list in the application's UserAdmin page
        /// </summary>
        /// <param name="CurrentUser"></param>
        /// <returns></returns>
        [NonAction]  // maybe move this elsewhere, (e.g. the model class itself?)
        private UserAdminClientFamilyListViewModel GetUserAdminClientListModel(ApplicationUser CurrentUser)
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
                ClientAndChildrenModel ClientModel = Queries.GetDescendentFamilyOfClient(RootClient, CurrentUser, RoleEnum.UserAdmin, false, true);
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
            try
            {
                #region Authorization
                // User must be UserCreator in the system
                if (!AuthorizationService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.UserCreator)).Result.Succeeded)
                {
                    var AssignedClientDetailObject = new { RequestedUser = Model.UserName, RequiredRole = RoleEnum.UserCreator.ToString(), RequestedClientIds = string.Join(",", Model.MemberOfClientIdArray) };
                    AuditEvent LogEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Request to create user without required role", AuditEventId.Unauthorized, AssignedClientDetailObject, User.Identity.Name, HttpContext.Session.Id);
                    _auditLogger.Log(LogEvent);

                    Response.Headers.Add("Warning", "You are not authorized to create a user");
                    return Unauthorized();
                }

                // If client assignment is requested, user must be UserAdmin for the requested client
                foreach (var AssignedClientId in Model.MemberOfClientIdArray)
                {
                    if (!AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.UserAdmin, AssignedClientId)).Result.Succeeded)
                    {
                        Client UnauthorizedClient = DbContext.Client.Find(AssignedClientId);
                        if (UnauthorizedClient != null)
                        {
                            var AssignedClientDetailObject = new { RequestedUser = Model.UserName, RequiredRole = RoleEnum.UserCreator.ToString(), RequestedClientIds = string.Join(",", Model.MemberOfClientIdArray) };
                            AuditEvent LogEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Request to create new user associated with unauthorized client(s)", AuditEventId.Unauthorized, AssignedClientDetailObject, User.Identity.Name, HttpContext.Session.Id);
                            _auditLogger.Log(LogEvent);

                            Response.Headers.Add("Warning", $"You are not authorized to assign a user to the requested client ({UnauthorizedClient.Name})");
                        }
                        else
                        {
                            var AssignedClientDetailObject = new { RequestedUser = Model.UserName, RequiredRole = RoleEnum.UserCreator.ToString(), RequestedClientIds = string.Join(",", Model.MemberOfClientIdArray) };
                            AuditEvent LogEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "Request to create new user associated with nonexistent client(s)", AuditEventId.Unauthorized, AssignedClientDetailObject, User.Identity.Name, HttpContext.Session.Id);
                            _auditLogger.Log(LogEvent);

                            Response.Headers.Add("Warning", "The requested client to be assigned does not exist in the system");
                        }
                        return Unauthorized();
                    }
                }
                #endregion

                #region Validation
                // 1. Email must be a valid address
                if (!GlobalFunctions.IsValidEmail(Model.Email))
                {
                    Response.Headers.Add("MapReason", "101");
                    Response.Headers.Add("Warning", $"The provided email address ({Model.Email}) is not valid");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }

                // 2. Make sure the Email does not exist in the database already as an Email or UserName
                if (await _userManager.FindByEmailAsync(Model.Email) != null || 
                    await _userManager.FindByLoginAsync("", Model.Email) != null)
                {
                    Response.Headers.Add("MapReason", "102");
                    Response.Headers.Add("Warning", $"The provided email address ({Model.Email}) already exists in the system");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }

                // 3. Make sure the UserName does not exist in the database already as an Email or UserName
                if (await _userManager.FindByEmailAsync(Model.UserName) != null || 
                    await _userManager.FindByLoginAsync("", Model.UserName) != null)
                {
                    Response.Headers.Add("MapReason", "103");
                    Response.Headers.Add("Warning", $"The provided user name ({Model.UserName}) already exists in the system");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }
                #endregion

                ApplicationUser NewUser = await InsertUser(Model);

                if (NewUser != null) // Insert succeeded
                {
                    // Audit log
                    var CreatedUserDetailObject = new { NewUserId = Model.UserName, Email = Model.Email, };
                    AuditEvent LogEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "New user created", AuditEventId.UserAccountCreated, CreatedUserDetailObject, User.Identity.Name, HttpContext.Session.Id);
                    _auditLogger.Log(LogEvent);

                    bool ClientAssignResult = true;
                    foreach (var ClientId in Model.MemberOfClientIdArray)
                    {
                        Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), ClientId.ToString());
                        IdentityResult ResultOfAddClaim = _userManager.AddClaimAsync(NewUser, ThisClientMembershipClaim).Result;
                        ClientAssignResult &= ResultOfAddClaim.Succeeded;

                        // Audit log
                        var AssignedClientDetailObject = new { NewUserId = Model.UserName, ClientId = ClientId, RequestedClientIds = string.Join(",", Model.MemberOfClientIdArray), };
                        LogEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "New user assigned to client", AuditEventId.UserAssignedToClient, AssignedClientDetailObject, User.Identity.Name, HttpContext.Session.Id);
                        _auditLogger.Log(LogEvent);
                    }

                    if (!ClientAssignResult)
                    {
                        _userManager.DeleteAsync(NewUser).Wait();

                        // Audit log
                        var DeletedUserDetailObject = new { DeletedUserId = Model.UserName, Email = Model.Email, RequestedClientIds = string.Join(",", Model.MemberOfClientIdArray), Reason = "Error assigning user to a requested client, transaction rollback" };
                        AuditEvent DelUserLogEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "New user deleted due to error", AuditEventId.UserAccountDeleted, DeletedUserDetailObject, User.Identity.Name, HttpContext.Session.Id);
                        _auditLogger.Log(DelUserLogEvent);

                        foreach (var ClientId in Model.MemberOfClientIdArray)
                        {
                            Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), ClientId.ToString());
                            IdentityResult ResultOfAddClaim = _userManager.RemoveClaimAsync(NewUser, ThisClientMembershipClaim).Result;

                            // Audit log
                            var RemovedClientDetailObject = new { NewUserId = Model.UserName, Email = Model.Email, RequestedClientIds = string.Join(",", Model.MemberOfClientIdArray), ClientId = ClientId, Reason = "Error assigning user to a requested client, transaction rollback", };
                            LogEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "New user removed from client", AuditEventId.UserRemovedFromClient, RemovedClientDetailObject, User.Identity.Name, HttpContext.Session.Id);
                            _auditLogger.Log(LogEvent);
                        }

                        Response.Headers.Add("Warning", $"Client assignment failed, user not saved");
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }

                    // TODO: Send proper welcome email w/ link to set initial password
                    MessageQueueService.QueueEmail(Model.Email, "Welcome to Milliman blah blah", "Message text");

                    Response.Headers.Add("Warning", $"The requested user was successfully saved");
                    return Ok("New User saved successfully");
                }
                else
                {
                    string ErrMsg = $"Failed to store new user \"{Model.UserName}\" ";
                    _logger.LogError(ErrMsg);
                    //return View();
                    return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
                }
                
            }
            catch (Exception e)
            {
                string ErrMsg = $"Exception while creating new user \"{Model.UserName}\" ";
                while (e != null)
                {
                    ErrMsg += $"\r\n{e.Message}";
                    e = e.InnerException;
                }
                _logger.LogError(ErrMsg);
                return StatusCode(StatusCodes.Status500InternalServerError, ErrMsg);
            }
        }

        [HttpGet]
        public IActionResult GetContentForClient(long RequestedClientId)
        {
            #region Authorization
            if (!AuthorizationService.AuthorizeAsync(User, null, new RoleInClientRequirement(RoleEnum.UserAdmin, RequestedClientId)).Result.Succeeded)
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

            var x = Json(ReturnModel);
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
                if (!AuthorizationService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.Admin)).Result.Succeeded)
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