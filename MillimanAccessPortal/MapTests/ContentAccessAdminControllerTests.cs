/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Unit tests for the content access admin controller
 * DEVELOPER NOTES: 
 */

using MapDbContextLib.Context;
using TestResourcesLib;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using MapCommonLib;
using MapDbContextLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MillimanAccessPortal.Models.ContentAccessAdmin;

namespace MapTests
{
    [Collection("DatabaseLifetime collection")]
    [LogTestBeginEnd]
    public class ContentAccessAdminControllerTests
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;

        public ContentAccessAdminControllerTests(DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _dbLifeTimeFixture = dbLifeTimeFixture;
        }

        /// <summary>Constructs a controller with the specified active user.</summary>
        /// <param name="Username"></param>
        /// <returns>ContentAccessAdminController</returns>
        private async Task<ContentAccessAdminController> GetControllerForUser(TestInitialization TestResources, string Username)
        {
            ContentAccessAdminController testController = new ContentAccessAdminController(
                TestResources.AuditLogger,
                TestResources.AuthorizationService,
                TestResources.DbContext,
                TestResources.ContentAccessAdminQueries,
                TestResources.UserManager,
                TestResources.Configuration,
                TestResources.QvConfig
                );

            try
            {
                Username = (await TestResources.UserManager.FindByNameAsync(Username)).UserName;
            }
            catch (System.NullReferenceException)
            {
                throw new ArgumentException($"Username '{Username}' is not present in the test database.");
            }
            testController.ControllerContext = TestResources.GenerateControllerContext(Username);
            testController.HttpContext.Session = new MockSession();

            return testController;
        }

        [Fact]
        public async Task Index_ErrorUnauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user2");
                #endregion

                #region Act
                var view = await controller.Index();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        [Fact]
        public async Task Index_ReturnsView()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                var view = await controller.Index();
                #endregion

                #region Assert
                Assert.IsType<ViewResult>(view);
                #endregion
            }
        }

        [Fact]
        public async Task ClientFamilyList_ErrorUnauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user2");
                #endregion

                #region Act
                var view = await controller.Clients();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        [Fact]
        public async Task ClientFamilyList_ReturnsJson()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                var view = await controller.Clients();
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<ClientsResponseModel>(result.Value);
                #endregion
            }
        }

        [Fact]
        public async Task RootContentItems_ErrorInvalid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                var view = await controller.ContentItems(TestUtil.MakeTestGuid(999));
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        [Fact]
        public async Task RootContentItems_ErrorUnauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user2");
                #endregion

                #region Act
                var view = await controller.ContentItems(TestUtil.MakeTestGuid(1));
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        [Fact]
        public async Task RootContentItems_ReturnsJson()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                var view = await controller.ContentItems(TestUtil.MakeTestGuid(1));
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<ContentItemsResponseModel>(result.Value);
                #endregion
            }
        }

        [Theory]
        [InlineData(999)]
        public async Task SelectionGroups_ErrorInvalid(int RootContentItemId)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                var view = await controller.SelectionGroups(TestUtil.MakeTestGuid(RootContentItemId));
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 2)]  // User has no role in the root content item
        public async Task SelectionGroups_ErrorUnauthorized(String UserName, int RootContentItemId)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, UserName);
                #endregion

                #region Act
                var view = await controller.SelectionGroups(TestUtil.MakeTestGuid(RootContentItemId));
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        [Fact]
        public async Task SelectionGroups_ReturnsJson()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                var view = await controller.SelectionGroups(TestUtil.MakeTestGuid(1));
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<SelectionGroupsResponseModel>(result.Value);
                #endregion
            }
        }

        [Theory]
        [InlineData(999)]
        public async Task CreateSelectionGroup_ErrorInvalid(int RootContentItemId)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                int preCount = TestResources.DbContext.SelectionGroup.Count();
                var view = await controller.CreateGroup(new CreateGroupRequestModel
                {
                    ContentItemId = TestUtil.MakeTestGuid(RootContentItemId),
                    Name = "GroupName",
                });
                int postCount = TestResources.DbContext.SelectionGroup.Count();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 2)]  // User has no role in the root content item
        public async Task CreateSelectionGroup_ErrorUnauthorized(String UserName, int RootContentItemId)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, UserName);
                #endregion

                #region Act
                int preCount = TestResources.DbContext.SelectionGroup.Count();
                var view = await controller.CreateGroup(new CreateGroupRequestModel
                {
                    ContentItemId = TestUtil.MakeTestGuid(RootContentItemId),
                    Name = "GroupName",
                });
                int postCount = TestResources.DbContext.SelectionGroup.Count();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Fact]
        public async Task CreateSelectionGroup_ReturnsJson()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                TestResources.HttpContextAccessor.HttpContext = controller.HttpContext;
                #endregion

                #region Act
                var view = await controller.CreateGroup(new CreateGroupRequestModel
                {
                    ContentItemId = TestUtil.MakeTestGuid(1),
                    Name = "GroupName",
                });
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<CreateGroupResponseModel>(result.Value);
                #endregion
            }
        }

        [Fact]
        public async Task CreateSelectionGroup_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                TestResources.HttpContextAccessor.HttpContext = controller.HttpContext;
                CreateGroupRequestModel requestModel = new CreateGroupRequestModel
                {
                    ContentItemId = TestUtil.MakeTestGuid(1),
                    Name = "GroupName",
                };
                #endregion

                #region Act
                int preCount = TestResources.DbContext.SelectionGroup.Count();
                var result = await controller.CreateGroup(requestModel);
                int postCount = TestResources.DbContext.SelectionGroup.Count();
                #endregion

                #region Assert
                var jsonResult = Assert.IsType<JsonResult>(result);
                Assert.Equal(preCount + 1, postCount);
                var responseModel = Assert.IsType<CreateGroupResponseModel>(jsonResult.Value);
                Assert.Equal(requestModel.Name, responseModel.Group.Name);
                #endregion
            }
        }

        [Theory]
        [InlineData(1, 999)]  // user does not exist
        [InlineData(2, 3)]    // user ID does not have appropriate role in root content item
        [InlineData(2, 2)]    // user ID already belongs to another selection group for this root content item
        public async Task UpdateSelectionGroup_ErrorInvalid(int SelectionGroupId, int UserId)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                var MembershipSet = new List<Guid>
            {
                TestUtil.MakeTestGuid(UserId),
                TestUtil.MakeTestGuid(1),
            };
                #endregion

                #region Act
                int preCount = TestResources.DbContext.UserInSelectionGroup.Count();
                var view = await controller.UpdateGroup(new UpdateGroupRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(SelectionGroupId),
                    Name = "",
                    Users = MembershipSet,
                });
                int postCount = TestResources.DbContext.UserInSelectionGroup.Count();
                #endregion

                #region Assert
                var typedView = Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, typedView.StatusCode);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("user1", 999)]  // Selection group does not exist
        [InlineData("user2", 1)]    // User is not content access admin
        [InlineData("user1", 3)]    // User has no role in the root content item
        public async Task UpdateSelectionGroup_ErrorUnauthorized(String UserName, int SelectionGroupId)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, UserName);
                var MembershipSet = new List<Guid>
            {
                TestUtil.MakeTestGuid(2),
                TestUtil.MakeTestGuid(1),
            };
                #endregion

                #region Act
                int preCount = TestResources.DbContext.UserInSelectionGroup.Count();
                var view = await controller.UpdateGroup(new UpdateGroupRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(SelectionGroupId),
                    Name = "",
                    Users = MembershipSet,
                });
                int postCount = TestResources.DbContext.UserInSelectionGroup.Count();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Fact]
        public async Task UpdateSelectionGroup_ReturnsJson()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                TestResources.HttpContextAccessor.HttpContext = controller.HttpContext;
                var MembershipSet = new List<Guid>
            {
                TestUtil.MakeTestGuid(2),
            };
                #endregion

                #region Act
                var view = await controller.UpdateGroup(new UpdateGroupRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(1),
                    Name = "",
                    Users = MembershipSet,
                });
                #endregion

                #region Assert
                JsonResult typedView = Assert.IsType<JsonResult>(view);
                Assert.IsType<UpdateGroupResponseModel>(typedView.Value);
                #endregion
            }
        }

        [Fact]
        public async Task UpdateSelectionGroup_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                TestResources.HttpContextAccessor.HttpContext = controller.HttpContext;
                var MembershipSet = new List<Guid>
            {
                TestUtil.MakeTestGuid(2),
                TestUtil.MakeTestGuid(1),
            };
                #endregion

                #region Act
                int preCount = TestResources.DbContext.UserInSelectionGroup.Count();
                var view = await controller.UpdateGroup(new UpdateGroupRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(1),
                    Name = "",
                    Users = MembershipSet,
                });
                int postCount = TestResources.DbContext.UserInSelectionGroup.Count();
                #endregion

                #region Assert
                Assert.Equal(preCount + 1, postCount);
                #endregion
            }
        }

        [Fact]
        public async Task DeleteSelectionGroup_BlockedByPendingPublication()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                ContentPublicationRequest newPubRequest = new ContentPublicationRequest
                {
                    Id = TestUtil.MakeTestGuid(100),
                    RootContentItemId = TestUtil.MakeTestGuid(1),
                    RequestStatus = PublicationStatus.Queued,
                    ApplicationUserId = TestUtil.MakeTestGuid(1),
                };
                TestResources.DbContext.ContentPublicationRequest.Add(newPubRequest);
                TestResources.DbContext.SaveChanges();
                var deleteRequestModel = new DeleteGroupRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(1),
                };
                #endregion

                #region Act
                int groupsPreCount = TestResources.DbContext.SelectionGroup.Count();
                int userPreCount = TestResources.DbContext.UserInSelectionGroup.Count();

                var view = await controller.DeleteGroup(deleteRequestModel);

                int groupsPostCount = TestResources.DbContext.SelectionGroup.Count();
                int userPostCount = TestResources.DbContext.UserInSelectionGroup.Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, (view as StatusCodeResult).StatusCode);
                Assert.Equal(groupsPreCount, groupsPostCount);
                Assert.Equal(userPreCount, userPostCount);
                #endregion
            }
        }

        [Fact]
        public async Task DeleteSelectionGroup_BlockedByPendingReduction()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                ContentReductionTask newReductionTask = new ContentReductionTask
                {
                    CreateDateTimeUtc = DateTime.UtcNow,
                    Id = TestUtil.MakeTestGuid(100),
                    SelectionGroupId = TestUtil.MakeTestGuid(1),
                    ReductionStatus = ReductionStatusEnum.Queued,
                    MasterFilePath = "",
                    ApplicationUserId = TestUtil.MakeTestGuid(1),
                };
                TestResources.DbContext.ContentReductionTask.Add(newReductionTask);
                TestResources.DbContext.SaveChanges();
                var deleteRequestModel = new DeleteGroupRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(1),
                };
                #endregion

                #region Act
                int groupsPreCount = TestResources.DbContext.SelectionGroup.Count();
                int userPreCount = TestResources.DbContext.UserInSelectionGroup.Count();

                var view = await controller.DeleteGroup(deleteRequestModel);

                int groupsPostCount = TestResources.DbContext.SelectionGroup.Count();
                int userPostCount = TestResources.DbContext.UserInSelectionGroup.Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, (view as StatusCodeResult).StatusCode);
                Assert.Equal(groupsPreCount, groupsPostCount);
                Assert.Equal(userPreCount, userPostCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 3)]  // User has no role in the root content item
        public async Task DeleteSelectionGroup_ErrorUnauthorized(String UserName, int SelectionGroupId)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, UserName);
                #endregion

                #region Act
                int groupsPreCount = TestResources.DbContext.SelectionGroup.Count();
                int userPreCount = TestResources.DbContext.UserInSelectionGroup.Count();

                var view = await controller.DeleteGroup(new DeleteGroupRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(SelectionGroupId),
                });

                int groupsPostCount = TestResources.DbContext.SelectionGroup.Count();
                int userPostCount = TestResources.DbContext.UserInSelectionGroup.Count();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                Assert.Equal(groupsPreCount, groupsPostCount);
                Assert.Equal(userPreCount, userPostCount);
                #endregion
            }
        }

        [Fact]
        public async Task DeleteSelectionGroup_ReturnsJson()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                TestResources.HttpContextAccessor.HttpContext = controller.HttpContext;
                #endregion

                #region Act
                var view = await controller.DeleteGroup(new DeleteGroupRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(1),
                });
                #endregion

                #region Assert
                JsonResult typedView = Assert.IsType<JsonResult>(view);
                Assert.IsType<DeleteGroupResponseModel>(typedView.Value);
                #endregion
            }
        }

        [Fact]
        public async Task DeleteSelectionGroup_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                TestResources.HttpContextAccessor.HttpContext = controller.HttpContext;
                #endregion

                #region Act
                int groupsPreCount = TestResources.DbContext.SelectionGroup.Count();

                var view = await controller.DeleteGroup(new DeleteGroupRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(1),
                });

                int groupsPostCount = TestResources.DbContext.SelectionGroup.Count();
                #endregion

                #region Assert
                Assert.Equal(groupsPreCount, groupsPostCount + 1);
                #endregion
            }
        }

        [Theory]
        [InlineData(999)]
        public async Task Selections_ErrorInvalid(int SelectionGroupId)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                var view = await controller.SelectionGroups(TestUtil.MakeTestGuid(SelectionGroupId));
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 3)]  // User has no role in the root content item
        public async Task Selections_ErrorUnauthorized(String UserName, int SelectionGroupId)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, UserName);
                #endregion

                #region Act
                var view = await controller.Selections(TestUtil.MakeTestGuid(SelectionGroupId));
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        [Fact]
        public async Task Selections_ReturnsJson()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                var view = await controller.Selections(TestUtil.MakeTestGuid(1));
                #endregion

                #region Assert
                JsonResult typedView = Assert.IsType<JsonResult>(view);
                Assert.IsType<SelectionsResponseModel>(typedView.Value);
                #endregion
            }
        }

        [Theory]
        [InlineData(999,    2, new ReductionStatusEnum[] { })]  // Selection group does not exist
        [InlineData(  1,  999, new ReductionStatusEnum[] { })]  // Hierarchy field value does not exist
        [InlineData(  1,    3, new ReductionStatusEnum[] { })]  // Hierarchy field value does not belong to the correct root content item
        [InlineData(  4,    1, new ReductionStatusEnum[] { })]  // Content has not been published for the root content item
        [InlineData(  1, null, new ReductionStatusEnum[] { })]  // The submit selections match the currently reduced selections
        [InlineData(  1,    2, new ReductionStatusEnum[] { ReductionStatusEnum.Queued    })]  // An outstanding reduction task exists for the root content item
        [InlineData(  1,    2, new ReductionStatusEnum[] { ReductionStatusEnum.Reducing  })]  // "
        [InlineData(  1,    2, new ReductionStatusEnum[] { ReductionStatusEnum.Reduced   })]  // "
        public async Task SingleReduction_ErrorInvalid(int SelectionGroupIdArg, int? HierarchyFieldValueIdArg, ReductionStatusEnum[] Tasks)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                Guid SelectionGroupId = TestUtil.MakeTestGuid(SelectionGroupIdArg);
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                var Selections = HierarchyFieldValueIdArg.HasValue
                    ? new Guid[]
                    {
                    TestUtil.MakeTestGuid(HierarchyFieldValueIdArg.Value),
                    }
                    : new Guid[] { };

                foreach (var Status in Tasks)
                {
                    TestResources.DbContext.ContentReductionTask.Add(new ContentReductionTask
                    {
                        ReductionStatus = Status,
                        ContentPublicationRequestId = null,
                        SelectionGroupId = SelectionGroupId,
                        ApplicationUserId = TestUtil.MakeTestGuid(1)
                    });
                }
                #endregion

                #region Act
                int tasksPreCount = TestResources.DbContext.ContentReductionTask.Count();

                var view = await controller.UpdateSelections(new UpdateSelectionsRequestModel
                {
                    GroupId = SelectionGroupId,
                    IsMaster = false,
                    Selections = Selections.ToList(),
                });

                int tasksPostCount = TestResources.DbContext.ContentReductionTask.Count();
                #endregion

                #region Assert
                var typedView = Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, typedView.StatusCode);
                Assert.Equal(tasksPreCount, tasksPostCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 3)]  // User has no role in the root content item
        public async Task SingleReduction_ErrorUnauthorized(String UserName, int SelectionGroupId)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, UserName);
                var Selections = new Guid[]
                {
                TestUtil.MakeTestGuid(2),
                };
                TestResources.DbContext.ContentReductionTask.Add(new ContentReductionTask
                {
                    ReductionStatus = ReductionStatusEnum.Live,
                    ContentPublicationRequestId = null,
                    SelectionGroupId = TestUtil.MakeTestGuid(SelectionGroupId),
                    ApplicationUserId = TestUtil.MakeTestGuid(1)
                });
                #endregion

                #region Act
                int tasksPreCount = TestResources.DbContext.ContentReductionTask.Count();

                var view = await controller.UpdateSelections(new UpdateSelectionsRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(SelectionGroupId),
                    IsMaster = false,
                    Selections = Selections.ToList(),
                });

                int tasksPostCount = TestResources.DbContext.ContentReductionTask.Count();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                Assert.Equal(tasksPreCount, tasksPostCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(1, null, new ReductionStatusEnum[] { })]                                // No queued tasks exist
        [InlineData(1, null, new ReductionStatusEnum[] { ReductionStatusEnum.Reduced   })]  // "
        [InlineData(1, null, new ReductionStatusEnum[] { ReductionStatusEnum.Live      })]  // "
        [InlineData(1, null, new ReductionStatusEnum[] { ReductionStatusEnum.Canceled  })]  // "
        [InlineData(1, null, new ReductionStatusEnum[] { ReductionStatusEnum.Rejected })]  // "
        [InlineData(1, null, new ReductionStatusEnum[] { ReductionStatusEnum.Replaced  })]  // "
        [InlineData(1,    1, new ReductionStatusEnum[] { ReductionStatusEnum.Queued    })]  // The queued task is part of a publication request
        public async Task CancelReduction_ErrorInvalid(int SelectionGroupId, int? ContentPublicationRequestId, ReductionStatusEnum[] Tasks)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                foreach (var Status in Tasks)
                {
                    TestResources.DbContext.ContentReductionTask.Add(new ContentReductionTask
                    {
                        ReductionStatus = Status,
                        ContentPublicationRequestId = ContentPublicationRequestId.HasValue ? TestUtil.MakeTestGuid(ContentPublicationRequestId.Value) : (Guid?)null,
                        SelectionGroupId = TestUtil.MakeTestGuid(SelectionGroupId),
                        ApplicationUserId = TestUtil.MakeTestGuid(1)
                    });
                }
                #endregion

                #region Act
                int tasksPreCount = TestResources.DbContext.ContentReductionTask.Count();

                var view = await controller.CancelReduction(new CancelReductionRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(SelectionGroupId),
                });

                int tasksPostCount = TestResources.DbContext.ContentReductionTask.Count();
                #endregion

                #region Assert
                var TypedView = Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, TypedView.StatusCode);
                Assert.Equal(tasksPreCount, tasksPostCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 3)]  // User has no role in the root content item
        public async Task CancelReduction_ErrorUnauthorized(String UserName, int SelectionGroupId)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, UserName);
                TestResources.DbContext.ContentReductionTask.Add(new ContentReductionTask
                {
                    ReductionStatus = ReductionStatusEnum.Queued,
                    ContentPublicationRequestId = null,
                    SelectionGroupId = TestUtil.MakeTestGuid(SelectionGroupId),
                    ApplicationUserId = TestUtil.MakeTestGuid(1)
                });
                #endregion

                #region Act
                int tasksPreCount = TestResources.DbContext.ContentReductionTask.Count();

                var view = await controller.CancelReduction(new CancelReductionRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(SelectionGroupId),
                });

                int tasksPostCount = TestResources.DbContext.ContentReductionTask.Count();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                Assert.Equal(tasksPreCount, tasksPostCount);
                #endregion
            }
        }

        [Fact]
        public async Task CancelReduction_ReturnsJson()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                TestResources.DbContext.ContentReductionTask.Add(new ContentReductionTask
                {
                    ReductionStatus = ReductionStatusEnum.Queued,
                    ContentPublicationRequestId = null,
                    SelectionGroupId = TestUtil.MakeTestGuid(1),
                    ApplicationUserId = TestUtil.MakeTestGuid(1),
                    SelectionCriteriaObj = new ContentReductionHierarchy<ReductionFieldValueSelection>(),
                    MasterFilePath = "",
                    CreateDateTimeUtc = DateTime.MinValue,
                });
                TestResources.DbContext.SaveChanges();
                #endregion

                #region Act
                var view = await controller.CancelReduction(new CancelReductionRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(1),
                });
                #endregion

                #region Assert
                JsonResult typedView = Assert.IsType<JsonResult>(view);
                Assert.IsType<SingleReductionModel>(typedView.Value);
                #endregion
            }
        }

        [Theory]
        [InlineData(1, 1, new ReductionStatusEnum[] { ReductionStatusEnum.Queued })]                                // A queued task created by the requesting user exists
        [InlineData(1, 2, new ReductionStatusEnum[] { ReductionStatusEnum.Queued })]                                // A queued task created by a different user exists
        [InlineData(1, 1, new ReductionStatusEnum[] { ReductionStatusEnum.Canceled, ReductionStatusEnum.Queued })]  // Multiple tasks exist, and at least one is queued
        public async Task CancelReduction_Success(int SelectionGroupId, int UserId, ReductionStatusEnum[] Tasks)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                int i = 10;
                foreach (var Status in Tasks)
                {
                    TestResources.DbContext.ContentReductionTask.Add(new ContentReductionTask
                    {
                        Id = TestUtil.MakeTestGuid(i++),
                        ReductionStatus = Status,
                        ContentPublicationRequestId = null,
                        SelectionGroupId = TestUtil.MakeTestGuid(SelectionGroupId),
                        ApplicationUserId = TestUtil.MakeTestGuid(UserId),
                        SelectionCriteriaObj = new ContentReductionHierarchy<ReductionFieldValueSelection>(),
                        MasterFilePath = "",
                        CreateDateTimeUtc = DateTime.MinValue,
                    });
                }
                TestResources.DbContext.SaveChanges();
                #endregion

                #region Act
                int tasksPreCount = TestResources.DbContext.ContentReductionTask.Count();
                int queuedPreCount = TestResources.DbContext.ContentReductionTask
                    .Where(crt => crt.SelectionGroupId == TestUtil.MakeTestGuid(SelectionGroupId))
                    .Where(crt => crt.ReductionStatus == ReductionStatusEnum.Queued)
                    .Count();

                var view = await controller.CancelReduction(new CancelReductionRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(SelectionGroupId),
                });

                int tasksPostCount = TestResources.DbContext.ContentReductionTask.Count();
                int queuedPostCount = TestResources.DbContext.ContentReductionTask
                    .Where(crt => crt.SelectionGroupId == TestUtil.MakeTestGuid(SelectionGroupId))
                    .Where(crt => crt.ReductionStatus == ReductionStatusEnum.Queued)
                    .Count();
                #endregion

                #region Assert
                Assert.Equal(tasksPreCount, tasksPostCount);
                Assert.Equal(1, queuedPreCount);
                Assert.Equal(0, queuedPostCount);
                #endregion
            }
        }

        [Fact]
        public async Task SetGroupPowerBiEditability_Unauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user2");
                #endregion

                #region Act
                var view = await controller.SetGroupPowerBiEditability(new SetPowerBiEditabilityRequestModel {
                    GroupId = TestUtil.MakeTestGuid(1),
                    Editable = true,
                });
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        [Fact]
        public async Task SetGroupPowerBiEditability_InvalidContentType()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                var view = await controller.SetGroupPowerBiEditability(new SetPowerBiEditabilityRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(1),
                    Editable = true,
                });
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                #endregion
            }
        }

        [Fact]
        public async Task SetGroupPowerBiEditability_IneligiblePowerBiContent()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                var view = await controller.SetGroupPowerBiEditability(new SetPowerBiEditabilityRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(6),
                    Editable = true,
                });
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                #endregion
            }
        }

        [Fact]
        public async Task SetGroupPowerBiEditability_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Reduction))
            {
                #region Arrange
                ContentAccessAdminController controller = await GetControllerForUser(TestResources, "user1");
                #endregion

                #region Act
                var view = await controller.SetGroupPowerBiEditability(new SetPowerBiEditabilityRequestModel
                {
                    GroupId = TestUtil.MakeTestGuid(5),
                    Editable = true,
                });
                #endregion

                #region Assert
                Assert.IsType<JsonResult>(view);
                #endregion
            }
        }
    }
}
