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
using Microsoft.Extensions.Primitives;
using Xunit;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.AuthorizedContentViewModels;
using TestResourcesLib;
using MapDbContextLib.Context;
using MapCommonLib;

namespace MapTests
{
    [Collection("DatabaseLifetime collection")]
    public class AuthorizedContentControllerTests
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;

        public AuthorizedContentControllerTests(DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _dbLifeTimeFixture = dbLifeTimeFixture;
        }

        /// <summary>
        /// Test that the Index returns a view containing a list of content items the test user can access.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Index_ReturnsAViewResult()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                // initialize dependencies
                // Create the system under test (sut)
                AuthorizedContentController sut = new AuthorizedContentController(
                TestResources.AuditLogger,
                TestResources.AuthorizationService,
                TestResources.DbContext,
                TestResources.MessageQueueServicesObject,
                TestResources.QvConfig,
                TestResources.UserManager,
                TestResources.Configuration,
                TestResources.PowerBiConfig);

                // For illustration only, the same result comes from either of the following techniques:
                // This one should never throw even if the user name is not in the context data
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: "test1");
                // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);
                #endregion

                #region Act
                // invoke the controller action to be tested
                var view = sut.Index();
                #endregion

                #region Assert
                Assert.IsType<ViewResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Test that the Index returns a view containing a list of content items the test user can access.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Content_DeduplicatesAssignedSelectionGroups()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                AuthorizedContentController sut = new AuthorizedContentController(
                    TestResources.AuditLogger,
                    TestResources.AuthorizationService,
                    TestResources.DbContext,
                    TestResources.MessageQueueServicesObject,
                    TestResources.QvConfig,
                    TestResources.UserManager,
                    TestResources.Configuration,
                    TestResources.PowerBiConfig);

                // For illustration only, the same result comes from either of the following techniques:
                // This one should never throw even if the user name is not in the context data
                sut.ControllerContext = TestResources.GenerateControllerContext("test1", new UriBuilder { Scheme = "https", Host = "www.test.com", Path = "/", Query = "p1=abc&p2=def&p3=ghi" });
                // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
                //sut.ControllerContext = TestInitialization2.GenerateControllerContext(userName: TestResources.DbContextObject.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);
                #endregion

                #region Act
                // invoke the controller action to be tested
                var view = await sut.Content();
                #endregion

                #region Assert
                Assert.Equal(2, TestResources.DbContext.UserInSelectionGroup.Where(usg => usg.UserId == TestUtil.MakeTestGuid(1) && usg.SelectionGroupId == TestUtil.MakeTestGuid(1)).Count());
                JsonResult returnModel = Assert.IsType<JsonResult>(view);
                AuthorizedContentViewModel typedModel = Assert.IsType<AuthorizedContentViewModel>(returnModel.Value);
                Assert.Single(typedModel.ItemGroups);
                Assert.Single(typedModel.ItemGroups[0].Items);
                #endregion
            }
        }

        /// <summary>
        /// Test that WebHostedContent(id) results in an error page when the user is not authorized to the content
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task WebHostedContent_ErrorWhenNotAuthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                // Attempt to load the content view for unauthorized content
                #region Arrange
                // Create the system under test (sut)
                AuthorizedContentController sut = new AuthorizedContentController(
                    TestResources.AuditLogger,
                    TestResources.AuthorizationService,
                    TestResources.DbContext,
                    TestResources.MessageQueueServicesObject,
                    TestResources.QvConfig,
                    TestResources.UserManager,
                    TestResources.Configuration,
                    TestResources.PowerBiConfig);

                // For illustration only, the same result comes from either of the following techniques:
                // This one should never throw even if the user name is not in the context data
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: "test1");
                // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);

                sut.ControllerContext.ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor { ActionName = "WebHostedContent" };
                sut.HttpContext.Session = new MockSession();
                #endregion

                #region Act
                var view = await sut.WebHostedContent(TestUtil.MakeTestGuid(3)); // User "test1" is not authorized to RootContentItem for SelectionGroup w/ ID 3
                #endregion

                #region Assert
                ViewResult typedResult = Assert.IsType<ViewResult>(view);
                Assert.Equal("UserMessage", typedResult.ViewName);
                #endregion
            }
        }

        /// <summary>
        /// Test that WebHostedContent(id) displays the content view when the user is authorized to the content
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task WebHostedContent_DisplaysWhenAuthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                // Create the system under test (sut)
                AuthorizedContentController sut = new AuthorizedContentController(
                    TestResources.AuditLogger,
                    TestResources.AuthorizationService,
                    TestResources.DbContext,
                    TestResources.MessageQueueServicesObject,
                    TestResources.QvConfig,
                    TestResources.UserManager,
                    TestResources.Configuration,
                    TestResources.PowerBiConfig);

                // For illustration only, the same result comes from either of the following techniques:
                // This one should never throw even if the user name is not in the context data
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: "test1");
                // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);
                sut.ControllerContext = TestResources.GenerateControllerContext(TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName,
                                                                                     new UriBuilder { Scheme = "https", Host = "www.test.com", Path = "/", Query = "p1=abc&p2=def&p3=ghi" },
                                                                                     new Dictionary<string, StringValues> { { "Referer", "https://www.impossible.wut/AuthorizedContent/ContentWrapper" } });

                // Add a file to the root content item and a content url to the selection group
                string FileName = "CCR_0273ZDM_New_Reduction_Script.qvw";
                string TestFileSourcePath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\Sample Data", FileName);
                string TestFileTargetPath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\ContentRoot", TestUtil.MakeTestGuid(1).ToString(), FileName);
                File.Copy(TestFileSourcePath, TestFileTargetPath, true);
                SelectionGroup ThisGroup = TestResources.DbContext.SelectionGroup.Single(sg => sg.Id == TestUtil.MakeTestGuid(1));
                RootContentItem ThisItem = TestResources.DbContext.RootContentItem.FirstOrDefault(rci => rci.Id == TestUtil.MakeTestGuid(1));
                ThisGroup.ReducedContentChecksum = GlobalFunctions.GetFileChecksum(TestFileTargetPath);
                ThisGroup.ContentInstanceUrl = $@"{ThisItem.Id}\{FileName}";

                #endregion

                #region Act
                // Attempt to load the content view for authorized content
                var actionResult = await sut.WebHostedContent(TestUtil.MakeTestGuid(1)); // User "test1" is authorized to RootContentItem w/ ID 1
                #endregion

                #region Assert
                // Test that a content view was returned
                RedirectResult viewResult = Assert.IsType<RedirectResult>(actionResult);

                // Test that the expected URI was returned
                UriBuilder Uri = new UriBuilder(viewResult.Url);
                Assert.Equal("https", Uri.Scheme);
                Assert.Equal(@"/qvajaxzfc/Authenticate.aspx", Uri.Path);
                Assert.Contains("type=html", Uri.Query);
                Assert.Contains(@"try=/qvajaxzfc/opendoc.htm", Uri.Query);
                Assert.Contains("document=", Uri.Query);
                Assert.Contains("webticket=", Uri.Query);
                #endregion
            }
        }

        /// <summary>
        /// Test that WebHostedContent(id) returns an error message when the checksum does not validate
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task WebHostedContent_DisplaysMessageWhenChecksumIsInvalid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                // Create the system under test (sut)
                AuthorizedContentController sut = new AuthorizedContentController(
                    TestResources.AuditLogger,
                    TestResources.AuthorizationService,
                    TestResources.DbContext,
                    TestResources.MessageQueueServicesObject,
                    TestResources.QvConfig,
                    TestResources.UserManager,
                    TestResources.Configuration,
                    TestResources.PowerBiConfig);

                // For illustration only, the same result comes from either of the following techniques:
                // This one should never throw even if the user name is not in the context data
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: "test1");
                // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
                sut.ControllerContext = TestResources.GenerateControllerContext(TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);

                sut.ControllerContext = TestResources.GenerateControllerContext(TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName,
                                                                                     new UriBuilder { Scheme = "https", Host = "www.test.com", Path = "/", Query = "p1=abc&p2=def&p3=ghi" },
                                                                                     new Dictionary<string, StringValues> { { "Referer", "https://www.impossible.wut/AuthorizedContent/ContentWrapper" } });

                // Add a file to the root content item and a content url to the selection group
                string FileName = "CCR_0273ZDM_New_Reduction_Script.qvw";
                string TestFileSourcePath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\Sample Data", FileName);
                string TestFileTargetPath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\ContentRoot", TestUtil.MakeTestGuid(1).ToString(), FileName);
                File.Copy(TestFileSourcePath, TestFileTargetPath, true);
                SelectionGroup ThisGroup = TestResources.DbContext.SelectionGroup.Single(sg => sg.Id == TestUtil.MakeTestGuid(1));
                RootContentItem ThisItem = TestResources.DbContext.RootContentItem.FirstOrDefault(rci => rci.Id == TestUtil.MakeTestGuid(1));
                ThisGroup.ReducedContentChecksum = "Bad Checksum Will Not Validate";
                ThisGroup.ContentInstanceUrl = $@"{ThisItem.Id}\{FileName}";

                #endregion

                #region Act
                // Attempt to load the content view for authorized content
                var result = await sut.WebHostedContent(TestUtil.MakeTestGuid(1)); // User "test1" is authorized to RootContentItem w/ ID 1
                #endregion

                #region Assert
                // Test that a ViewResult was returned instead of a RedirectResult
                ViewResult viewResult = Assert.IsType<ViewResult>(result);

                // Test that the Message view was returned
                Assert.Equal("UserMessage", viewResult.ViewName);
                #endregion
            }
        }

        [Fact]
        public async Task WebHostedContent_RedirectToContentWrapperWhenNotReferredTherefrom()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                // Create the system under test (sut)
                AuthorizedContentController sut = new AuthorizedContentController(
                    TestResources.AuditLogger,
                    TestResources.AuthorizationService,
                    TestResources.DbContext,
                    TestResources.MessageQueueServicesObject,
                    TestResources.QvConfig,
                    TestResources.UserManager,
                    TestResources.Configuration,
                    TestResources.PowerBiConfig);

                // For illustration only, the same result comes from either of the following techniques:
                // This one should never throw even if the user name is not in the context data
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: "test1");
                // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
                sut.ControllerContext = TestResources.GenerateControllerContext(TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);

                sut.ControllerContext = TestResources.GenerateControllerContext(TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName,
                                                                                     new UriBuilder { Scheme = "https", Host = "www.test.com", Path = "/", Query = "p1=abc&p2=def&p3=ghi" },
                                                                                     new Dictionary<string, StringValues> { { "Referer", "https://www.impossible.wut/AuthorizedContent/Index" } });

                // Add a file to the root content item and a content url to the selection group
                string FileName = "CCR_0273ZDM_New_Reduction_Script.qvw";
                string TestFileSourcePath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\Sample Data", FileName);
                string TestFileTargetPath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\ContentRoot", TestUtil.MakeTestGuid(1).ToString(), FileName);
                File.Copy(TestFileSourcePath, TestFileTargetPath, true);
                SelectionGroup ThisGroup = TestResources.DbContext.SelectionGroup.Single(sg => sg.Id == TestUtil.MakeTestGuid(1));
                RootContentItem ThisItem = TestResources.DbContext.RootContentItem.FirstOrDefault(rci => rci.Id == TestUtil.MakeTestGuid(1));
                ThisGroup.ReducedContentChecksum = "Bad Checksum Will Not Validate";
                ThisGroup.ContentInstanceUrl = $@"{ThisItem.Id}\{FileName}";

                #endregion

                #region Act
                // Attempt to load the content view for authorized content
                var result = await sut.WebHostedContent(TestUtil.MakeTestGuid(1)); // User "test1" is authorized to RootContentItem w/ ID 1
                #endregion

                #region Assert
                // Test that a ViewResult was returned instead of a RedirectResult
                RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);

                // Test that the Message view was returned
                Assert.Contains("/ContentWrapper", redirectResult.Url);
                #endregion
            }
        }

        /// <summary>
        /// Thumbnail error for invalid SelectionGroup
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ThumbnailErrorForInvalidSelectionGroup()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                // Create the system under test (sut)
                AuthorizedContentController sut = new AuthorizedContentController(
                    TestResources.AuditLogger,
                    TestResources.AuthorizationService,
                    TestResources.DbContext,
                    TestResources.MessageQueueServicesObject,
                    TestResources.QvConfig,
                    TestResources.UserManager,
                    TestResources.Configuration,
                    TestResources.PowerBiConfig);

                // For illustration only, the same result comes from either of the following techniques:
                // This one should never throw even if the user name is not in the context data
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: "test1");
                // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);
                #endregion

                #region Act
                var actionResult = await sut.WebHostedContent(TestUtil.MakeTestGuid(999));
                #endregion

                #region Assert
                ViewResult typedResult = Assert.IsType<ViewResult>(actionResult);
                Assert.Equal("UserMessage", typedResult.ViewName);
                #endregion
            }
        }

        /// <summary>
        /// Thumbnail error for invalid SelectionGroup
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task WebHostedContentUnauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                // Create the system under test (sut)
                AuthorizedContentController sut = new AuthorizedContentController(
                    TestResources.AuditLogger,
                    TestResources.AuthorizationService,
                    TestResources.DbContext,
                    TestResources.MessageQueueServicesObject,
                    TestResources.QvConfig,
                    TestResources.UserManager,
                    TestResources.Configuration,
                    TestResources.PowerBiConfig);

                // For illustration only, the same result comes from either of the following techniques:
                // This one should never throw even if the user name is not in the context data
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: "test1");
                // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);
                #endregion

                #region Act
                // Attempt to load the content view for authorized content
                var actionResult = await sut.WebHostedContent(TestUtil.MakeTestGuid(4)); // user1 is NOT assigned to SelectionGroup 4
                #endregion

                #region Assert
                ViewResult typedResult = Assert.IsType<ViewResult>(actionResult);
                Assert.Equal("UserMessage", typedResult.ViewName);
                #endregion
            }
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
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                // Create the system under test (sut)
                AuthorizedContentController sut = new AuthorizedContentController(
                    TestResources.AuditLogger,
                    TestResources.AuthorizationService,
                    TestResources.DbContext,
                    TestResources.MessageQueueServicesObject,
                    TestResources.QvConfig,
                    TestResources.UserManager,
                    TestResources.Configuration,
                    TestResources.PowerBiConfig);

                // For illustration only, the same result comes from either of the following techniques:
                // This one should never throw even if the user name is not in the context data
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: "test1");
                // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);

                string UserGuideSourcePath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\Sample Data", "IHopeSo.pdf");
                string UserGuideTestPath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\ContentRoot", purpose + ".pdf");
                File.Copy(UserGuideSourcePath, UserGuideTestPath, true);
                RootContentItem ThisItem = TestResources.DbContext.RootContentItem.Single(rci => rci.Id == TestUtil.MakeTestGuid(1));
                ThisItem.ContentFilesList = new List<MapDbContextLib.Models.ContentRelatedFile>
            {
                new MapDbContextLib.Models.ContentRelatedFile { Checksum = GlobalFunctions.GetFileChecksum(UserGuideTestPath), FileOriginalName = "", FilePurpose = purpose, FullPath = UserGuideTestPath, }
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
                    PhysicalFileResult fileResult = Assert.IsType<PhysicalFileResult>(result);
                    Assert.Equal(UserGuideTestPath, fileResult.FileName);
                }
                finally
                {
                    File.Delete(UserGuideTestPath);
                }
                #endregion
            }
        }

        /// <summary>
        /// Related PDF load (e.g. release notes or user guide
        /// 
        /// Validate that an incorrect checksum will not return a file result
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RelatedPdfInvalidChecksum()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                // Create the system under test (sut)
                AuthorizedContentController sut = new AuthorizedContentController(
                    TestResources.AuditLogger,
                    TestResources.AuthorizationService,
                    TestResources.DbContext,
                    TestResources.MessageQueueServicesObject,
                    TestResources.QvConfig,
                    TestResources.UserManager,
                    TestResources.Configuration,
                    TestResources.PowerBiConfig);

                // For illustration only, the same result comes from either of the following techniques:
                // This one should never throw even if the user name is not in the context data
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: "test1");
                // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);
                string purpose = "UserGuide";
                string UserGuideSourcePath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\Sample Data", "IHopeSo.pdf");
                string UserGuideTestPath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\ContentRoot", purpose + ".pdf");
                File.Copy(UserGuideSourcePath, UserGuideTestPath, true);
                RootContentItem ThisItem = TestResources.DbContext.RootContentItem.Single(rci => rci.Id == TestUtil.MakeTestGuid(1));
                ThisItem.ContentFilesList = new List<MapDbContextLib.Models.ContentRelatedFile>
            {
                new MapDbContextLib.Models.ContentRelatedFile { Checksum = "Bad Checksum Will Not Validate", FileOriginalName = "", FilePurpose = purpose, FullPath = UserGuideTestPath, }
            };
                #endregion

                #region Act
                // Attempt to load the content view for authorized content
                var result = await sut.RelatedPdf(purpose, TestUtil.MakeTestGuid(1)); // user1 is assigned to SelectionGroup 1
                #endregion

                #region Assert
                try
                {
                    // Test that a content view was not returned
                    ViewResult viewResult = Assert.IsType<ViewResult>(result);
                    Assert.Equal("UserMessage", viewResult.ViewName);
                }
                finally
                {
                    File.Delete(UserGuideTestPath);
                }
                #endregion
            }
        }

        /// <summary>
        /// Related PDF load invalid SelectionGroup
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RelatedPdfInvalidSelectionGroup()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                // Create the system under test (sut)
                AuthorizedContentController sut = new AuthorizedContentController(
                    TestResources.AuditLogger,
                    TestResources.AuthorizationService,
                    TestResources.DbContext,
                    TestResources.MessageQueueServicesObject,
                    TestResources.QvConfig,
                    TestResources.UserManager,
                    TestResources.Configuration,
                    TestResources.PowerBiConfig);

                // For illustration only, the same result comes from either of the following techniques:
                // This one should never throw even if the user name is not in the context data
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: "test1");
                // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);

                string purpose = "UserGuide";
                string UserGuideSourcePath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\Sample Data", "IHopeSo.pdf");
                string UserGuideTestPath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\ContentRoot", purpose + ".pdf");
                File.Copy(UserGuideSourcePath, UserGuideTestPath, true);
                RootContentItem ThisItem = TestResources.DbContext.RootContentItem.Single(rci => rci.Id == TestUtil.MakeTestGuid(1));
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
                    ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
                    Assert.Equal(500, objectResult.StatusCode);
                }
                finally
                {
                    File.Delete(UserGuideTestPath);
                }
                #endregion
            }
        }

        /// <summary>
        /// Related PDF load invalid SelectionGroup
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RelatedPdfUnauthorizedSelectionGroup()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                // Create the system under test (sut)
                AuthorizedContentController sut = new AuthorizedContentController(
                    TestResources.AuditLogger,
                    TestResources.AuthorizationService,
                    TestResources.DbContext,
                    TestResources.MessageQueueServicesObject,
                    TestResources.QvConfig,
                    TestResources.UserManager,
                    TestResources.Configuration,
                    TestResources.PowerBiConfig);

                // For illustration only, the same result comes from either of the following techniques:
                // This one should never throw even if the user name is not in the context data
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: "test1");
                // Following throws if dependency failed to create or specified user is not in the data. Use try/catch to prevent failure for this cause
                sut.ControllerContext = TestResources.GenerateControllerContext(userName: TestResources.DbContext.ApplicationUser.Where(u => u.UserName == "test1").First().UserName);

                string purpose = "UserGuide";
                string UserGuideSourcePath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\Sample Data", "IHopeSo.pdf");
                string UserGuideTestPath = Path.Combine(@"\\indy-syn01.milliman.com\prm_test\ContentRoot", purpose + ".pdf");
                File.Copy(UserGuideSourcePath, UserGuideTestPath, true);
                RootContentItem ThisItem = TestResources.DbContext.RootContentItem.Single(rci => rci.Id == TestUtil.MakeTestGuid(1));
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
}

