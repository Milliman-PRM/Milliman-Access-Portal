/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Perform unit tests against HostedContentController
 * DEVELOPER NOTES: <What future developers need to know.>
 */
using System;
using System.Linq;
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
using Microsoft.EntityFrameworkCore.Query;
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
            MockUserManager.Setup(m => m.GetUserName(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns("test1");
        }

        /// <summary>
        /// Test that the Index returns a view containing a list of content items the test user can access.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void Index_ReturnsAViewResult()
        {
            // Reference https://msdn.microsoft.com/en-us/library/dn314429(v=vs.113).aspx
            
            #region Mock Context
            // Had to implement a parameterless constructor in the context class, I hope this doesn't cause any problem in EF
            var MockContext = TestInitialization.GenerateStandardDataset().Object;

            StandardQueries QueriesObj = new StandardQueries(MockContext, MockUserManager.Object);

            HostedContentController sut = new HostedContentController(MockQlikViewConfig, 
                                                                      MockUserManager.Object, 
                                                                      Logger, 
                                                                      MockContext,
                                                                      QueriesObj);

            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserName: MockContext.ApplicationUser.First().UserName);
            #endregion

            #region Act
            var view = sut.Index();
            #endregion

            #region Assert
            Assert.IsType(typeof(ViewResult), view);

            ViewResult viewResult = view as ViewResult;
            Assert.IsType(typeof(List<HostedContentViewModel>), viewResult.Model);

            List<HostedContentViewModel> ModelReturned = (List<HostedContentViewModel>)viewResult.Model;
            Assert.Equal(1, ModelReturned.Count);

            Assert.Equal(MockContext.RootContentItem.FirstOrDefault().ContentName, ModelReturned[0].ContentName);
            #endregion
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
