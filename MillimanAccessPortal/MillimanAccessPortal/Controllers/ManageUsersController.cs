using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MillimanAccessPortal.Models;
using MillimanAccessPortal.Data;
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
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
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
                user.Email = collection["Email"];
                user.NormalizedEmail = collection["Email"].ToString().ToUpper();
                user.LockoutEnabled = Convert.ToBoolean(collection["LockoutEnabled"]);
                
                await _userManager.UpdateAsync(user);
                
                return RedirectToAction("Index");
            }
            catch
            {
                return View(user);
            }
        }

        // GET: ManageUsers/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ManageUsers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}