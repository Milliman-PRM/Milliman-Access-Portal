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

    }
}

