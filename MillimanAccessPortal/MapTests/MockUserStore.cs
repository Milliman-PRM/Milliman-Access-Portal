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

namespace MapTests
{
    internal class MockUserStore
    {
        //internal static Mock<IUserStore<ApplicationUser>> New(Mock<ApplicationDbContext> Context)
        internal static Mock<UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>> New(Mock<ApplicationDbContext> Context)
        {
            //Mock<IUserStore<ApplicationUser>> NewStore = new Mock<IUserStore<ApplicationUser>>();
            Mock<UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>> NewStore = new Mock<UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>>() {CallBase=true };

            // Setup mocked object methods to interact with persisted data
            NewStore.Setup(d => d.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>())).Callback<ApplicationUser, CancellationToken>((au, ct) => Context.Object.ApplicationUser.Add(au));
            NewStore.Setup(d => d.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync<string, CancellationToken, UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>, ApplicationUser>((id, ct) => Context.Object.ApplicationUser.SingleOrDefault(au => au.Id == long.Parse(id)));
            NewStore.Setup(d => d.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync<string, CancellationToken, UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>, ApplicationUser>((nm, ct) => Context.Object.ApplicationUser.SingleOrDefault(au => au.UserName == nm));

            NewStore.Setup(d => d.GetUsersForClaimAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>())).ReturnsAsync<Claim, UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, long>, IList<ApplicationUser>>((Claim claim) => 
                                            Context.Object.UserClaims.Join(
                                            Context.Object.Users,
                                            cl => cl.UserId,
                                            us => us.Id,
                                            (cl, us) => new { cl, us }
                                            ).Where(dat => dat.cl.ClaimValue == claim.Value && dat.cl.ClaimType == ClaimNames.ClientMembership.ToString())
                                            .Select(usrs => usrs.us).ToList()
                    );
            // more?

            return NewStore;
        }

    }
}
