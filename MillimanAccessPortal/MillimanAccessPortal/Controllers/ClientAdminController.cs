using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Security.Claims;
using MillimanAccessPortal.Models.ClientAdminViewModels;
using MillimanAccessPortal.Authorization;

namespace MillimanAccessPortal.Controllers
{
    public class ClientAdminController : Controller
    {
        private readonly ApplicationDbContext DbContext;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly IServiceProvider ServiceProvider;
        private readonly IAuthorizationService AuthorizationService;

        public ClientAdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> UserManagerArg,
            IServiceProvider ServiceProviderArg,
            IAuthorizationService AuthorizationServiceArg
            )
        {
            DbContext = context;
            UserManager = UserManagerArg;
            ServiceProvider = ServiceProviderArg;
            AuthorizationService = AuthorizationServiceArg;
        }

        // GET: ClientAdmin
        // Intended for access by ajax from Index view
        [HttpGet]
        public IActionResult ClientFamilyList()
        {
            if (!AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement { RoleEnum = RoleEnum.ClientAdministrator }).Result)
            {
                return Unauthorized();
            }

            long CurrentUserId = GetCurrentUser().Id;

            List<ClientAndChildrenViewModel> ModelToReturn = new List<ClientAndChildrenViewModel>();

            List<Client> AllRootClients = new StandardQueries(ServiceProvider).GetAllRootClients();
            foreach (Client C in AllRootClients.OrderBy(c=>c.Name))
            {
                ClientAndChildrenViewModel ClientModel = new StandardQueries(ServiceProvider).GetDescendentFamilyOfClient(C, CurrentUserId, true);
                if (ClientModel.IsThisOrAnyChildManageable())
                {
                    ModelToReturn.Add(ClientModel);
                }
            }

            return Json(ModelToReturn);
        }

        // GET: ClientAdmin
        // Intended for access by ajax from Index view
        [HttpGet]
        public IActionResult ClientUserLists(long? id)
        {
            if (id == null)
            {
                // TODO do better than this?
                return NotFound();
            }
            Client ThisClient = DbContext.Client.SingleOrDefaultAsync(m => m.Id == id).Result;

            if (!AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement { RoleEnum = RoleEnum.ClientAdministrator, ClientId = ThisClient.Id }).Result)
            {
                return Unauthorized();
            }

            if (ThisClient == null)
            {
                // TODO do better than this?
                return NotFound();
            }

            // Authorization check TODO move this to proper authorization mechanism
            ApplicationUser CurrentUser = GetCurrentUser();
            if (!DbContext
                .UserRoleForClient
                .Include(urc => urc.Role)
                .Any(urc => urc.UserId == CurrentUser.Id && 
                            urc.ClientId == id && 
                            urc.Role.RoleEnum == RoleEnum.ClientAdministrator)
                )
            {
                return Unauthorized();
            }

            ClientUserListsViewModel Model = new ClientUserListsViewModel();

            Claim ThisClientMembershipClaim = new Claim("ClientMembership", ThisClient.Name);

            // Get the list of users already assigned to this client
            Model.AssignedUsers = UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim)
                                             .Result
                                             .Select(ApUser => (UserInfo)ApUser)
                                             .ToList();
            // Assign the remaining assigned user properties
            foreach (UserInfo Item in Model.AssignedUsers)
            {
                Item.UserRoles = new StandardQueries(ServiceProvider).GetUserRolesForClient(Item.Id, ThisClient.Id);
            }

            // Get all users currently member of any related Client (any descendant of the root client)
            List<Client> AllRelatedClients = new StandardQueries(ServiceProvider).GetAllRelatedClients(ThisClient);
            List<ApplicationUser> UsersAssignedToClientFamily = new List<ApplicationUser>();
            foreach (Client OneClient in AllRelatedClients)
            {
                ThisClientMembershipClaim = new Claim("ClientMembership", OneClient.Name);
                UsersAssignedToClientFamily = UsersAssignedToClientFamily.Union(UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim).Result).ToList();
                // TODO Test whether the other overload of .Union() needs to be used with an IEqualityComparer argument.  For this use equality should probably be based on Id only.
            }

            foreach (string AcceptableDomain in ThisClient.AcceptedEmailDomainList)
            {
                if (string.IsNullOrWhiteSpace(AcceptableDomain))
                {
                    continue;
                }
                Model.EligibleUsers.AddRange(UsersAssignedToClientFamily.Where(u => u.NormalizedEmail.Contains($"@{AcceptableDomain.ToUpper()}"))
                                                                        .Select(u=>(UserInfo)u));
            }

            // Assign the remaining assigned user properties
            foreach (UserInfo Item in Model.EligibleUsers)
            {
                Item.UserRoles = new StandardQueries(ServiceProvider).GetUserRolesForClient(Item.Id, ThisClient.Id);
            }

            // Subtract the assigned users from the overall list of eligible users
            Model.EligibleUsers = Model.EligibleUsers.Except(Model.AssignedUsers, new UserInfoEqualityComparer()).ToList();

            return Json(Model);
        }

        // GET: ClientAdmin
        /// <summary>
        /// Action leading to the main landing page for Client administration UI
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
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
                return RedirectToAction(nameof(ClientAdminController.Index));
            }

            var ThisClient = await DbContext.Client.SingleOrDefaultAsync(m => m.Id == id);
            if (ThisClient == null)
            {
                return RedirectToAction(nameof(ClientAdminController.Index));
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

        [NonAction]
        private ApplicationUser GetCurrentUser()
        {
            return UserManager.GetUserAsync(HttpContext.User).Result;
        }
    }
}
