using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapTests
{
    class MockUserManager
    {
        public static Mock<UserManager<ApplicationUser>> New(Mock<ApplicationDbContext> MockDbContextArg)
        {
            Mock<UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>> UserStore = MockUserStore.New(MockDbContextArg);
            Mock<UserManager<ApplicationUser>> ReturnMockUserManager = new Mock<UserManager<ApplicationUser>>(UserStore.Object, null, null, null, null, null, null, null, null);

            // Re-create methods we're calling against the UserManager
            // User-related methods
            ReturnMockUserManager.Setup(m => m.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns<ClaimsPrincipal>(cp => UserStore.Object.FindByNameAsync(cp.Identity.Name, CancellationToken.None).Result.UserName);
            ReturnMockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync<ClaimsPrincipal, UserManager<ApplicationUser>, ApplicationUser>(cp => UserStore.Object.FindByNameAsync(cp.Identity.Name, CancellationToken.None).Result);
            ReturnMockUserManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync<string, UserManager<ApplicationUser>, ApplicationUser>(name => UserStore.Object.FindByNameAsync(name, CancellationToken.None).Result);
            ReturnMockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync<string, UserManager<ApplicationUser>, ApplicationUser>(id => UserStore.Object.FindByIdAsync(id, CancellationToken.None ).Result);
            ReturnMockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync<string, UserManager<ApplicationUser>, ApplicationUser>(email => UserStore.Object.FindByEmailAsync(email, CancellationToken.None).Result);
            ReturnMockUserManager.Setup(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync<string, string, UserManager<ApplicationUser>, ApplicationUser>((string provider, string key) => UserStore.Object.FindByLoginAsync(provider, key).Result);
            // TODO: Make this method actually try the password. Currently it will always return true.
            ReturnMockUserManager.Setup(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync<ApplicationUser, string, UserManager<ApplicationUser>, bool>((user, password) => true);
            ReturnMockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync<ApplicationUser, string, UserManager<ApplicationUser>, IdentityResult>((user, password) => UserStore.Object.CreateAsync(user, CancellationToken.None).Result);

            // Role-related methods
            ReturnMockUserManager.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync<ApplicationUser, string, UserManager<ApplicationUser>, bool>((user, role) => UserStore.Object.IsInRoleAsync(user, role, CancellationToken.None).Result);
            ReturnMockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Callback<ApplicationUser, string>((user, role) => UserStore.Object.AddToRoleAsync(user, role, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.RemoveFromRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Callback<ApplicationUser, string>((user, role) => UserStore.Object.RemoveFromRoleAsync(user, role, CancellationToken.None));

            // Claims-related methods
            ReturnMockUserManager.Setup(m => m.GetClaimsAsync(It.IsAny<ApplicationUser>())).ReturnsAsync<ApplicationUser, UserManager<ApplicationUser>, IList<Claim>>(user => UserStore.Object.GetClaimsAsync(user, CancellationToken.None).Result);
            ReturnMockUserManager.Setup(m => m.AddClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>())).Callback<ApplicationUser, Claim>((user, claim) => UserStore.Object.AddClaimsAsync(user, new List<Claim>() { claim }, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>())).Callback<ApplicationUser, IEnumerable<Claim>>((user, claims) => UserStore.Object.AddClaimsAsync(user, claims, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.RemoveClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>())).Callback<ApplicationUser, Claim>((user, claim) => UserStore.Object.RemoveClaimsAsync(user, new List<Claim>() { claim }, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.RemoveClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>())).Callback<ApplicationUser, IEnumerable<Claim>>((user, claims) => UserStore.Object.RemoveClaimsAsync(user, claims, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.GetUsersForClaimAsync(It.IsAny<Claim>())).ReturnsAsync<Claim, UserManager<ApplicationUser>, IList<ApplicationUser>>(claim => UserStore.Object.GetUsersForClaimAsync(claim, CancellationToken.None).Result);
            
            Claim thisClaim = new Claim("type", "value");
            var Claims = new List<Claim>() { thisClaim };

            return ReturnMockUserManager;
        }
    }
}
