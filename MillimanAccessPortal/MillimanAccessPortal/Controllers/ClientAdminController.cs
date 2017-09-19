using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;

namespace MillimanAccessPortal.Controllers
{
    public class ClientAdminController : Controller
    {
        private readonly ApplicationDbContext DbContext;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly IServiceProvider ServiceProvider;

        public ClientAdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> UserManagerArg,
            IServiceProvider ServiceProviderArg
            )
        {
            DbContext = context;
            UserManager = UserManagerArg;
            ServiceProvider = ServiceProviderArg;
        }

    // GET: ClientAdmin
    public IActionResult Index()
        {
            List<Client> AuthorizedClients = new StandardQueries(ServiceProvider).GetListOfClientsUserIsAuthorizedToManage(UserManager.GetUserName(HttpContext.User));
            return View(AuthorizedClients);
        }

        // GET: ClientAdmin/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await DbContext.Client
                .Include(c => c.ParentClient)
                .SingleOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // GET: ClientAdmin/Create
        public IActionResult Create()
        {
            List<Client> AuthorizedClients = new StandardQueries(ServiceProvider).GetListOfClientsUserIsAuthorizedToManage(UserManager.GetUserName(HttpContext.User));
            // Use this one.  Filter out clients that current user is not authorized to manage
            //IQueryable<Client> FilteredCandidateParents = _context.Client.Where(c => /*user authorized to manage c*/);
            //IQueryable<Client> FilteredCandidateParents = DbContext.Client.Where(c => c.Id == 1/*user authorized to manage c*/);
            ViewData["ParentClientId"] = new SelectList(AuthorizedClients, "Id", "Name")
                                                            .OrderBy(c => c.Text)
                                                            .Prepend(new SelectListItem {Value = "", Text = "None" });  // for new root client
            return View();
        }

        // POST: ClientAdmin/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,AcceptedEmailDomainList,ParentClientId")] Client client)
        {
            if (ModelState.IsValid)
            {
                DbContext.Add(client);
                await DbContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewData["ParentClientId"] = new SelectList(DbContext.Client, "Id", "Name", client.ParentClientId);
            return View(client);
        }

        // GET: ClientAdmin/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await DbContext.Client.SingleOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return NotFound();
            }
            ViewData["ParentClientId"] = new SelectList(DbContext.Client, "Id", "Name", client.ParentClientId);
            return View(client);
        }

        // POST: ClientAdmin/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,Name,AcceptedEmailDomainList,ParentClientId")] Client client)
        {
            if (id != client.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    DbContext.Update(client);
                    await DbContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
            ViewData["ParentClientId"] = new SelectList(DbContext.Client, "Id", "Name", client.ParentClientId);
            return View(client);
        }

        // GET: ClientAdmin/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await DbContext.Client
                .Include(c => c.ParentClient)
                .SingleOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // POST: ClientAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var client = await DbContext.Client.SingleOrDefaultAsync(m => m.Id == id);
            DbContext.Client.Remove(client);
            await DbContext.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        private bool ClientExists(long id)
        {
            return DbContext.Client.Any(e => e.Id == id);
        }
    }
}
