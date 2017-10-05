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
        /// <summary>
        /// Action leading to the main landing page for Client administration UI
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        // GET: ClientAdmin/ClientFamilyList
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

        // GET: ClientAdmin/ClientUserLists
        // Intended for access by ajax from Index view
        [HttpGet]
        public IActionResult ClientUserLists(long? id)
        {
            Client ThisClient = DbContext.Client.SingleOrDefaultAsync(m => m.Id == id).Result;
            if (ThisClient == null)
            {
                return NotFound();
            }

            // Check current user's authorization to manage the requested Client
            if (!AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement { RoleEnum = RoleEnum.ClientAdministrator, ClientId = ThisClient.Id }).Result)
            {
                return Unauthorized();
                // or:
                // return NotFound();
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

            if (ThisClient.AcceptedEmailDomainList != null)
            {
                foreach (string AcceptableDomain in ThisClient.AcceptedEmailDomainList)
                {
                    if (string.IsNullOrWhiteSpace(AcceptableDomain))
                    {
                        continue;
                    }
                    Model.EligibleUsers.AddRange(UsersAssignedToClientFamily.Where(u => u.NormalizedEmail.Contains($"@{AcceptableDomain.ToUpper()}"))
                                                                            .Select(u => (UserInfo)u));
                }
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

#if true
        [HttpPost]
        //[ValidateAntiForgeryToken]
#else
        [HttpGet]
#endif
        public async Task<IActionResult> AssignUserToClient(ClientUserAssociationViewModel Model)
        {
            // Authorization check
            if (!AuthorizationService.AuthorizeAsync(User, null, new ClientRoleRequirement { RoleEnum = RoleEnum.ClientAdministrator, ClientId = Model.ClientId }).Result)
            {
                return Unauthorized();
            }

            // Validate the request
            // 1 Requested client must exist
            Client RequestedClient = DbContext
                                     .Client
                                     .Where(c => c.Id == Model.ClientId)
                                     .SingleOrDefault();
            if (RequestedClient == null)
            {
                return BadRequest("The requested client does not exist");
            }

            // 2 Requested user must exist
            ApplicationUser RequestedUser = DbContext
                                            .ApplicationUser
                                            .Where(u => u.Id == Model.UserId)
                                            .SingleOrDefault();
            if (RequestedUser == null)
            {
                return BadRequest("The requested user does not exist");
            }

            // 3 Requested User's email must comply with accepted address exception or accepted domain for the client
            string RequestedUserEmail = RequestedUser.NormalizedEmail.ToUpper();
            string RequestedUserEmailDomain = RequestedUserEmail.Substring(RequestedUserEmail.IndexOf('@') + 1).ToUpper();
            bool DomainMatch = RequestedClient.AcceptedEmailDomainList != null && 
                               RequestedClient.AcceptedEmailDomainList.Select(d=>d.ToUpper()).Contains(RequestedUserEmailDomain);
            bool EmailMatch = RequestedClient.AcceptedEmailAddressExceptionList != null && 
                              RequestedClient.AcceptedEmailAddressExceptionList.Select(d => d.ToUpper()).Contains(RequestedUserEmail);
            if (!EmailMatch && !DomainMatch)
            {
                // 412 is Precondition Failed
                return StatusCode(412, "Requested user's email not allowed for this client");
            }

            Client ThisClient = await DbContext.Client.SingleOrDefaultAsync(m => m.Id == Model.ClientId);
            Claim ThisClientMembershipClaim = new Claim("ClientMembership", ThisClient.Name);
            IList<ApplicationUser> AlreadyAssignedUsers = UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim).Result;

            /*
            foreach (var UserInSelectList in Model.ApplicableUsers)
            {
                ApplicationUser ThisUser = DbContext.ApplicationUser.SingleOrDefault(u => u.Id == UserInSelectList.);

                if (!AlreadyAssignedUsers.Select(u=>u.Id).Contains(AssignedUserId))
                {
                    // Create a claim for this user and this client
                    UserManager.AddClaimAsync(ThisUser, ThisClientMembershipClaim).Wait();
                }
                var x = UserInSelectList;
                }
                if (ModelState.IsValid)
                {
                    //UserAuthorizationToClient NewRoleAssignment
                }

            }
            
            return RedirectToAction("Edit", new { Id = Model.ClientId });
            */
            return RedirectToAction("Index");
        }

        /*

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

            // TODO fill this in

            return View();
        }

        // POST: ClientAdmin/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(long id, [Bind("Id,Name,AcceptedEmailDomainList,ParentClientId")] Client client)
        public async Task<IActionResult> Edit(long id, ClientUserAssociationViewModel Model)
        {
            if (id != Model.ClientId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    DbContext.Update(Model.ClientId);
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
        }*/

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
