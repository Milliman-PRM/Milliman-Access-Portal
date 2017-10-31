/*
 * CODE OWNERS: Ben Wyatt, Tom Puckett
 * OBJECTIVE: Provide all application logic for user administration
 * DEVELOPER NOTES: 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using AuditLogLib;
using AuditLogLib.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using MillimanAccessPortal.Authorization;
using MapCommonLib;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.Models.UserAdminViewModels;

namespace MillimanAccessPortal.Controllers
{
    public class UserAdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly MessageQueueServices MessageQueueService;
        private readonly ILogger _logger;

        public UserAdminController(
            UserManager<ApplicationUser> userManager,
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            MessageQueueServices MessageQueueServiceArg,
            ILoggerFactory LoggerFactoryArg
            )
        {
            _userManager = userManager;
            _auditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            MessageQueueService = MessageQueueServiceArg;
            _logger = LoggerFactoryArg.CreateLogger<UserAdminController>();
        }

        // GET: UserAdmin
        public ActionResult Index()
        {
            List<ApplicationUserViewModel> Model = _userManager.Users.ToList()
                .Select(u => new ApplicationUserViewModel(u)).ToList();

            return View(Model);
        }

        // GET: UserAdmin/Details/5
        public async Task<ActionResult> Details(string id)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(id);

            ViewData["isSystemAdmin"] = await _userManager.IsInRoleAsync(user, ApplicationRole.MapRoles[RoleEnum.SystemAdmin]);
            return View(user);
        }

        // GET: UserAdmin/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task <ActionResult> Create(ApplicationUserViewModel Model)
        {
            try
            {
                #region Authorization
                if (!AuthorizationService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.SystemAdmin)).Result)
                {
                    return Unauthorized();
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

                IdentityResult result = await InsertUser(Model);

                if (result.Succeeded)
                {
                    var LogDetailObject = new { NewUserId = Model.UserName, Email = Model.Email, };
                    AuditEvent LogEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "New user created", AuditEventId.UserAccountCreated, LogDetailObject, User.Identity.Name, HttpContext.Session.Id);
                    _auditLogger.Log(LogEvent);

                    // TODO: Send welcome email w/ link to set initial password
                    MessageQueueService.QueueEmail(Model.Email, "Welcome to Milliman blah blah", "Message text");

                    // TODO: Add a success message to Index
                    return RedirectToAction("Index");
                }
                else
                {
                    string ErrMsg = $"Failed to store new user \"{Model.UserName}\" ";
                    _logger.LogError(ErrMsg);
                    //return View();
                    return Content(ErrMsg);
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
                return View();
            }
        }

        /// <summary>
        /// Reusable private method to execute new user insertion
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        [NonAction]
        private async Task<IdentityResult> InsertUser(ApplicationUserViewModel Model)
        {
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
            return await _userManager.CreateAsync(NewUser);
        }

        // GET: UserAdmin/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(id);

            ViewData["isSystemAdmin"] = await _userManager.IsInRoleAsync(user, ApplicationRole.MapRoles[RoleEnum.SystemAdmin]);
            return View(user);
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
                if (!AuthorizationService.AuthorizeAsync(User, null, new UserGlobalRoleRequirement(RoleEnum.SystemAdmin)).Result)
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
                if (await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(HttpContext.User), ApplicationRole.MapRoles[RoleEnum.SystemAdmin]))
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