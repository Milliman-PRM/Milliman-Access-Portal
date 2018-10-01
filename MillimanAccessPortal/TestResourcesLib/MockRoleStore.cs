using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using System.Threading;
using System.Linq;

namespace TestResourcesLib
{
    public class MockRoleStore
    {
        public static Mock<IRoleStore<ApplicationRole>> NewStore(Mock<ApplicationDbContext> Context)
        {
            Mock<IRoleStore<ApplicationRole>> ReturnRoleStore = new Mock<IRoleStore<ApplicationRole>>();

            // Provide mocked methods required by tests
            ReturnRoleStore.Setup(s => s.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync<string, CancellationToken, IRoleStore<ApplicationRole>, ApplicationRole>((nm, ct) => Context.Object.ApplicationRole.SingleOrDefault(ar => ar.Name == nm));
            ReturnRoleStore.Setup(s => s.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync<string, CancellationToken, IRoleStore<ApplicationRole>, ApplicationRole>((id, ct) => Context.Object.ApplicationRole.SingleOrDefault(ar => ar.Id.ToString() == id));

            return ReturnRoleStore;
        }

    }
}
