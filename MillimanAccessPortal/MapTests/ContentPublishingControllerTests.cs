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
using System.Reflection;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MapTests
{
    [Collection("DatabaseLifetime collection")]
    [LogTestBeginEnd]
    public class ContentPublishingControllerTests
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;

        public ContentPublishingControllerTests(DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _dbLifeTimeFixture = dbLifeTimeFixture;
        }

        /// <summary>Constructs a controller with the specified active user.</summary>
        /// <param name="Username"></param>
        /// <returns>ContentPublishingController</returns>
        private async Task<ContentPublishingController> GetControllerForUser(TestInitialization TestResources, string Username)
        {
            var testController = new ContentPublishingController(
                TestResources.AuditLogger,
                TestResources.AuthorizationService,
                TestResources.DbContext,
                TestResources.FileSystemTasks,
                TestResources.GoLiveTaskQueue,
                TestResources.UserManager,
                TestResources.Configuration,
                TestResources.PowerBiConfig,
                TestResources.QvConfig,
                TestResources.PublicationPostProcessingTaskQueue,
                TestResources.ContentPublishingAdminQueries
                );

            try
            {
                Username = (await TestResources.UserManager.FindByNameAsync(Username)).UserName;
            }
            catch (NullReferenceException)
            {
                throw new ArgumentException($"Username '{Username}' is not present in the test database.");
            }
            testController.ControllerContext = TestResources.GenerateControllerContext(Username);
            testController.HttpContext.Session = new MockSession();

            return testController;
        }

        [Fact]
        public async Task Index_ReturnsView()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                var view = controller.Index();
                #endregion

                #region Assert
                Assert.IsType<ViewResult>(view);
                #endregion
            }
        }

        [Theory]
        [InlineData(999, 1, true)]
        [InlineData(1, 999, true)]
        [InlineData(1, 1, false)]
        public async Task CreateRootContentItem_ErrorInvalid(int clientIdArg, int contentTypeIdArg, bool useContentName)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                Guid clientId = TestUtil.MakeTestGuid(clientIdArg);
                Guid contentTypeId = TestUtil.MakeTestGuid(contentTypeIdArg);
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                var validRootContentItem = new RootContentItem
                {
                    ClientId = clientId,
                    ContentTypeId = contentTypeId,
                    DoesReduce = false,
                };
                if (useContentName)
                {
                    validRootContentItem.ContentName = "CreateRootContentItem_ErrorInvalid";
                }
                var jObject = JObject.FromObject(validRootContentItem, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                #endregion

                #region Act
                int preCount = TestResources.DbContext.RootContentItem.Count();
                var view = await controller.CreateRootContentItem(jObject);
                int postCount = TestResources.DbContext.RootContentItem.Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, (view as StatusCodeResult).StatusCode);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content publisher
        [InlineData("user1", 2)]  // User has no role in the client
        public async Task CreateRootContentItem_ErrorUnauthorized(String userName, int clientIdArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                Guid clientId = TestUtil.MakeTestGuid(clientIdArg);
                ContentPublishingController controller = await GetControllerForUser(TestResources, userName);
                var validRootContentItem = new RootContentItem
                {
                    ClientId = clientId,
                    ContentTypeId = TestUtil.MakeTestGuid(1),
                    ContentName = "",
                    DoesReduce = false,
                };
                var jObject = JObject.FromObject(validRootContentItem, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                #endregion

                #region Act
                int preCount = TestResources.DbContext.RootContentItem.Count();
                var view = await controller.CreateRootContentItem(jObject);
                int postCount = TestResources.DbContext.RootContentItem.Count();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Fact]
        public async Task CreateRootContentItem_ReturnsJson()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                var validRootContentItem = new RootContentItem
                {
                    ClientId = TestUtil.MakeTestGuid(1),
                    ContentTypeId = TestResources.DbContext.ContentType.Single(t => t.TypeEnum == ContentTypeEnum.Qlikview).Id,
                    ContentName = "CreateRootContentItem_ReturnsJson",
                    DoesReduce = false,
                };
                var jObject = JObject.FromObject(validRootContentItem, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                #endregion

                #region Act
                var view = await controller.CreateRootContentItem(jObject);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                IEnumerable<PropertyInfo> resultProperties = result.Value.GetType().GetRuntimeProperties();
                Assert.Equal(typeof(RootContentItemSummary), resultProperties.Single(p => p.Name == "summary").PropertyType);
                Assert.Equal(typeof(RootContentItemDetail), resultProperties.Single(p => p.Name == "detail").PropertyType);

                //Assert.IsType<>(result.Value);
                #endregion
            }
        }

        [Fact]
        public async Task CreateRootContentItem_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                var validRootContentItem = new RootContentItem
                {
                    ClientId = TestUtil.MakeTestGuid(1),
                    ContentTypeId = TestResources.DbContext.ContentType.Single(t => t.TypeEnum == ContentTypeEnum.Qlikview).Id,
                    ContentName = "CreateRootContentItem_Success",
                    DoesReduce = false,
                };
                var jObject = JObject.FromObject(validRootContentItem, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                #endregion

                #region Act
                int preCount = TestResources.DbContext.RootContentItem.Count();
                var view = await controller.CreateRootContentItem(jObject);
                int postCount = TestResources.DbContext.RootContentItem.Count();
                #endregion

                #region Assert
                Assert.Equal(preCount + 1, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(999)]
        public async Task DeleteRootContentItem_ErrorInvalid(int rootContentItemIdArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                ApplicationUser user = await TestResources.UserManager.FindByNameAsync("user1");
                #endregion

                #region Act
                Guid rootContentItemId = TestUtil.MakeTestGuid(rootContentItemIdArg);
                int preCount = TestResources.DbContext.RootContentItem.Count();
                var view = await controller.DeleteRootContentItem(rootContentItemId);
                int postCount = TestResources.DbContext.RootContentItem.Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, (view as StatusCodeResult).StatusCode);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content publisher
        [InlineData("user1", 2)]  // User has no role in the client
        public async Task DeleteRootContentItem_ErrorUnauthorized(String userName, int rootContentItemIdArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, userName);
                ApplicationUser user = await TestResources.UserManager.FindByNameAsync("user1");
                #endregion

                #region Act
                Guid rootContentItemId = TestUtil.MakeTestGuid(rootContentItemIdArg);
                int preCount = TestResources.DbContext.RootContentItem.Count();
                var view = await controller.DeleteRootContentItem(rootContentItemId);
                int postCount = TestResources.DbContext.RootContentItem.Count();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Fact]
        public async Task DeleteRootContentItem_ReturnsJson()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {

                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                ApplicationUser user = await TestResources.UserManager.FindByNameAsync("user1");
                #endregion

                #region Act
                var view = await controller.DeleteRootContentItem(TestUtil.MakeTestGuid(3));
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<RootContentItemDetail>(result.Value);
                #endregion
            }
        }

        [Fact]
        public async Task DeleteRootContentItem_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                ApplicationUser user = await TestResources.UserManager.FindByNameAsync("user1");
                #endregion

                #region Act
                int preCount = TestResources.DbContext.RootContentItem.Count();
                var view = await controller.DeleteRootContentItem(TestUtil.MakeTestGuid(3));
                int postCount = TestResources.DbContext.RootContentItem.Count();
                #endregion

                #region Assert
                Assert.Equal(preCount - 1, postCount);
                #endregion
            }
        }

        [Fact]
        public async Task Publish_UnauthorizedUser()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user3");
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
        }

        [Fact]
        public async Task Publish_MissingFileUploadRecord()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                // user1 is authorized with role 4 (ContentPublisher) to RootContentItem 3
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                PublishRequest RequestArg = new PublishRequest
                {
                    RootContentItemId = TestUtil.MakeTestGuid(3),
                    NewRelatedFiles = new List<UploadedRelatedFile>
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
        }

        [Theory]
        [InlineData(PublicationStatus.Queued)]
        [InlineData(PublicationStatus.Processing)]
        public async Task Publish_PreviousPublishingPending(PublicationStatus ExistingRequestStatus)
        {
            // user1 is authorized with role 4 (ContentPublisher) to RootContentItem 3
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                PublishRequest RequestArg = new PublishRequest
                {
                    RootContentItemId = TestUtil.MakeTestGuid(3),
                    NewRelatedFiles = new List<UploadedRelatedFile>(),
                };
                // Create a new publicationrequest record with blocking status
                TestResources.DbContext.ContentPublicationRequest.Add(new ContentPublicationRequest
                {
                    Id = TestUtil.MakeTestGuid(999),
                    ApplicationUserId = TestUtil.MakeTestGuid(1),
                    RootContentItemId = TestUtil.MakeTestGuid(3),
                    RequestStatus = ExistingRequestStatus,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles> { },
                    CreateDateTimeUtc = DateTime.UtcNow - new TimeSpan(0, 1, 0),
                });
                TestResources.DbContext.SaveChanges();
                #endregion

                #region Act
                var view = await controller.Publish(RequestArg);
                #endregion

                #region Assert
                Assert.IsType<BadRequestResult>(view);
                Assert.Contains(controller.Response.Headers, h => h.Value == "A previous publication is pending for this content.");
                #endregion
            }
        }

        [Theory]
        [InlineData(ReductionStatusEnum.Queued)]
        [InlineData(ReductionStatusEnum.Reducing)]
        public async Task Publish_PreviousReductionPending(ReductionStatusEnum ExistingTaskStatus)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                // user1 is authorized with role 4 (ContentPublisher) to RootContentItem 3
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                PublishRequest RequestArg = new PublishRequest
                {
                    RootContentItemId = TestUtil.MakeTestGuid(3),
                    NewRelatedFiles = new List<UploadedRelatedFile>(),
                };
                // Create a new publicationrequest record with blocking status
                TestResources.DbContext.ContentReductionTask.Add(new ContentReductionTask
                {
                    Id = Guid.NewGuid(),
                    SelectionGroup = TestResources.DbContext.SelectionGroup.Single(g => g.RootContentItemId == TestUtil.MakeTestGuid(3)),
                    ApplicationUserId = TestUtil.MakeTestGuid(1),
                    ReductionStatus = ExistingTaskStatus,
                    CreateDateTimeUtc = DateTime.UtcNow,
                    MasterFilePath = "",
                });
                TestResources.DbContext.SaveChanges();
                #endregion

                #region Act
                var view = await controller.Publish(RequestArg);
                #endregion

                #region Assert
                Assert.IsType<BadRequestResult>(view);
                Assert.Contains(controller.Response.Headers, h => h.Value == "A previous reduction task is pending for this content.");
                #endregion
            }
        }

        [Fact]
        public async Task GoLive_UnauthorizedUser()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user3");
                var goLiveViewModel = new GoLiveViewModel
                {
                    RootContentItemId = TestUtil.MakeTestGuid(1),
                    PublicationRequestId = TestUtil.MakeTestGuid(1),
                    ValidationSummaryId = "",
                };
                #endregion

                #region Act
                var view = await controller.GoLive(goLiveViewModel);
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                Assert.Contains(controller.Response.Headers, h => h.Value == "You are not authorized to publish content for this content item.");
                #endregion
            }
        }

        [Fact]
        public async Task GoLive_InvalidRequestStatus()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                // Create a new publicationrequest record with blocking status
                TestResources.DbContext.ContentPublicationRequest.Add(new ContentPublicationRequest
                {
                    Id = TestUtil.MakeTestGuid(999),
                    ApplicationUserId = TestUtil.MakeTestGuid(1),
                    RootContentItemId = TestUtil.MakeTestGuid(3),
                    RequestStatus = PublicationStatus.Processing,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles> { },
                    CreateDateTimeUtc = DateTime.UtcNow - new TimeSpan(0, 1, 0),
                });
                var goLiveViewModel = new GoLiveViewModel
                {
                    RootContentItemId = TestUtil.MakeTestGuid(3),
                    PublicationRequestId = TestUtil.MakeTestGuid(999),
                    ValidationSummaryId = "",
                };
                #endregion

                #region Act
                var view = await controller.GoLive(goLiveViewModel);
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, (view as StatusCodeResult).StatusCode);
                Assert.Contains(controller.Response.Headers, h => h.Value == "Go-Live request references an invalid publication request.");
                #endregion
            }
        }

        [Fact]
        public async Task Reject_Unauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user3");
                var goLiveViewModel = new GoLiveViewModel
                {
                    RootContentItemId = TestUtil.MakeTestGuid(1),
                    PublicationRequestId = TestUtil.MakeTestGuid(1),
                    ValidationSummaryId = "",
                };
                #endregion

                #region Act
                var view = await controller.Reject(goLiveViewModel);
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                Assert.Contains(controller.Response.Headers, h => h.Value == "You are not authorized to publish content for this content item.");
                #endregion
            }
        }

        [Theory]
        [InlineData(3, 999, PublicationStatus.Queued, "user1", "The requested publication request does not exist.")]
        [InlineData(3, 3, PublicationStatus.Processing, "user1", "The specified publication request is not currently processed.")]
        public async Task Reject_BadRequest(int rootContentItemId, int pubRequestId, PublicationStatus initialPubRequestStatus, string UserName, string ExpectedHeaderString)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, UserName);
                ContentPublicationRequest pubRequest = TestResources.DbContext.ContentPublicationRequest.SingleOrDefault(r => r.RootContentItemId == TestUtil.MakeTestGuid(rootContentItemId));
                if (pubRequest != null)
                {
                    pubRequest.RootContentItemId = TestUtil.MakeTestGuid(rootContentItemId);
                    pubRequest.RootContentItem = TestResources.DbContext.RootContentItem.SingleOrDefault(rc => rc.Id == TestUtil.MakeTestGuid(rootContentItemId));
                    pubRequest.RequestStatus = initialPubRequestStatus;
                }
                var goLiveViewModel = new GoLiveViewModel
                {
                    RootContentItemId = TestUtil.MakeTestGuid(rootContentItemId),
                    PublicationRequestId = TestUtil.MakeTestGuid(pubRequestId),
                    ValidationSummaryId = "",
                };
                #endregion

                #region Act
                var view = await controller.Reject(goLiveViewModel);
                #endregion

                #region Assert
                Assert.IsType<BadRequestResult>(view);
                Assert.Contains(controller.Response.Headers, h => h.Value == ExpectedHeaderString);
                #endregion
            }
        }

        [Fact]
        public async Task UpdateRootContentItem_Unauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user3");
                RootContentItem dbItem = TestResources.DbContext.RootContentItem.Find(TestUtil.MakeTestGuid(1));
                RootContentItem updateModel = new RootContentItem
                {
                    Id = dbItem.Id,
                    ContentTypeId = dbItem.ContentTypeId,
                    ClientId = dbItem.ClientId,
                    ContentName = dbItem.ContentName,
                    Notes = "This note is added",
                };
                var jObject = JObject.FromObject(updateModel, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                Assert.Null(dbItem.Notes);
                #endregion

                #region Act
                var view = await controller.UpdateRootContentItem(jObject);
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                Assert.Contains(controller.Response.Headers, h => h.Value == "You are not authorized to update this content item.");
                #endregion
            }
        }

        [Fact]
        public async Task UpdateRootContentItem_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                RootContentItem dbItem = TestResources.DbContext.RootContentItem.Find(TestUtil.MakeTestGuid(3));
                RootContentItem updateModel = new RootContentItem
                {
                    Id = dbItem.Id,
                    ContentTypeId = dbItem.ContentTypeId,
                    ContentType = dbItem.ContentType,
                    ClientId = dbItem.ClientId,
                    Client = dbItem.Client,
                    ContentName = dbItem.ContentName,
                    Notes = "This note is added",
                };
                var jObject = JObject.FromObject(updateModel, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                Assert.Null(dbItem.Notes);
                #endregion

                #region Act
                var view = await controller.UpdateRootContentItem(jObject);
                #endregion

                #region Assert
                Assert.False(string.IsNullOrWhiteSpace(dbItem.Notes));
                Assert.Equal(updateModel.Notes, dbItem.Notes);
                #endregion
            }
        }

        [Fact]
        public async Task UpdateRootContentItem_TypeSpecificProperties_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentPublishingController controller = await GetControllerForUser(TestResources, "user1");
                RootContentItem dbItem = TestResources.DbContext.RootContentItem.Find(TestUtil.MakeTestGuid(4));
                PowerBiContentItemProperties props = new PowerBiContentItemProperties
                {
                    FilterPaneEnabled = true,
                    NavigationPaneEnabled = true,
                    BookmarksPaneEnabled = true,
                };
                RootContentItem updateModel = new RootContentItem
                {
                    Id = dbItem.Id,
                    ContentTypeId = dbItem.ContentTypeId,
                    ContentType = dbItem.ContentType,
                    ClientId = dbItem.ClientId,
                    Client = dbItem.Client,
                    ContentName = dbItem.ContentName,
                };
                updateModel.TypeSpecificDetailObject = props;
                var jObject = JObject.FromObject(updateModel, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                #endregion

                #region Act
                var view = await controller.UpdateRootContentItem(jObject);
                #endregion

                #region Assert
                PowerBiContentItemProperties savedProps = Assert.IsType<PowerBiContentItemProperties>(dbItem.TypeSpecificDetailObject);
                Assert.Equal(props.NavigationPaneEnabled, savedProps.NavigationPaneEnabled);
                Assert.Equal(props.FilterPaneEnabled, savedProps.FilterPaneEnabled);
                Assert.Equal(props.BookmarksPaneEnabled, savedProps.BookmarksPaneEnabled);
                #endregion
            }
        }

    }
}
