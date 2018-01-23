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
using MillimanAccessPortal.Models.ContentAccessAdminViewModels;
using MillimanAccessPortal.Models.ClientAdminViewModels;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Controllers
{
    public class ContentAccessAdminController : Controller
    {
        private readonly ApplicationDbContext DbContext;
        private readonly IAuditLogger _auditLogger;
        private readonly IAuthorizationService AuthorizationService;
        private readonly ILogger _logger;
        private readonly StandardQueries Queries;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContentAccessAdminController(
            ApplicationDbContext DbContextArg,
            IAuditLogger AuditLoggerArg,
            IAuthorizationService AuthorizationServiceArg,
            ILoggerFactory LoggerFactoryArg,
            StandardQueries QueriesArg,
            UserManager<ApplicationUser> userManager
            )
        {
            DbContext = DbContextArg;
            _auditLogger = AuditLoggerArg;
            AuthorizationService = AuthorizationServiceArg;
            _logger = LoggerFactoryArg.CreateLogger<ContentAccessAdminController>();
            Queries = QueriesArg;
            _userManager = userManager;
        }

        // GET: ContentAccessAdmin
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

        // GET: ContentAccessAdmin/ClientFamilyList
        /// <summary>
        /// Returns the list of Client families that the current user has visibility to (defined by GetContentAccessAdminClientListModel(...)
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

            ContentAccessAdminClientFamilyListViewModel Model = await GetContentAccessAdminClientListModel(await Queries.GetCurrentApplicationUser(User));

            return Json(Model);
        }

        /// <summary>
        /// A utility method to provide a model for the client list in the application's ContentAccessAdmin page
        /// </summary>
        /// <param name="CurrentUser"></param>
        /// <returns></returns>
        [NonAction]  // maybe move this elsewhere, (e.g. the model class itself?)
        private async Task<ContentAccessAdminClientFamilyListViewModel> GetContentAccessAdminClientListModel(ApplicationUser CurrentUser)
        {
            #region Validation
            if (CurrentUser == null)
            {
                return null;
            }
            #endregion            
            
            // Instantiate working variables
            ContentAccessAdminClientFamilyListViewModel ModelToReturn = new ContentAccessAdminClientFamilyListViewModel();

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


        
        // GET: ContentAccessAdmin/Details/5
        public async Task<ActionResult> Details(string id)
        {
            ApplicationUser RequestedUser = await _userManager.FindByIdAsync(id);

            ViewData["isSystemAdmin"] = await _userManager.IsInRoleAsync(RequestedUser, RoleEnum.Admin.ToString());
            return View(RequestedUser);
        }

        // GET: ContentAccessAdmin/Create
        public ActionResult Create()
        {
            // NEXT TODO return a ViewModel with the list of clients the current user is authorized to admin
            return View();
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

            ContentAccessAdminClientDetailViewModel ReturnModel = ContentAccessAdminClientDetailViewModel.GetModel(RequestedClientId, DbContext);

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

        // POST: ContentAccessAdmin/Edit/5
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