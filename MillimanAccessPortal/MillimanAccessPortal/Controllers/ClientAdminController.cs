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

        // TODO Do we need this at all?  Maybe combine with Edit
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
        // Id argument is the intended parent client id if any
        public IActionResult Create(int? Id = null)
        {
            List<Client> AuthorizedClients = new StandardQueries(ServiceProvider).GetListOfClientsUserIsAuthorizedToManage(UserManager.GetUserName(HttpContext.User));

            // Choose the requested parent client, but only if it is authorized
            SelectList ParentSelectList = AuthorizedClients.Any(c => c.Id == Id) ?
                new SelectList(AuthorizedClients, "Id", "Name", Id) : 
                new SelectList(AuthorizedClients, "Id", "Name");

            ViewData["ParentClientId"] = ParentSelectList.OrderBy(c => c.Text).Prepend(new SelectListItem { Value = "", Text = "None" });  // for new root client;
            return View();
        }

        // POST: ClientAdmin/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,AcceptedEmailDomainList,ParentClientId")] Client client)
        {
            if (ModelState.IsValid)
            {
                DbContext.Add(client);
                await DbContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return Create();  // Go back to the empty Create form
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
            // TODO need a bunch of code to undo everything that links to the client and effusive validation checks too.  
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
