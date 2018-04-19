using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Threading.Tasks;

namespace TestResourcesLib
{
    internal class MockUserStore
    {
        //internal static Mock<IUserStore<ApplicationUser>> New(Mock<ApplicationDbContext> Context)
        internal static Mock<UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>> New(Mock<ApplicationDbContext> Context)
        {
            //Mock<IUserStore<ApplicationUser>> NewStore = new Mock<IUserStore<ApplicationUser>>();
            Mock<UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>> NewStore = new Mock<UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>>(Context.Object, null) {CallBase=true };
            
            // Setup mocked object methods to interact with persisted data
            NewStore.Setup(d => d.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>())).Callback<ApplicationUser, CancellationToken>((au, ct) => NewStore.Object.Context.ApplicationUser.Add(au));
            NewStore.Setup(d => d.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync<string, CancellationToken, UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>, ApplicationUser>((id, ct) => Context.Object.ApplicationUser.SingleOrDefault(au => au.Id == long.Parse(id)));
            NewStore.Setup(d => d.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync<string, CancellationToken, UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>, ApplicationUser>((nm, ct) => Context.Object.ApplicationUser.SingleOrDefault(au => au.UserName == nm));
            NewStore.Setup(d => d.GetUsersForClaimAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>())).ReturnsAsync<Claim, CancellationToken, UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>, IList<ApplicationUser>>((Claim claim, CancellationToken ct) =>
                                            Context.Object.UserClaims.Join(
                                            Context.Object.Users,
                                            cl => cl.UserId,
                                            us => us.Id,
                                            (cl, us) => new { cl, us }
                                            ).Where(dat => dat.cl.ClaimValue == claim.Value && dat.cl.ClaimType == claim.Type)
                                            .Select(usrs => usrs.us).ToList()
                                        );
            //, UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>, IdentityResult
            NewStore.Setup(d => d.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>(), It.IsAny<CancellationToken>())).Returns<ApplicationUser, IEnumerable<Claim>, CancellationToken>((usr, claims, ct) => 
                Context.Object.UserClaims.AddRangeAsync(BuildClaimList(usr, claims), ct)
            );
            NewStore.Setup(d => d.RemoveClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>(), It.IsAny<CancellationToken>())).Returns<ApplicationUser, IEnumerable<Claim>, CancellationToken>((usr, claims, ct) =>
                {
                    int initialCount = Context.Object.UserClaims.Count();
                    int removeCount = claims.Count();
                    int expectedFinalCount = initialCount - removeCount;

                    List<IdentityUserClaim<long>> claimsList = BuildClaimList(usr, claims, Context.Object);

                    Context.Object.UserClaims.RemoveRange(claimsList);

                    int finalCount = Context.Object.UserClaims.Count();
                    
                    if (finalCount == expectedFinalCount)
                        return Task.CompletedTask;
                    else
                        return Task.FromException(new Exception("Failed to remove claims"));                    
                }
            );
            NewStore.Setup(d => d.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync<string, CancellationToken, UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>, ApplicationUser>((em, ct) => Context.Object.ApplicationUser.SingleOrDefault(au => au.Email == em));
            NewStore.Setup(d => d.GetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>())).ReturnsAsync<ApplicationUser, CancellationToken, UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>, string>((usr, ct) => Context.Object.Users.SingleOrDefault(ausr => ausr.Id == usr.Id).UserName);

            /*
             * Because UserClaims contains IdentityUserClaims but GetClaimsAsync needs to return a list of generic Claim objects,
             *      this mock requires a more complicated return block than most others.
             */
            NewStore.Setup(d => d.GetClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync<ApplicationUser, CancellationToken, UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>, IList<Claim>>
                    ((usr, ct) => 
                        {
                            var Claims = Context.Object.UserClaims.Where(uc => uc.UserId == usr.Id).ToList();
                            List<Claim> claimList = new List<Claim>();
                            foreach (IdentityUserClaim<long> claim in Claims)
                            {
                                claimList.Add(claim.ToClaim());
                            }
                            return claimList;
                        });

            // more?

            return NewStore;
        }

        /// <summary>
        /// Convenience method to build a list of IdentityUserClaim objects, which simplifies the callbacks for AddClaimsAsync and RemoveClaimsAsync above
        /// </summary>
        /// <param name="userArg">The targeted user</param>
        /// <param name="claimsArg">The list of generic claim objects to transform to IdentityUserClaim objects</param>
        /// <param name="contextArg">The Context to search for an existing claim. Useful for building claims to be removed.</param>
        /// <returns></returns>
        internal static List<IdentityUserClaim<long>> BuildClaimList(ApplicationUser userArg, IEnumerable<Claim> claimsArg, ApplicationDbContext contextArg = null)
        {
            List<IdentityUserClaim<long>> returnList = new List<IdentityUserClaim<long>>();

            foreach (Claim claimInput in claimsArg)
            {
                int idVal = 0;
                if (contextArg != null)
                {
                    idVal = contextArg.UserClaims.FirstOrDefault(uc => uc.ClaimValue == claimInput.Value &&
                                                                       uc.ClaimType == claimInput.Type &&
                                                                       uc.UserId == userArg.Id).Id;
                }
                returnList.Add(new IdentityUserClaim<long> { Id = idVal, UserId = userArg.Id, ClaimType = claimInput.Type, ClaimValue = claimInput.Value });
            }

            return returnList;
        }

    }
}
