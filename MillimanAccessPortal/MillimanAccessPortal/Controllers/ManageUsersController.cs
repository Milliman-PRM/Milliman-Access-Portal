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

namespace MillimanAccessPortal.Controllers
{
    public class ManageUsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        
        public ManageUsersController(
            UserManager<ApplicationUser> userManager
            )
        {
            _userManager = userManager;
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

            ViewData["isSystemAdmin"] = await user.IsSuperUser(_userManager);
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
            try
            {
                /*
                 * Validations needed:
                 *      Username or email address already exists in the database
                 *      Email address is a valid address (client-side preferred)
                 */

                // Make sure the user does not exist in the database already
                ApplicationUser userByName = await _userManager.FindByEmailAsync(collection["Email"]);
                ApplicationUser userById = await _userManager.FindByLoginAsync("", collection["Email"]);

                if (userByName != null || userById != null)
                {
                    // The specified email address already exists in the database
                    // TODO: Implement a custom error handler here and redirect back to the form w/ an error
                    return View();
                }

                ApplicationUser user = new ApplicationUser(collection["Email"]);

                // Prepare basic profile information
                user.Email = user.UserName;
                user.NormalizedEmail = user.Email.ToUpper();
                user.NormalizedUserName = user.UserName.ToUpper();
                user.ConcurrencyStamp = new Guid().ToString();

                // Save new user to the database
                IdentityResult result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    // TODO: Send welcome email w/ link to set initial password
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

            ViewData["isSystemAdmin"] = await user.IsSuperUser(_userManager);
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
                user.LockoutEnabled = Convert.ToBoolean(collection["LockoutEnabled"]);
                
                await _userManager.UpdateAsync(user);

                // Process Super User checkbox
                // The checkbox returns "true,false" or "false,true" if you change the value. The first one is the new value, so we need to grab it.
                bool IsSuperUser = Convert.ToBoolean(collection["IsSystemAdmin"].ToString().Split(',')[0]);
                
                if (IsSuperUser && !(await user.IsSuperUser(_userManager)))
                {
                    await _userManager.AddToRoleAsync(user, "Super User");
                }
                else if (!IsSuperUser && (await user.IsSuperUser(_userManager)))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Super User");
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