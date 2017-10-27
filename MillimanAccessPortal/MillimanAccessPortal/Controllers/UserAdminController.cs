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

            ViewData["isSystemAdmin"] = await _userManager.IsInRoleAsync(user, ApplicationRole.MapRoles[RoleEnum.SuperUser]);
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
            // TODO need to convert the argument to a specialized class so that named members can be referenced

            try
            {
                #region Authorization
                // What is the required role to authorize this action
                if (!AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement(RoleEnum.UserManager, null)).Result)
                {
                    //return Unauthorized();
                }
                #endregion

                #region Validation
                // 1. Email must be a valid address
                if (!GlobalFunctions.IsValidEmail(Model.Email))
                {
                    Response.Headers.Add("Warning", $"The provided email address ({Model.Email}) is not valid");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }

                // 2. Make sure the Email does not exist in the database already as an Email or UserName
                if (await _userManager.FindByEmailAsync(Model.Email) != null || await _userManager.FindByLoginAsync("", Model.Email) != null)
                {
                    Response.Headers.Add("Warning", $"The provided email address ({Model.Email}) already exists in the system");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }

                // 3. Make sure the UserName does not exist in the database already as an Email or UserName
                if (await _userManager.FindByEmailAsync(Model.UserName) != null || await _userManager.FindByLoginAsync("", Model.UserName) != null)
                {
                    Response.Headers.Add("Warning", $"The provided user name ({Model.UserName}) already exists in the system");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }
                #endregion

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
                IdentityResult result = await _userManager.CreateAsync(NewUser);

                if (result.Succeeded)
                {
                    var LogDetailObject = new { NewUserId = NewUser.UserName, Email = NewUser.Email, };
                    AuditEvent LogEvent = AuditEvent.New($"{this.GetType().Name}.{ControllerContext.ActionDescriptor.ActionName}", "New user created", AuditEventId.UserAccountCreated, LogDetailObject, User.Identity.Name, HttpContext.Session.Id);
                    _auditLogger.Log(LogEvent);

                    // TODO: Send welcome email w/ link to set initial password
                    MessageQueueService.QueueEmail(NewUser.Email, "Welcome to Milliman blah blah", "Message text");

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

        // GET: UserAdmin/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(id);

            ViewData["isSystemAdmin"] = await _userManager.IsInRoleAsync(user, ApplicationRole.MapRoles[RoleEnum.SuperUser]);
            return View(user);
        }

        // POST: UserAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(id);

            try
            {
                // Process built-in Identity fields
                user.Email = collection["Email"];
                user.NormalizedEmail = collection["Email"].ToString().ToUpper();
                user.LockoutEnabled = Convert.ToBoolean(collection["LockoutEnabled"].ToString().Split(',')[0]);
                
                await _userManager.UpdateAsync(user);

                // Process Super User checkbox
                // The checkbox returns "true,false" or "false,true" if you change the value. The first one is the new value, so we need to grab it.
                bool IsSuperUser = Convert.ToBoolean(collection["IsSystemAdmin"].ToString().Split(',')[0]);
                
                if (IsSuperUser && !(await _userManager.IsInRoleAsync(user, ApplicationRole.MapRoles[RoleEnum.SuperUser])))
                {
                    await _userManager.AddToRoleAsync(user, ApplicationRole.MapRoles[RoleEnum.SuperUser]);
                }
                else if (!IsSuperUser && (await _userManager.IsInRoleAsync(user, ApplicationRole.MapRoles[RoleEnum.SuperUser])))
                {
                    await _userManager.RemoveFromRoleAsync(user, ApplicationRole.MapRoles[RoleEnum.SuperUser]);
                }

                return RedirectToAction("Index");
            }
            catch
            {
                // TODO: Add more robust error handling & display an error message to the user
                return View(user);
            }
        }

        // TODO: Add & implement action to disable (not delete) a user account
    }
}