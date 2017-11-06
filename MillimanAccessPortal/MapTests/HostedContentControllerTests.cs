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
using MillimanAccessPortal.DataQueries;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QlikviewLib;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.HostedContentViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MapTests
{
    public class HostedContentControllerTests
    {
        private IOptions<QlikviewConfig> MockQlikViewConfig { get; set; }
        private Mock<ApplicationDbContext> MockDataContext { get; set; }
        private readonly Mock<UserManager<ApplicationUser>> MockUserManager;
        private readonly ILoggerFactory Logger;

        /// <summary>
        /// Constructor sets up requirements and data for the controller being tested.
        /// </summary>
        public HostedContentControllerTests()
        {
            // Mock requirements for HostedContentController

            MockQlikViewConfig = new Mock<IOptions<QlikviewConfig>>().Object;
            Logger = new LoggerFactory();

            // Configure DataContext required by injected services
            MockDataContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());

            // Configure UserManager to avoid accessing a database
            Mock<IUserStore<ApplicationUser>> MockUserStore = new Mock<IUserStore<ApplicationUser>>();
            MockUserManager = new Mock<UserManager<ApplicationUser>>(MockUserStore.Object, null, null, null, null, null, null, null, null);
        }

        /// <summary>
        /// Test that the Index returns a view containing a list of content items the test user can access.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void Index_ReturnsAViewResult()
        {
            //Arrange
            var ModelToBeReturned = new HostedContentViewModel
            {
                ContentName = "My CCR",
                Url = "Folder/Document.qvw",
                UserGroupId = 1,
                RoleNames = new HashSet<string> { "", "" },
                ClientList = null,
            };

            var MockStandardQueries = new Mock<StandardQueries>(MockDataContext.Object);
            MockStandardQueries.Setup(q => q.GetAuthorizedUserGroupsAndRoles(It.IsAny<string>())).Returns(() => new List<HostedContentViewModel>
            {
                ModelToBeReturned
            });

            HostedContentController sut = new HostedContentController(MockQlikViewConfig, 
                                                                                 MockUserManager.Object, 
                                                                                 Logger, 
                                                                                 null,
                                                                                 MockStandardQueries.Object);
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserName: "test@example.com");

            // Act
            var view = sut.Index();

            // Assert
            Assert.IsType(typeof(ViewResult), view);

            ViewResult viewResult = view as ViewResult;
            Assert.IsType(typeof(List<HostedContentViewModel>), viewResult.Model);

            List<HostedContentViewModel> model = (List<HostedContentViewModel>)viewResult.Model;
            Assert.Equal(1, model.Count);

            HostedContentViewModel modelZero = model[0];
            Assert.Equal(ModelToBeReturned.ContentName, modelZero.ContentName);
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
