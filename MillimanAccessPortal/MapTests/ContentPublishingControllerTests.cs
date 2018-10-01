/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Unit tests for the content publishing controller
 * DEVELOPER NOTES: 
 */

using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.ContentPublishing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using TestResourcesLib;
using MapDbContextLib.Models;
using System.Linq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;

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
                TestResources.ConfigurationObject,
                TestResources.QvConfig
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
        public async Task CreateRootContentItem_ErrorInvalid(int clientIdArg, int contentTypeIdArg, bool useContentName)
        {
            #region Arrange
            Guid clientId = TestUtil.MakeTestGuid(clientIdArg);
            Guid contentTypeId = TestUtil.MakeTestGuid(contentTypeIdArg);
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
            Assert.Equal(422, (view as StatusCodeResult).StatusCode);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content publisher
        [InlineData("user1", 2)]  // User has no role in the client
        public async Task CreateRootContentItem_ErrorUnauthorized(String userName, int clientIdArg)
        {
            #region Arrange
            Guid clientId = TestUtil.MakeTestGuid(clientIdArg);
            ContentPublishingController controller = await GetControllerForUser(userName);
            var validRootContentItem = new RootContentItem
            {
                ClientId = clientId,
                ContentTypeId = TestUtil.MakeTestGuid(1),
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
                ClientId = TestUtil.MakeTestGuid(1),
                ContentTypeId = TestUtil.MakeTestGuid(1),
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
                ClientId = TestUtil.MakeTestGuid(1),
                ContentTypeId = TestUtil.MakeTestGuid(1),
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
        public async Task DeleteRootContentItem_ErrorInvalid(int rootContentItemIdArg)
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user1");
            ApplicationUser user = await TestResources.UserManagerObject.FindByNameAsync("user1");
            await TestResources.UserManagerObject.AddPasswordAsync(user, "password");
            #endregion

            #region Act
            Guid rootContentItemId = TestUtil.MakeTestGuid(rootContentItemIdArg);
            int preCount = TestResources.DbContextObject.RootContentItem.Count();
            var view = await controller.DeleteRootContentItem(rootContentItemId, "password");
            int postCount = TestResources.DbContextObject.RootContentItem.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            Assert.Equal(422, (view as StatusCodeResult).StatusCode);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content publisher
        [InlineData("user1", 2)]  // User has no role in the client
        public async Task DeleteRootContentItem_ErrorUnauthorized(String userName, int rootContentItemIdArg)
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser(userName);
            ApplicationUser user = await TestResources.UserManagerObject.FindByNameAsync("user1");
            await TestResources.UserManagerObject.AddPasswordAsync(user, "password");
            #endregion

            #region Act
            Guid rootContentItemId = TestUtil.MakeTestGuid(rootContentItemIdArg);
            int preCount = TestResources.DbContextObject.RootContentItem.Count();
            var view = await controller.DeleteRootContentItem(rootContentItemId, "password");
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
            ApplicationUser user = await TestResources.UserManagerObject.FindByNameAsync("user1");
            await TestResources.UserManagerObject.AddPasswordAsync(user, "password");
            #endregion

            #region Act
            var view = await controller.DeleteRootContentItem(TestUtil.MakeTestGuid(3), "password");
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
            ApplicationUser user = await TestResources.UserManagerObject.FindByNameAsync("user1");
            await TestResources.UserManagerObject.AddPasswordAsync(user, "password");
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.RootContentItem.Count();
            var view = await controller.DeleteRootContentItem(TestUtil.MakeTestGuid(3), "password");
            int postCount = TestResources.DbContextObject.RootContentItem.Count();
            #endregion

            #region Assert
            Assert.Equal(preCount - 1, postCount);
            #endregion
        }

        [Fact]
        public async Task Publish_UnauthorizedUser()
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user3");
            PublishRequest RequestArg = new PublishRequest();
            #endregion

            #region Act
            var view = await controller.Publish(RequestArg);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            Assert.Contains(controller.Response.Headers, h => h.Value == "You are not authorized to publish this content");
            #endregion
        }

        [Fact]
        public async Task Publish_MissingFileUploadRecord()
        {
            // user1 is authorized with role 4 (ContentPublisher) to RootContentItem 3
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user1");
            PublishRequest RequestArg = new PublishRequest
            {
                RootContentItemId = TestUtil.MakeTestGuid(3),
                RelatedFiles = new UploadedRelatedFile[]
                {
                    new UploadedRelatedFile
                    {  // does not exist in initialized FileUpload entity. 
                        FilePurpose = "MasterContent",
                        FileUploadId = TestUtil.MakeTestGuid(99),
                    }
                }
            };
            #endregion

            #region Act
            var view = await controller.Publish(RequestArg);
            #endregion

            #region Assert
            Assert.IsType<BadRequestResult>(view);
            Assert.Contains(controller.Response.Headers, h => h.Value == "A specified uploaded file was not found.");
            #endregion
        }

        [Theory]
        [InlineData(PublicationStatus.Queued)]
        [InlineData(PublicationStatus.Processing)]
        public async Task Publish_PreviousPublishingPending(PublicationStatus ExistingRequestStatus)
        {
            // user1 is authorized with role 4 (ContentPublisher) to RootContentItem 3
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user1");
            PublishRequest RequestArg = new PublishRequest
            {
                RootContentItemId = TestUtil.MakeTestGuid(3),
                RelatedFiles = new UploadedRelatedFile[0],
            };
            // Create a new publicationrequest record with blocking status
            TestResources.DbContextObject.ContentPublicationRequest.Add(new ContentPublicationRequest
            {
                Id = TestUtil.MakeTestGuid(999),
                ApplicationUserId = TestUtil.MakeTestGuid(1),
                RootContentItemId = TestUtil.MakeTestGuid(3),
                RequestStatus = ExistingRequestStatus,
                ReductionRelatedFilesObj = new List<ReductionRelatedFiles>{ },
                CreateDateTimeUtc = DateTime.UtcNow - new TimeSpan(0,1,0),
            });
            #endregion

            #region Act
            var view = await controller.Publish(RequestArg);
            #endregion

            #region Assert
            Assert.IsType<BadRequestResult>(view);
            Assert.Contains(controller.Response.Headers, h => h.Value == "A previous publication is pending for this content.");
            #endregion
        }

        [Theory]
        [InlineData(ReductionStatusEnum.Queued)]
        [InlineData(ReductionStatusEnum.Reducing)]
        public async Task Publish_PreviousReductionPending(ReductionStatusEnum ExistingTaskStatus)
        {
            // user1 is authorized with role 4 (ContentPublisher) to RootContentItem 3
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user1");
            PublishRequest RequestArg = new PublishRequest
            {
                RootContentItemId = TestUtil.MakeTestGuid(3),
                RelatedFiles = new UploadedRelatedFile[0],
            };
            // Create a new publicationrequest record with blocking status
            TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
            {
                Id = Guid.NewGuid(),
                SelectionGroup = TestResources.DbContextObject.SelectionGroup.Single(g => g.RootContentItemId == TestUtil.MakeTestGuid(3)),
                ApplicationUserId = TestUtil.MakeTestGuid(1),
                ReductionStatus = ExistingTaskStatus,
                CreateDateTimeUtc = DateTime.UtcNow,
            });
            #endregion

            #region Act
            var view = await controller.Publish(RequestArg);
            #endregion

            #region Assert
            Assert.IsType<BadRequestResult>(view);
            Assert.Contains(controller.Response.Headers, h => h.Value == "A previous reduction task is pending for this content.");
            #endregion
        }

        [Fact]
        public async Task GoLive_UnauthorizedUser()
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user3");
            #endregion

            #region Act
            var view = await controller.GoLive(TestUtil.MakeTestGuid(1), TestUtil.MakeTestGuid(1), "");
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            Assert.Contains(controller.Response.Headers, h => h.Value == "You are not authorized to publish content for this content item.");
            #endregion
        }

        [Fact]
        public async Task GoLive_InvalidRequestStatus()
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user1");
            // Create a new publicationrequest record with blocking status
            TestResources.DbContextObject.ContentPublicationRequest.Add(new ContentPublicationRequest
            {
                Id = TestUtil.MakeTestGuid(999),
                ApplicationUserId = TestUtil.MakeTestGuid(1),
                RootContentItemId = TestUtil.MakeTestGuid(3),
                RequestStatus = PublicationStatus.Processing,
                ReductionRelatedFilesObj = new List<ReductionRelatedFiles> { },
                CreateDateTimeUtc = DateTime.UtcNow - new TimeSpan(0, 1, 0),
            });
            #endregion

            #region Act
            var view = await controller.GoLive(TestUtil.MakeTestGuid(3), TestUtil.MakeTestGuid(999), "");
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            Assert.Equal(422, (view as StatusCodeResult).StatusCode);
            Assert.Contains(controller.Response.Headers, h => h.Value == "Go-Live request references an invalid publication request.");
            #endregion
        }

        [Fact]
        public async Task Reject_Unauthorized()
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("user3");
            #endregion

            #region Act
            var view = await controller.Reject(TestUtil.MakeTestGuid(1), TestUtil.MakeTestGuid(1));
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            Assert.Contains(controller.Response.Headers, h => h.Value == "You are not authorized to publish content for this content item.");
            #endregion
        }

        [Theory]
        [InlineData(3, 999, PublicationStatus.Queued, "user1", "The requested publication request does not exist.")]
        [InlineData(3, 3, PublicationStatus.Processing, "user1", "The specified publication request is not currently queued.")]
        public async Task Reject_BadRequest(int rootContentItemId, int pubRequestId, PublicationStatus initialPubRequestStatus, string UserName, string ExpectedHeaderString)
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser(UserName);
            ContentPublicationRequest pubRequest = TestResources.DbContextObject.ContentPublicationRequest.SingleOrDefault(r => r.RootContentItemId == TestUtil.MakeTestGuid(rootContentItemId));
            if (pubRequest != null)
            {
                pubRequest.RootContentItemId = TestUtil.MakeTestGuid(rootContentItemId);
                pubRequest.RootContentItem = TestResources.DbContextObject.RootContentItem.SingleOrDefault(rc => rc.Id == TestUtil.MakeTestGuid(rootContentItemId));
                pubRequest.RequestStatus = initialPubRequestStatus;
            }
            #endregion

            #region Act
            var view = await controller.Reject(TestUtil.MakeTestGuid(rootContentItemId), TestUtil.MakeTestGuid(pubRequestId));
            #endregion

            #region Assert
            Assert.IsType<BadRequestResult>(view);
            Assert.Contains(controller.Response.Headers, h => h.Value == ExpectedHeaderString);
            #endregion
        }

    }
}
