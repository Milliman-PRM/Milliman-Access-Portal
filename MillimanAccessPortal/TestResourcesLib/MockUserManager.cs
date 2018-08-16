using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestResourcesLib
{
    public class MockUserManager
    {
        public static Mock<UserManager<ApplicationUser>> New(Mock<ApplicationDbContext> MockDbContextArg)
        {
            Mock<UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>> UserStore = MockUserStore.New(MockDbContextArg);
            Mock<UserManager<ApplicationUser>> ReturnMockUserManager = new Mock<UserManager<ApplicationUser>>(UserStore.Object, null, null, null, null, null, null, null, null);

            // Re-create methods we're calling against the UserManager
            // User-related methods
            ReturnMockUserManager.Setup(m => m.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns<ClaimsPrincipal>(cp => cp.Identity.Name);

            ReturnMockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).Returns(async (ClaimsPrincipal cp) => await UserStore.Object.FindByNameAsync(cp.Identity.Name, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).Returns(async (string name) => await UserStore.Object.FindByNameAsync(name, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).Returns(async (string id) => await UserStore.Object.FindByIdAsync(id, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).Returns(async (string email) => await UserStore.Object.FindByEmailAsync(email, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(async (string provider, string key) => await UserStore.Object.FindByLoginAsync(provider, key));
            ReturnMockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(async (ApplicationUser user, string password) => await UserStore.Object.CreateAsync(user, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync<UserManager<ApplicationUser>, IdentityResult>(IdentityResult.Success);

            // Role-related methods
            ReturnMockUserManager.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(async (ApplicationUser user, string role) => await UserStore.Object.IsInRoleAsync(user, role, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Callback((ApplicationUser user, string role) => UserStore.Object.AddToRoleAsync(user, role, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.RemoveFromRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Callback((ApplicationUser user, string role) => UserStore.Object.RemoveFromRoleAsync(user, role, CancellationToken.None));

            // Claims-related methods
            ReturnMockUserManager.Setup(m => m.GetClaimsAsync(It.IsAny<ApplicationUser>())).Returns(async (ApplicationUser user) => await UserStore.Object.GetClaimsAsync(user, CancellationToken.None));
            ReturnMockUserManager.Setup(m => m.AddClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>())).Returns(async (ApplicationUser user, Claim claim) => await Task.FromResult(AddClaims(UserStore.Object, user, new List<Claim>() { claim })));
            ReturnMockUserManager.Setup(m => m.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>())).Returns(async (ApplicationUser user, IEnumerable<Claim> claims) => await Task.FromResult(AddClaims(UserStore.Object, user, claims)));

            ReturnMockUserManager.Setup(m => m.RemoveClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>())).Returns(async (ApplicationUser user, Claim claim) => await Task.FromResult(RemoveClaims(UserStore.Object, user, new List<Claim> { claim })));
            ReturnMockUserManager.Setup(m => m.RemoveClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>())).Returns(async (ApplicationUser user, IEnumerable<Claim> claims) => await Task.FromResult(RemoveClaims(UserStore.Object, user, claims)));
            ReturnMockUserManager.Setup(m => m.GetUsersForClaimAsync(It.IsAny<Claim>())).Returns(async (Claim claim) => await UserStore.Object.GetUsersForClaimAsync(claim, CancellationToken.None));

            // Password-related methods
            // Return true if the password is not null
            // As of 8/2018 there is no password validation built into anthing in this class so tests should no rely on it.
            ReturnMockUserManager.Setup(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(async (ApplicationUser usr, string pwd) => await Task.FromResult(usr.PasswordHash == pwd + "xyz"));
            ReturnMockUserManager.Setup(m => m.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>())).Returns(async (ApplicationUser usr, string token, string pwd) =>
            {
                usr.PasswordHash = pwd + "xyz";
                return await Task.FromResult(IdentityResult.Success);
            });
            ReturnMockUserManager.Setup(m => m.AddPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(async (ApplicationUser usr, string pw) =>
            {
                usr.PasswordHash = pw + "xyz";  // useful enough for testing
                return await Task.FromResult(IdentityResult.Success);
            });
            ReturnMockUserManager.Setup(m => m.ChangePasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>())).Returns(async(ApplicationUser usr, string oldpw, string newpw) =>
            {
                if (usr.PasswordHash == oldpw + "xyz")  // valid oldpw
                {
                    usr.PasswordHash = newpw + "xyz";
                    return await Task.FromResult(IdentityResult.Success);
                }
                else  // invalid oldpw
                {
                    return await Task.FromResult(IdentityResult.Failed(new IdentityError {Code="", Description = "oldpw incorrect" }));
                }
            });

            // Account Enablement methods
            ReturnMockUserManager.Setup(m => m.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(async (ApplicationUser usr, string token) => await Task.FromResult(IdentityResult.Success));

            return ReturnMockUserManager;
        }

        /// <summary>
        /// Encapsulate behavior for adding claims into a method shared by both calls above
        /// </summary>
        /// <param name="userStoreArg"></param>
        /// <param name="userArg"></param>
        /// <param name="claimsArg"></param>
        /// <returns></returns>
        public static IdentityResult AddClaims(UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long> userStoreArg, ApplicationUser userArg, IEnumerable<Claim> claimsArg)
        {

            int beforeCount = Queryable.Count(userStoreArg.Context.UserClaims);
            int addingCount = 0;

            // Count how many elements will be added, to get a precise expected final count
            foreach (Claim inputClaim in claimsArg)
            {
                if (!Queryable.Contains(userStoreArg.Context.UserClaims, BuildClaim(userArg, inputClaim)))
                    addingCount++;
            }

            int expectedFinalCount = beforeCount + addingCount;

            userStoreArg.AddClaimsAsync(userArg, claimsArg, CancellationToken.None);

            int finalCount = Queryable.Count(userStoreArg.Context.UserClaims);

            if (finalCount == expectedFinalCount)
                return IdentityResult.Success;
            else
                return IdentityResult.Failed();
        }

        public static IdentityResult RemoveClaims(UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long> userStoreArg, ApplicationUser userArg, IEnumerable<Claim> claimsArg)
        {
            int beforeCount = Queryable.Count(userStoreArg.Context.UserClaims);
            int removingCount = claimsArg.Count();
            int expectedFinalCount = beforeCount - removingCount;

            userStoreArg.RemoveClaimsAsync(userArg, claimsArg, CancellationToken.None);

            int finalCount = Queryable.Count(userStoreArg.Context.UserClaims);

            if (finalCount == expectedFinalCount)
            {
                return IdentityResult.Success;
            }
            else
                return IdentityResult.Failed();
        }

        public static IdentityUserClaim<long> BuildClaim(ApplicationUser userArg, Claim claimArg)
        {
            return new IdentityUserClaim<long> { UserId = userArg.Id, ClaimType = claimArg.Type, ClaimValue = claimArg.Value };
        }
    }
}
