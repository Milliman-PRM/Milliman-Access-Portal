using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Security.Claims;
using MillimanAccessPortal.Models.ClientAdminViewModels;

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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUser(EditClientViewModel Model)
        {
            // TODO check whether each assigned user email is from an acceptable domain

            Client ThisClient = await DbContext.Client.SingleOrDefaultAsync(m => m.Id == Model.ThisClient.Id);
            Claim ThisClientMembershipClaim = new Claim("ClientMembership", ThisClient.Name);
            IList<ApplicationUser> AlreadyAssignedUsers = UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim).Result;

            foreach (var UserInSelectList in Model.ApplicableUsers)
            {
                /*
                ApplicationUser ThisUser = DbContext.ApplicationUser.SingleOrDefault(u => u.Id == UserInSelectList.);

                if (!AlreadyAssignedUsers.Select(u=>u.Id).Contains(AssignedUserId))
                {
                    // Create a claim for this user and this client
                    UserManager.AddClaimAsync(ThisUser, ThisClientMembershipClaim).Wait();
                }
                */
                var x = UserInSelectList;
            }
            if (ModelState.IsValid)
            {
                //UserAuthorizationToClient NewRoleAssignment
            }

            return RedirectToAction("Edit", new { Id = Model.ThisClient.Id });
        }

        // GET: ClientAdmin/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ThisClient = await DbContext.Client.SingleOrDefaultAsync(m => m.Id == id);
            if (ThisClient == null)
            {
                return NotFound();
            }

            Client NoParent = new Client { Id = -1, Name = "<None>", ParentClientId = null };
            List<Client> AllClients = DbContext.Client
                .OrderBy(i => i.Name)
                .Prepend(NoParent)
                .ToList();

            ViewBag.ParentClientId = new SelectList(AllClients, "Id", "Name", ThisClient.ParentClientId != null ? ThisClient.ParentClientId : NoParent.Id);

            // Get all users currently assigned to any related Client (any descendant of the root client)
            List<Client> AllRelatedClients = new StandardQueries(ServiceProvider).GetAllRelatedClients(ThisClient);
            List<ApplicationUser> UsersAssignedToRelatedClients = new List<ApplicationUser>();
            foreach (Client OneClient in AllRelatedClients)
            {
                Claim ThisClientMembershipClaim = new Claim("ClientMembership", OneClient.Name);
                UsersAssignedToRelatedClients = UsersAssignedToRelatedClients.Union(UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim).Result).ToList();
            }

            List < ApplicationUser> EligibleUsers = new List<ApplicationUser>();
            foreach (string AcceptableDomain in ThisClient.AcceptedEmailDomainList)
            {
                if (string.IsNullOrWhiteSpace( AcceptableDomain))
                {
                    continue;
                }
                EligibleUsers.AddRange(DbContext.ApplicationUser.Where(u => u.NormalizedEmail.Contains($"@{AcceptableDomain.ToUpper()}")));
            }

            EditClientViewModel Model = new EditClientViewModel
            {
                ThisClient = ThisClient,
                //ApplicableUsers = new List<SelectableNamedThing>(EligibleUsers.Intersect(UsersAssignedToRelatedClients).OrderBy(u => u.NormalizedEmail), "Id", "UserName", UsersAssignedToRelatedClients.Select(u => u.Id)).ToList(),
                ApplicableUsers = EligibleUsers.Intersect(UsersAssignedToRelatedClients)
                                  .OrderBy(u => u.NormalizedEmail)
                                  .Select(x => new SelectableNamedThing { Id = x.Id, DisplayName = x.Email, Selected = true }) // TODO fix Selected to indicate those selected for this client
                                  .ToList()
            };

            return View(Model);
        }

        // POST: ClientAdmin/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(long id, [Bind("Id,Name,AcceptedEmailDomainList,ParentClientId")] Client client)
        public async Task<IActionResult> Edit(long id, EditClientViewModel Model)
        {
            if (id != Model.ThisClient.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    DbContext.Update(Model.ThisClient);
                    await DbContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(Model.ThisClient.Id))
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
            //ViewData["ParentClientId"] = new SelectList(DbContext.Client, "Id", "Name", client.ParentClientId);
            return RedirectToAction("Edit", new { Id = Model.ThisClient.Id });
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
