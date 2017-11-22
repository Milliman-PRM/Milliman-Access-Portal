/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Perform unit tests against HostedContentController
 * DEVELOPER NOTES: <What future developers need to know.>
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using QlikviewLib;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.HostedContentViewModels;
using MillimanAccessPortal.Authorization;

namespace MapTests
{
    public class HostedContentControllerTests
    {
        private IOptions<QlikviewConfig> MockQlikViewConfig { get; set; }
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

            #region Arrange
            var MockContext = TestInitialization.GenerateTestDataset(new DataSelection[] { DataSelection.Basic }).Object;

            var x = AuthorizationServiceMockExtensionFactory_RequirementList_Failure();

            StandardQueries QueriesObj = new StandardQueries(MockContext, MockUserManager.Object);

            HostedContentController sut = new HostedContentController(MockQlikViewConfig, 
                                                                      MockUserManager.Object, 
                                                                      Logger, 
                                                                      MockContext,
                                                                      QueriesObj,
                                                                      x.Object);

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

        public Mock<IAuthorizationService> AuthorizationServiceMockExtensionFactory_Success()
        {
            var mockRepository = new Moq.MockRepository(Moq.MockBehavior.Strict);
            var mockFactory = mockRepository.Create<IAuthorizationService>();
            var ClaimsPrincipal = mockRepository.Create<ClaimsPrincipal>();
            mockFactory.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IAuthorizationRequirement>())).ReturnsAsync(AuthorizationResult.Success());
            return mockFactory;
        }

        public Mock<IAuthorizationService> AuthorizationServiceMockExtensionFactory_Failure()
        {
            var mockRepository = new Moq.MockRepository(Moq.MockBehavior.Strict);
            var mockFactory = mockRepository.Create<IAuthorizationService>();
            var ClaimsPrincipal = mockRepository.Create<ClaimsPrincipal>();
            mockFactory.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IAuthorizationRequirement>())).ReturnsAsync(AuthorizationResult.Failed());
            return mockFactory;
        }

        public Mock<IAuthorizationService> AuthorizationServiceMockExtensionFactory_RequirementList_Success()
        {
            var mockRepository = new Moq.MockRepository(Moq.MockBehavior.Strict);
            var mockFactory = mockRepository.Create<IAuthorizationService>();
            var ClaimsPrincipal = mockRepository.Create<ClaimsPrincipal>();
            mockFactory.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>())).ReturnsAsync(AuthorizationResult.Success());
            return mockFactory;
        }

        public Mock<IAuthorizationService> AuthorizationServiceMockExtensionFactory_RequirementList_Failure()
        {
            var mockRepository = new Moq.MockRepository(Moq.MockBehavior.Strict);
            var mockFactory = mockRepository.Create<IAuthorizationService>();
            var ClaimsPrincipal = mockRepository.Create<ClaimsPrincipal>();
            mockFactory.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>())).ReturnsAsync(AuthorizationResult.Failed());
            return mockFactory;
        }
    }
}

#if false
#endif