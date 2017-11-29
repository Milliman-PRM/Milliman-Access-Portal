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

namespace MapTests
{
    internal class MockUserStore
    {
        internal static Mock<IUserStore<ApplicationUser>> New(Mock<ApplicationDbContext> Context)
        {
            Mock<IUserStore<ApplicationUser>> NewStore = new Mock<IUserStore<ApplicationUser>>();

            // Setup mocked object methods to interact with persisted data
            NewStore.Setup(d => d.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>())).Callback<ApplicationUser, CancellationToken>((au, ct) => Context.Object.ApplicationUser.Add(au));
            NewStore.Setup(d => d.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync<string, CancellationToken, IUserStore<ApplicationUser>, ApplicationUser>((Id, ct) => Context.Object.ApplicationUser.SingleOrDefault(au => au.UserName == Id));
            // more?

            return NewStore;
        }

    }
}
