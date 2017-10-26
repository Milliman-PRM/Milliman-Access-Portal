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
using MillimanAccessPortal.Authorization;
using MapCommonLib;
using MillimanAccessPortal.Services;

namespace MillimanAccessPortal.Controllers
{
    public class ManageUsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly MessageQueueServices MessageQueueService;

        public ManageUsersController(
            UserManager<ApplicationUser> userManager,
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            MessageQueueServices MessageQueueServiceArg
            )
        {
            _userManager = userManager;
            _auditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            MessageQueueService = MessageQueueServiceArg;
        }

        // GET: ManageUsers
        public ActionResult Index()
        {
            IQueryable<ApplicationUser> users = _userManager.Users;
            
            return View(users);
        }

        // GET: ManageUsers/Details/5
        public async Task<ActionResult> Details(string id)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(id);

            ViewData["isSystemAdmin"] = await _userManager.IsInRoleAsync(user, ApplicationRole.MapRoles[RoleEnum.SuperUser]);
            return View(user);
        }

        // GET: ManageUsers/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ManageUsers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task <ActionResult> Create(IFormCollection collection)
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
                if (!GlobalFunctions.IsValidEmail(collection["Email"]))
                {
                    Response.Headers.Add("Warning", $"The provided email address is not valid");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }

                // 2. Make sure the Email does not exist in the database already
                ApplicationUser userByEmail = await _userManager.FindByEmailAsync(collection["Email"]);
                if (userByEmail != null)
                {
                    Response.Headers.Add("Warning", $"The provided email address already exists");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }

                // 3. Make sure the UserName does not exist in the database already
                ApplicationUser userByUserName = await _userManager.FindByLoginAsync("", collection["UserName"]);
                if (userByUserName != null)
                {
                    Response.Headers.Add("Warning", $"The provided user name already exists");
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }
                #endregion

                ApplicationUser NewUser = new ApplicationUser
                {
                    UserName = collection["UserName"],
                    Email = collection["Email"],
                    LastName = collection["LastName"],
                    FirstName = collection["FirstName"],
                    Employer = collection["Employer"],
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
                    // TODO: Raise some kind of error
                    //return View();
                    return Content(result.ToString());
                }
                
            }
            catch
            {
                return View();
            }
        }
        
        // GET: ManageUsers/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(id);

            ViewData["isSystemAdmin"] = await _userManager.IsInRoleAsync(user, ApplicationRole.MapRoles[RoleEnum.SuperUser]);
            return View(user);
        }

        // POST: ManageUsers/Edit/5
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