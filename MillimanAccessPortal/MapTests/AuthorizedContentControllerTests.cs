/*
 * CODE OWNERS: Ben Wyatt, Tom Puckett
 * OBJECTIVE: Perform unit tests against AuthorizedContentController
 * DEVELOPER NOTES: <What future developers need to know.>
 */
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.AuthorizedContentViewModels;
using TestResourcesLib;
using MapDbContextLib.Context;

namespace MapTests
{
    public class AuthorizedControllerTests
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
            AuthorizedContentController sut = new AuthorizedContentController(
                TestResources.AuditLoggerObject,
                TestResources.AuthorizationService,
                TestResources.DbContextObject,
                TestResources.MessageQueueServicesObject,
                TestResources.LoggerFactory,
                TestResources.QvConfig,
                TestResources.QueriesObj,
                TestResources.UserManagerObject,
                TestResources.ConfigurationObject);

            // For illustration only, the same result comes from either of the following techniques:
            // This one should never throw even if the user name is not in the context data
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: "test1");
            // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.DbContextObject.ApplicationUser.Where(u => u.UserName=="test1").First().UserName);
            #endregion

            #region Act
            // invoke the controller action to be tested
            var view = sut.Index();
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            #endregion
        }

        /// <summary>
        /// Test that WebHostedContent(id) results in an error page when the user is not authorized to the content
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task WebHostedContent_ErrorWhenNotAuthorized()
        {
            // Attempt to load the content view for unauthorized content
            #region Arrange
            // initialize dependencies
            TestInitialization TestResources = new TestInitialization();

            // initialize data
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });

            // Create the system under test (sut)
            AuthorizedContentController sut = new AuthorizedContentController(
                TestResources.AuditLoggerObject,
                TestResources.AuthorizationService,
                TestResources.DbContextObject,
                TestResources.MessageQueueServicesObject,
                TestResources.LoggerFactory,
                TestResources.QvConfig,
                TestResources.QueriesObj,
                TestResources.UserManagerObject,
                TestResources.ConfigurationObject);

            // For illustration only, the same result comes from either of the following techniques:
            // This one should never throw even if the user name is not in the context data
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: "test1");
            // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.DbContextObject.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);

            sut.ControllerContext.ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor { ActionName = "WebHostedContent" };
            sut.HttpContext.Session = new MockSession();
            #endregion


            #region Act
            var view = await sut.WebHostedContent(TestUtil.MakeTestGuid(3)); // User "test1" is not authorized to RootContentItem for SelectionGroup w/ ID 3
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
        public async Task WebHostedContent_DisplaysWhenAuthorized()
        {
            #region Arrange
            // initialize dependencies
            TestInitialization TestResources = new TestInitialization();

            // initialize data
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });

            // Create the system under test (sut)
            AuthorizedContentController sut = new AuthorizedContentController(
                TestResources.AuditLoggerObject,
                TestResources.AuthorizationService,
                TestResources.DbContextObject,
                TestResources.MessageQueueServicesObject,
                TestResources.LoggerFactory,
                TestResources.QvConfig,
                TestResources.QueriesObj,
                TestResources.UserManagerObject,
                TestResources.ConfigurationObject);

            // For illustration only, the same result comes from either of the following techniques:
            // This one should never throw even if the user name is not in the context data
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: "test1");
            // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.DbContextObject.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);
            #endregion

            #region Act
            // Attempt to load the content view for authorized content
            var actionResult = await sut.WebHostedContent(TestUtil.MakeTestGuid(1)); // User "test1" is authorized to RootContentItem w/ ID 1
            #endregion

            #region Assert
            // Test that a content view was returned
            Assert.IsType<RedirectResult>(actionResult);

            // Test that the expected URI was returned
            RedirectResult viewResult = actionResult as RedirectResult;
            UriBuilder Uri = new UriBuilder(viewResult.Url);
            Assert.Equal("https", Uri.Scheme);
            Assert.Equal(@"/qvajaxzfc/Authenticate.aspx", Uri.Path);
            Assert.Contains("type=html", Uri.Query);
            Assert.Contains(@"try=/qvajaxzfc/opendoc.htm", Uri.Query);
            Assert.Contains("document=", Uri.Query);
            Assert.Contains("back=", Uri.Query);
            Assert.Contains("webticket=", Uri.Query);
            #endregion
        }

        /// <summary>
        /// Thumbnail error for invalid SelectionGroup
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ThumbnailErrorForInvalidSelectionGroup()
        {
            #region Arrange
            // initialize dependencies
            TestInitialization TestResources = new TestInitialization();

            // initialize data
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });

            // Create the system under test (sut)
            AuthorizedContentController sut = new AuthorizedContentController(
                TestResources.AuditLoggerObject,
                TestResources.AuthorizationService,
                TestResources.DbContextObject,
                TestResources.MessageQueueServicesObject,
                TestResources.LoggerFactory,
                TestResources.QvConfig,
                TestResources.QueriesObj,
                TestResources.UserManagerObject,
                TestResources.ConfigurationObject);

            // For illustration only, the same result comes from either of the following techniques:
            // This one should never throw even if the user name is not in the context data
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: "test1");
            // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.DbContextObject.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);
            #endregion

            #region Act
            // Attempt to load the content view for authorized content
            var actionResult = await sut.WebHostedContent(TestUtil.MakeTestGuid(999));
            #endregion

            #region Assert
            // Test that a content view was returned
            Assert.IsType<ObjectResult>(actionResult);

            // Test that the expected error was returned
            ObjectResult objectResult = actionResult as ObjectResult;
            Assert.Equal(500, objectResult.StatusCode);
            #endregion
        }

        /// <summary>
        /// Thumbnail error for invalid SelectionGroup
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task WebHostedContentUnauthorized()
        {
            #region Arrange
            // initialize dependencies
            TestInitialization TestResources = new TestInitialization();

            // initialize data
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });

            // Create the system under test (sut)
            AuthorizedContentController sut = new AuthorizedContentController(
                TestResources.AuditLoggerObject,
                TestResources.AuthorizationService,
                TestResources.DbContextObject,
                TestResources.MessageQueueServicesObject,
                TestResources.LoggerFactory,
                TestResources.QvConfig,
                TestResources.QueriesObj,
                TestResources.UserManagerObject,
                TestResources.ConfigurationObject);

            // For illustration only, the same result comes from either of the following techniques:
            // This one should never throw even if the user name is not in the context data
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: "test1");
            // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.DbContextObject.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);
            #endregion

            #region Act
            // Attempt to load the content view for authorized content
            var actionResult = await sut.WebHostedContent(TestUtil.MakeTestGuid(4)); // user1 is NOT assigned to SelectionGroup 4
            #endregion

            #region Assert
            // Test that a content view was returned
            Assert.IsType<UnauthorizedResult>(actionResult);
            Assert.Contains("You are not authorized to access the requested content", sut.Response.Headers.Select(h => h.Value));
            #endregion
        }

        /// <summary>
        /// Related PDF load (e.g. release notes or user guide
        /// </summary>
        /// <returns></returns>
        [Theory]
        [InlineData("UserGuide")]
        [InlineData("ReleaseNotes")]
        public async Task RelatedPdfSuccess(string purpose)
        {
            #region Arrange
            // initialize dependencies
            TestInitialization TestResources = new TestInitialization();

            // initialize data
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });

            // Create the system under test (sut)
            AuthorizedContentController sut = new AuthorizedContentController(
                TestResources.AuditLoggerObject,
                TestResources.AuthorizationService,
                TestResources.DbContextObject,
                TestResources.MessageQueueServicesObject,
                TestResources.LoggerFactory,
                TestResources.QvConfig,
                TestResources.QueriesObj,
                TestResources.UserManagerObject,
                TestResources.ConfigurationObject);

            // For illustration only, the same result comes from either of the following techniques:
            // This one should never throw even if the user name is not in the context data
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: "test1");
            // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.DbContextObject.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);

            string UserGuideSourcePath = Path.Combine(@"\\indy-syn01\prm_test\Sample Data", "IHopeSo.pdf");
            string UserGuideTestPath = Path.Combine(@"\\indy-syn01\prm_test\ContentRoot", purpose + ".pdf");
            File.Copy(UserGuideSourcePath, UserGuideTestPath, true);
            RootContentItem ThisItem = TestResources.DbContextObject.RootContentItem.Single(rci => rci.Id == TestUtil.MakeTestGuid(1));
            ThisItem.ContentFilesList = new List<MapDbContextLib.Models.ContentRelatedFile>
            {
                new MapDbContextLib.Models.ContentRelatedFile { Checksum = "", FileOriginalName = "", FilePurpose = purpose, FullPath = UserGuideTestPath, }
            };
            #endregion

            #region Act
            // Attempt to load the content view for authorized content
            var result = await sut.RelatedPdf(purpose, TestUtil.MakeTestGuid(1)); // user1 is assigned to SelectionGroup 1
            #endregion

            #region Assert
            try
            {
                // Test that a content view was returned
                Assert.IsType<FileStreamResult>(result);
                FileStreamResult fileResult = result as FileStreamResult;
                Assert.Equal(UserGuideTestPath, ((System.IO.FileStream)fileResult.FileStream).Name);
                fileResult.FileStream.Close();
            }
            finally
            {
                File.Delete(UserGuideTestPath);
            }
            #endregion
        }

        /// <summary>
        /// Related PDF load invalid SelectionGroup
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RelatedPdfInvalidSelectionGroup()
        {
            #region Arrange
            // initialize dependencies
            TestInitialization TestResources = new TestInitialization();

            // initialize data
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });

            // Create the system under test (sut)
            AuthorizedContentController sut = new AuthorizedContentController(
                TestResources.AuditLoggerObject,
                TestResources.AuthorizationService,
                TestResources.DbContextObject,
                TestResources.MessageQueueServicesObject,
                TestResources.LoggerFactory,
                TestResources.QvConfig,
                TestResources.QueriesObj,
                TestResources.UserManagerObject,
                TestResources.ConfigurationObject);

            // For illustration only, the same result comes from either of the following techniques:
            // This one should never throw even if the user name is not in the context data
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: "test1");
            // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.DbContextObject.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);

            string purpose = "UserGuide";
            string UserGuideSourcePath = Path.Combine(@"\\indy-syn01\prm_test\Sample Data", "IHopeSo.pdf");
            string UserGuideTestPath = Path.Combine(@"\\indy-syn01\prm_test\ContentRoot", purpose + ".pdf");
            File.Copy(UserGuideSourcePath, UserGuideTestPath, true);
            RootContentItem ThisItem = TestResources.DbContextObject.RootContentItem.Single(rci => rci.Id == TestUtil.MakeTestGuid(1));
            ThisItem.ContentFilesList = new List<MapDbContextLib.Models.ContentRelatedFile>
            {
                new MapDbContextLib.Models.ContentRelatedFile { Checksum = "", FileOriginalName = "", FilePurpose = purpose, FullPath = UserGuideTestPath, }
            };
            #endregion

            #region Act
            // Attempt to load the content view for authorized content
            var result = await sut.RelatedPdf(purpose, TestUtil.MakeTestGuid(999)); // SelectionGroup 999 does not exist
            #endregion

            #region Assert
            try
            {
                // Test that a content view was returned
                Assert.IsType<ObjectResult>(result);
                ObjectResult objectResult = result as ObjectResult;
                Assert.Equal(500, objectResult.StatusCode);
            }
            finally
            {
                File.Delete(UserGuideTestPath);
            }
            #endregion
        }

        /// <summary>
        /// Related PDF load invalid SelectionGroup
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RelatedPdfUnauthorizedSelectionGroup()
        {
            #region Arrange
            // initialize dependencies
            TestInitialization TestResources = new TestInitialization();

            // initialize data
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });

            // Create the system under test (sut)
            AuthorizedContentController sut = new AuthorizedContentController(
                TestResources.AuditLoggerObject,
                TestResources.AuthorizationService,
                TestResources.DbContextObject,
                TestResources.MessageQueueServicesObject,
                TestResources.LoggerFactory,
                TestResources.QvConfig,
                TestResources.QueriesObj,
                TestResources.UserManagerObject,
                TestResources.ConfigurationObject);

            // For illustration only, the same result comes from either of the following techniques:
            // This one should never throw even if the user name is not in the context data
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: "test1");
            // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
            sut.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.DbContextObject.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);

            string purpose = "UserGuide";
            string UserGuideSourcePath = Path.Combine(@"\\indy-syn01\prm_test\Sample Data", "IHopeSo.pdf");
            string UserGuideTestPath = Path.Combine(@"\\indy-syn01\prm_test\ContentRoot", purpose + ".pdf");
            File.Copy(UserGuideSourcePath, UserGuideTestPath, true);
            RootContentItem ThisItem = TestResources.DbContextObject.RootContentItem.Single(rci => rci.Id == TestUtil.MakeTestGuid(1));
            ThisItem.ContentFilesList = new List<MapDbContextLib.Models.ContentRelatedFile>
            {
                new MapDbContextLib.Models.ContentRelatedFile { Checksum = "", FileOriginalName = "", FilePurpose = purpose, FullPath = UserGuideTestPath, }
            };
            #endregion

            #region Act
            // Attempt to load the content view for authorized content
            var result = await sut.RelatedPdf(purpose, TestUtil.MakeTestGuid(2)); // user1 is NOT assigned to SelectionGroup 2
            #endregion

            #region Assert
            try
            {
                // Test that a content view was returned
                Assert.IsType<UnauthorizedResult>(result);
            }
            finally
            {
                File.Delete(UserGuideTestPath);
            }
            #endregion
        }

    }
}

