/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Perform unit tests against HostedContentController
 * DEVELOPER NOTES: <What future developers need to know.>
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Moq;
using Xunit;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.HostedContentViewModels;
using MillimanAccessPortal.Authorization;

namespace MapTests
{
    public class HostedContentControllerTests
    {
        /// <summary>
        /// Constructor sets up requirements and data for the controller being tested.
        /// </summary>
        public HostedContentControllerTests()
        {}

        /// <summary>
        /// Test that the Index returns a view containing a list of content items the test user can access.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void Index_ReturnsAViewResult()
        {
            // Reference https://msdn.microsoft.com/en-us/library/dn314429(v=vs.113).aspx

            #region Arrange
            // initialize dependencies
            TestInitialization TestResources = new TestInitialization();

            // initialize data
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });

            // Create the system under test (sut)
            HostedContentController sut = new HostedContentController(TestResources.QlikViewConfigObject,
                                                                      TestResources.UserManagerObject,
                                                                      TestResources.LoggerFactory,
                                                                      TestResources.DbContextObject,
                                                                      TestResources.QueriesObj,
                                                                      TestResources.AuthorizationService);

            // For illustration only, the same result comes from any of the following 3 techniques:
            // This one should never throw even if the user name is not in the context data
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: "test1");
            // Following 2 throw if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.DbContextObject.ApplicationUser.Where(u => u.UserName=="test1").First().UserName);
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.UserManagerObject.FindByNameAsync("test1").Result.UserName);
            #endregion

            #region Act
            // invoke the controller action to be tested
            var view = sut.Index();
            #endregion

            #region Assert
            Assert.IsType(typeof(ViewResult), view);

            ViewResult viewResult = view as ViewResult;
            Assert.IsType(typeof(List<HostedContentViewModel>), viewResult.Model);

            List<HostedContentViewModel> ModelReturned = (List<HostedContentViewModel>)viewResult.Model;
            Assert.Equal(1, ModelReturned.Count);

            Assert.Equal(TestResources.DbContextObject.RootContentItem.FirstOrDefault().ContentName, ModelReturned[0].ContentName);
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

        public Mock<IAuthorizationService> AuthorizationServiceMockExtensionFactory<T1>(AuthorizationResult Result1) 
            where T1 : MapAuthorizationRequirementBase
        {
            var mockRepository = new Moq.MockRepository(Moq.MockBehavior.Strict);
            var mockFactory = mockRepository.Create<IAuthorizationService>();
            var ClaimsPrincipal = mockRepository.Create<ClaimsPrincipal>();
            mockFactory.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<T1>())).ReturnsAsync(Result1);
            return mockFactory;
        }

        /// <summary>
        /// To be used where the handler is expected to service 2 separate requirement types in the system under test
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="Result1"></param>
        /// <param name="Result2"></param>
        /// <returns></returns>
        public static Mock<IAuthorizationService> AuthorizationServiceMockExtensionFactory<T1,T2>(AuthorizationResult Result1, AuthorizationResult Result2) 
            where T1 : MapAuthorizationRequirementBase
            where T2 : MapAuthorizationRequirementBase
        {
            var mockRepository = new Moq.MockRepository(Moq.MockBehavior.Strict);
            var mockFactory = mockRepository.Create<IAuthorizationService>();
            var ClaimsPrincipal = mockRepository.Create<ClaimsPrincipal>();
            mockFactory.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<T1>())).ReturnsAsync(Result1);
            mockFactory.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<T2>())).ReturnsAsync(Result2);
            return mockFactory;
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
            mockFactory.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<MapAuthorizationRequirementBase>>())).ReturnsAsync(AuthorizationResult.Success());
            return mockFactory;
        }

        public Mock<IAuthorizationService> AuthorizationServiceMockExtensionFactory_RequirementList_Failure()
        {
            var mockRepository = new Moq.MockRepository(Moq.MockBehavior.Strict);
            var mockFactory = mockRepository.Create<IAuthorizationService>();
            var ClaimsPrincipal = mockRepository.Create<ClaimsPrincipal>();
            mockFactory.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<MapAuthorizationRequirementBase>>())).ReturnsAsync(AuthorizationResult.Failed());
            return mockFactory;
        }
    }
}

#if false
#endif