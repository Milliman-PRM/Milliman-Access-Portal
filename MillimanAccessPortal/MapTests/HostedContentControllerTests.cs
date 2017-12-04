/*
 * CODE OWNERS: Ben Wyatt, Tom Puckett
 * OBJECTIVE: Perform unit tests against HostedContentController
 * DEVELOPER NOTES: <What future developers need to know.>
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.HostedContentViewModels;
using MapDbContextLib.Context;

namespace MapTests
{
    public class HostedContentControllerTests
    {
        /// <summary>
        /// Test that the Index returns a view containing a list of content items the test user can access.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void Index_ReturnsAViewResult()
        {
            #region Arrange
            // initialize dependencies
            TestInitialization TestResources = new TestInitialization();

            // initialize data
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });

            // Create the system under test (sut)
            HostedContentController sut = new HostedContentController(TestResources.QvConfig,
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
            #region Arrange
            // initialize dependencies
            TestInitialization TestResources = new TestInitialization();

            // initialize data
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });

            // Create the system under test (sut)
            HostedContentController sut = new HostedContentController(TestResources.QvConfig,
                                                                      TestResources.UserManagerObject,
                                                                      TestResources.LoggerFactory,
                                                                      TestResources.DbContextObject,
                                                                      TestResources.QueriesObj,
                                                                      TestResources.AuthorizationService);

            // For illustration only, the same result comes from any of the following 3 techniques:
            // This one should never throw even if the user name is not in the context data
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: "test1");
            // Following 2 throw if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.DbContextObject.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.UserManagerObject.FindByNameAsync("test1").Result.UserName);
            sut.ControllerContext.ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor { ActionName = "WebHostedContent" };
            sut.HttpContext.Session = new MockSession();
            #endregion


            #region Act
            var view = sut.WebHostedContent(3); // User "test1" is not authorized to RootContentItem for ContentItemUserGroup w/ ID 3
            #endregion

            #region Assert
            // Test that a 500 error was returned instead of the content
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        /// <summary>
        /// Test that WebHostedContent(id) displays the content view when the user is authorized to the content
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void WebHostedContent_DisplaysWhenAuthorized()
        {
            #region Arrange
            // initialize dependencies
            TestInitialization TestResources = new TestInitialization();

            // initialize data
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });

            // Create the system under test (sut)
            HostedContentController sut = new HostedContentController(TestResources.QvConfig,
                                                                      TestResources.UserManagerObject,
                                                                      TestResources.LoggerFactory,
                                                                      TestResources.DbContextObject,
                                                                      TestResources.QueriesObj,
                                                                      TestResources.AuthorizationService);

            // For illustration only, the same result comes from any of the following 3 techniques:
            // This one should never throw even if the user name is not in the context data
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: "test1");
            // Following 2 throw if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.DbContextObject.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.UserManagerObject.FindByNameAsync("test1").Result.UserName);
            #endregion

            #region Act
            // Attempt to load the content view for authorized content
            var view = sut.WebHostedContent(1); // User "test1" is authorized to RootContentItem w/ ID 1
            #endregion

            #region Assert
            // Test that a content view was returned
            Assert.IsType<ViewResult>(view);

            // Test that the expected content item was returned
            ViewResult viewResult = view as ViewResult;
            HostedContentViewModel ModelReturned = (HostedContentViewModel)viewResult.Model;
            Assert.Equal("RootContent 1", ModelReturned.ContentName);
            Assert.Equal(1, ModelReturned.UserGroupId);
            #endregion
        }

    }
}

