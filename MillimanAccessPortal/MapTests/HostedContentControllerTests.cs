/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Perform unit tests against HostedContentController
 * DEVELOPER NOTES: <What future developers need to know.>
 */
using System;
using System.Collections.Generic;
using Moq;
using Xunit;
using MillimanAccessPortal.Controllers;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QlikviewLib;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MapTests
{
    public class HostedContentControllerTests
    {
        private IOptions<QlikviewConfig> QlikViewConfig { get; set; }
        private Mock<ApplicationDbContext> DataContext { get; set; }
        private readonly Mock<UserManager<ApplicationUser>> UserManager;
        private readonly ILoggerFactory Logger;
        private readonly IServiceProvider ServiceProvider;
        private HostedContentController TestController;

        /// <summary>
        /// Constructor sets up requirements and data for the controller being tested.
        /// </summary>
        public HostedContentControllerTests()
        {
            // Mock requirements for HostedContentController

            QlikViewConfig = new Mock<IOptions<QlikviewConfig>>().Object;
            Logger = new LoggerFactory();
            ServiceProvider = new Mock<IServiceProvider>().Object;

            // Configure DataContext with mock data
            DataContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
            // Needed: 1 user, 2 reports, 1 user authorization to report

            // Create a single test user
            DataContext.Object.Users.Add(new ApplicationUser()
            {
                Id = 1,
                Email = "test@example.com",
                UserName = "test@example.com",
                NormalizedEmail = "test@example.com",
                NormalizedUserName = "test@example.com"
            });

            // Populate standard roles
            DataContext.Object.Roles.Add(new ApplicationRole()
            {
                Id = 1,
                Name = "Super User",
                RoleEnum = RoleEnum.SuperUser,
                NormalizedName = "super user"
            }
            );
            DataContext.Object.Roles.Add(new ApplicationRole()
            {
                Id = 2,
                Name = "Client Administrator",
                RoleEnum = RoleEnum.ClientAdministrator,
                NormalizedName = "client administrator"
            }
            );
            DataContext.Object.Roles.Add(new ApplicationRole()
            {
                Id = 3,
                Name = "User Manager",
                RoleEnum = RoleEnum.UserManager,
                NormalizedName = "user manager"
            }
            );
            DataContext.Object.Roles.Add(new ApplicationRole()
            {
                Id = 4,
                Name = "Content Publisher",
                RoleEnum = RoleEnum.ContentPublisher,
                NormalizedName = "content publisher"
            }
            );
            DataContext.Object.Roles.Add(new ApplicationRole()
            {
                Id = 5,
                Name = "Content User",
                RoleEnum = RoleEnum.ContentUser,
                NormalizedName = "content user"
            }
            );

            // Mock test clients
            DataContext.Object.Client.Add(new Client()
            {
                Id = 1,
                Name = "Test Client",
                AcceptedEmailDomainList = new string[] { "@milliman.com", "@example.com" }
            });
            DataContext.Object.Client.Add(new Client()
            {
                Id = 2,
                Name = "Other Client",
                AcceptedEmailDomainList = new string[] { "@google.com" }
            });

            // Mock content type
            DataContext.Object.ContentType.Add(new ContentType {
                Id = 1,
                Name = "Qlikview",
                CanReduce = true
            });

            // Mock content items
            DataContext.Object.RootContentItem.Add(new RootContentItem() {
                Id = 1,
                ContentName = "Test Qlikview Report 1",
                ContentTypeId = 1,
                ClientIdList = new long[] { 1 }
            });
            DataContext.Object.RootContentItem.Add(new RootContentItem()
            {
                Id = 2,
                ContentName = "Test Qlikview Report 2",
                ContentTypeId = 1,
                ClientIdList = new long[] { 1 }
            });
            DataContext.Object.RootContentItem.Add(new RootContentItem()
            {
                Id = 3,
                ContentName = "Test Qlikview Report 3",
                ContentTypeId = 1,
                ClientIdList = new long[] { 1 }
            });

            // Mock content user groups
            DataContext.Object.ContentItemUserGroup.Add(new ContentItemUserGroup()
            {
                Id = 1,
                GroupName = "Populated group",
                ContentInstanceUrl = "https://google.com",
                ClientId = 1,
                RootContentItemId = 1
            });

            // Add test user to group
            DataContext.Object.UserRoleForContentItemUserGroup.Add(new UserInContentItemUserGroup
            {
                Id = 1,
                ContentItemUserGroupId = 1,
                UserId = 1,
                RoleId = 5 // Content User
            });

            // TODO figure out this issue:
            DataContext.Setup(x => x.UserRoleForClient).Returns(new DbSet<UserAuthorizationToClient>());
            DataContext.Setup(x => x.UserRoleForContentItemUserGroup).Returns(DataContext.Object.UserRoleForContentItemUserGroup);

            // Configure UserManager to return valid values
            var UserStore = new Mock<IUserStore<ApplicationUser>>();
            UserManager = new Mock<UserManager<ApplicationUser>>(UserStore.Object, null, null, null, null, null, null, null, null);
            UserManager.Setup(manager => manager.GetUserName(It.IsAny<ClaimsPrincipal>()))
                       .Returns("test@example.com");

            TestController = new HostedContentController(QlikViewConfig, UserManager.Object, Logger, DataContext.Object);
        }
        
        /// <summary>
        /// Test that the Index returns a view containing a list of content items the test user can access.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void Index_ReturnsAViewResult()
        {
            var view = TestController.Index();

            // Make assertions
            //Assert.Equal(HttpStatusCode.OK, )

            Assert.Null(view);

            // TODO: Boilerplate. Remove when the test is written.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test that WebHostedContent(id) results in an error page when the user is not authorized to the content
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void WebHostedContent_ErrorWhenNotAuthorized()
        {
            // Attempt to load the content view for unauthorized content

            // Test that the ErrorController was loaded instead

            // TODO: Boilerplate. Remove when the test is written.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test that WebHostedContent(id) displays the content view when the user is authorized to the content
        /// </summary>
        /// <returns></returns>
        [Fact]
        public Task WebHostedContent_DisplaysWhenAuthorized()
        {
            // Attempt to load the content view for authorized content

            // Test that the content view was returned

            // TODO: Boilerplate. Remove when the test is written.
            throw new NotImplementedException();
        }
    }
}
