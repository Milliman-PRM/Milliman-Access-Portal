using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

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
            ReturnMockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync();
            ReturnMockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync();
            ReturnMockUserManager.Setup(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync();
            ReturnMockUserManager.Setup(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync();
            ReturnMockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync();

            // Role-assignment methods
            ReturnMockUserManager.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync();
            ReturnMockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync();
            ReturnMockUserManager.Setup(m => m.RemoveFromRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync();

            // Claims-related methods
            ReturnMockUserManager.Setup(m => m.GetClaimsAsync(It.IsAny<ApplicationUser>())).ReturnsAsync();
            ReturnMockUserManager.Setup(m => m.AddClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>())).ReturnsAsync();
            ReturnMockUserManager.Setup(m => m.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>())).ReturnsAsync();
            ReturnMockUserManager.Setup(m => m.RemoveClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>())).ReturnsAsync();
            ReturnMockUserManager.Setup(m => m.RemoveClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>())).ReturnsAsync();
            ReturnMockUserManager.Setup(m => m.GetUsersForClaimAsync(It.IsAny<Claim>())).ReturnsAsync();

            /*ReturnMockUserManager.Setup(m => m.GetUsersForClaimAsync(It.IsAny<Claim>()))
                .ReturnsAsync
                    ((Claim claim) => 
                       DbContextObject.UserClaims.Join(
                           DbContextObject.Users,
                           cl => cl.UserId,
                           us => us.Id,
                           (cl, us) => new { cl, us }
                           ).Where(dat => dat.cl.ClaimValue == claim.Value && dat.cl.ClaimType == ClaimNames.ClientMembership.ToString())
                           .Select(usrs => usrs.us).ToList()
                    );*/

            return ReturnMockUserManager;
        }
    }
}
