/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Unit tests for the content publishing controller
 * DEVELOPER NOTES: 
 */

using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using System;
using System.Threading.Tasks;
using Xunit;
using TestResourcesLib;
using MapDbContextLib.Models;
using System.Linq;
using MapDbContextLib.Context;

namespace MapTests
{
    public class ContentPublishingControllerTests
    {
        internal TestInitialization TestResources { get; set; }

        /// <summary>Initializes test resources.</summary>
        /// <remarks>This constructor is called before each test.</remarks>
        public ContentPublishingControllerTests()
        {
            TestResources = new TestInitialization();
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Reduction });
        }

        /// <summary>Constructs a controller with the specified active user.</summary>
        /// <param name="Username"></param>
        /// <returns>ContentPublishingController</returns>
        public async Task<ContentPublishingController> GetControllerForUser(string Username)
        {
            var testController = new ContentPublishingController(
                TestResources.AuditLoggerObject,
                TestResources.AuthorizationService,
                TestResources.DbContextObject,
                TestResources.LoggerFactory,
                TestResources.QueriesObj,
                TestResources.UserManagerObject,
                TestResources.ConfigurationObject
                );

            try
            {
                Username = (await TestResources.UserManagerObject.FindByNameAsync(Username)).UserName;
            }
            catch (NullReferenceException)
            {
                throw new ArgumentException($"Username '{Username}' is not present in the test database.");
            }
            testController.ControllerContext = TestInitialization.GenerateControllerContext(Username);
            testController.HttpContext.Session = new MockSession();

            return testController;
        }

        [Fact]
        public async Task Index_ReturnsView()
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            var view = controller.Index();
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            #endregion
        }

        [Theory]
        [InlineData(999, 1, true)]
        [InlineData(1, 999, true)]
        [InlineData(1, 1, false)]
        public async Task CreateRootContentItem_ErrorInvalid(long clientId, long contentTypeId, bool useContentName)
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user1");
            var validRootContentItem = new RootContentItem
            {
                ClientId = clientId,
                ContentTypeId = contentTypeId,
                DoesReduce = false,
            };
            if (useContentName)
            {
                validRootContentItem.ContentName = "";
            }
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.RootContentItem.Count();
            var view = await controller.CreateRootContentItem(validRootContentItem);
            int postCount = TestResources.DbContextObject.RootContentItem.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            StatusCodeResult viewResult = (StatusCodeResult) view;
            Assert.Equal("422", viewResult.StatusCode.ToString());
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content publisher
        [InlineData("user1", 2)]  // User has no role in the client
        public async Task CreateRootContentItem_ErrorUnauthorized(String userName, long clientId)
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser(userName);
            var validRootContentItem = new RootContentItem
            {
                ClientId = clientId,
                ContentTypeId = 1,
                ContentName = "",
                DoesReduce = false,
            };
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.RootContentItem.Count();
            var view = await controller.CreateRootContentItem(validRootContentItem);
            int postCount = TestResources.DbContextObject.RootContentItem.Count();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Fact]
        public async Task CreateRootContentItem_ReturnsJson()
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user1");
            var validRootContentItem = new RootContentItem
            {
                ClientId = 1,
                ContentTypeId = 1,
                ContentName = "",
                DoesReduce = false,
            };
            #endregion

            #region Act
            var view = await controller.CreateRootContentItem(validRootContentItem);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Fact]
        public async Task CreateRootContentItem_Success()
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user1");
            var validRootContentItem = new RootContentItem
            {
                ClientId = 1,
                ContentTypeId = 1,
                ContentName = "",
                DoesReduce = false,
            };
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.RootContentItem.Count();
            var view = await controller.CreateRootContentItem(validRootContentItem);
            int postCount = TestResources.DbContextObject.RootContentItem.Count();
            #endregion

            #region Assert
            Assert.Equal(preCount + 1, postCount);
            #endregion
        }

        [Theory]
        [InlineData(999)]
        public async Task DeleteRootContentItem_ErrorInvalid(long rootContentItemId)
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.RootContentItem.Count();
            var view = await controller.DeleteRootContentItem(rootContentItemId);
            int postCount = TestResources.DbContextObject.RootContentItem.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            StatusCodeResult viewResult = (StatusCodeResult)view;
            Assert.Equal("422", viewResult.StatusCode.ToString());
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content publisher
        [InlineData("user1", 2)]  // User has no role in the client
        public async Task DeleteRootContentItem_ErrorUnauthorized(String userName, long rootContentItemId)
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser(userName);
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.RootContentItem.Count();
            var view = await controller.DeleteRootContentItem(rootContentItemId);
            int postCount = TestResources.DbContextObject.RootContentItem.Count();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Fact]
        public async Task DeleteRootContentItem_ReturnsJson()
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            var view = await controller.DeleteRootContentItem(3);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Fact]
        public async Task DeleteRootContentItem_Success()
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.RootContentItem.Count();
            var view = await controller.DeleteRootContentItem(3);
            int postCount = TestResources.DbContextObject.RootContentItem.Count();
            #endregion

            #region Assert
            Assert.Equal(preCount - 1, postCount);
            #endregion
        }

        /*
        [Fact]
        public async Task RequestContentPublication_Ok()
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("test1");
            #endregion

            #region Act
            // TODO Make this test work
            var view = await controller.Publish(new PublishRequest { RootContentItemId = 1, RelatedFiles= new ContentRelatedFile[] { new ContentRelatedFile{ FilePurpose = "MasterContent", FileUploadId = Guid.Empty } } });
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }
        */
    }
}
