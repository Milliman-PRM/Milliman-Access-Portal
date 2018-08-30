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

namespace MapTests
{
    public class ContentAccessAdminControllerTests
    {
        internal TestInitialization TestResources { get; set; }

        /// <summary>Initializes test resources.</summary>
        /// <remarks>This constructor is called before each test.</remarks>
        public ContentAccessAdminControllerTests()
        {
            TestResources = new TestInitialization();
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Reduction });
        }

        /// <summary>Constructs a controller with the specified active user.</summary>
        /// <param name="Username"></param>
        /// <returns>ContentAccessAdminController</returns>
        public async Task<ContentAccessAdminController> GetControllerForUser(string Username)
        {
            ContentAccessAdminController testController = new ContentAccessAdminController(
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
            catch (System.NullReferenceException)
            {
                throw new ArgumentException($"Username '{Username}' is not present in the test database.");
            }
            testController.ControllerContext = TestInitialization.GenerateControllerContext(Username);
            testController.HttpContext.Session = new MockSession();

            return testController;
        }

        [Fact]
        public async Task Index_ErrorUnauthorized()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user2");
            #endregion

            #region Act
            var view = await controller.Index();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        [Fact]
        public async Task Index_ReturnsView()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            var view = await controller.Index();
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            #endregion
        }

        [Fact]
        public async Task ClientFamilyList_ErrorUnauthorized()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user2");
            #endregion

            #region Act
            var view = await controller.ClientFamilyList();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        [Fact]
        public async Task ClientFamilyList_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            var view = await controller.ClientFamilyList();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Fact]
        public async Task RootContentItems_ErrorInvalid()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            var view = await controller.RootContentItems(MakeGuid(999));
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            Assert.Equal(422, (view as StatusCodeResult).StatusCode);
            #endregion
        }

        [Fact]
        public async Task RootContentItems_ErrorUnauthorized()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user2");
            #endregion

            #region Act
            var view = await controller.RootContentItems(MakeGuid(1));
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        [Fact]
        public async Task RootContentItems_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            var view = await controller.RootContentItems(MakeGuid(1));
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Theory]
        [InlineData(999)]
        public async Task SelectionGroups_ErrorInvalid(int RootContentItemId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            var view = await controller.SelectionGroups(MakeGuid(RootContentItemId));
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            Assert.Equal(422, (view as StatusCodeResult).StatusCode);
            #endregion
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 2)]  // User has no role in the root content item
        public async Task SelectionGroups_ErrorUnauthorized(String UserName, int RootContentItemId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            #endregion

            #region Act
            var view = await controller.SelectionGroups(MakeGuid(RootContentItemId));
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        [Fact]
        public async Task SelectionGroups_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            var view = await controller.SelectionGroups(MakeGuid(1));
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Theory]
        [InlineData(999)]
        public async Task CreateSelectionGroup_ErrorInvalid(int RootContentItemId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.SelectionGroup.Count();
            var view = await controller.CreateSelectionGroup(MakeGuid(RootContentItemId), "GroupName");
            int postCount = TestResources.DbContextObject.SelectionGroup.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            Assert.Equal(422, (view as StatusCodeResult).StatusCode);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 2)]  // User has no role in the root content item
        public async Task CreateSelectionGroup_ErrorUnauthorized(String UserName, int RootContentItemId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.SelectionGroup.Count();
            var view = await controller.CreateSelectionGroup(MakeGuid(RootContentItemId), "GroupName");
            int postCount = TestResources.DbContextObject.SelectionGroup.Count();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Fact]
        public async Task CreateSelectionGroup_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            var view = await controller.CreateSelectionGroup(MakeGuid(1), "GroupName");
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Fact]
        public async Task CreateSelectionGroup_Success()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.SelectionGroup.Count();
            var view = await controller.CreateSelectionGroup(MakeGuid(1), "GroupName");
            int postCount = TestResources.DbContextObject.SelectionGroup.Count();
            #endregion

            #region Assert
            Assert.Equal(preCount + 1, postCount);
            #endregion
        }

        [Theory]
        [InlineData(999, 2, true)]  // selection group does not exist
        [InlineData(1, 999, true)]  // user does not exist
        [InlineData(2, 3, true)]    // user ID does not have appropriate role in root content item
        [InlineData(2, 2, true)]    // user ID already belongs to another selection group for this root content item
        public async Task UpdateSelectionGroup_ErrorInvalid(int SelectionGroupId, int UserId, bool MembershipStatus)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            Dictionary<Guid, bool> MembershipSet = new Dictionary<Guid, bool>
            {
                { MakeGuid(UserId), MembershipStatus },
                { MakeGuid(1), true }
            };
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            var view = await controller.UpdateSelectionGroupUserAssignments(MakeGuid(SelectionGroupId), MembershipSet);
            int postCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            Assert.Equal(422, (view as StatusCodeResult).StatusCode);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 3)]  // User has no role in the root content item
        public async Task UpdateSelectionGroup_ErrorUnauthorized(String UserName, int SelectionGroupId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            Dictionary<Guid, bool> MembershipSet = new Dictionary<Guid, bool>
            {
                { MakeGuid(2), true },
                { MakeGuid(1), true }
            };
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            var view = await controller.UpdateSelectionGroupUserAssignments(MakeGuid(SelectionGroupId), MembershipSet);
            int postCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Fact]
        public async Task UpdateSelectionGroup_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            Dictionary<Guid, bool> MembershipSet = new Dictionary<Guid, bool>
            {
                { MakeGuid(2), true },
                { MakeGuid(1), false }
            };
            #endregion

            #region Act
            var view = await controller.UpdateSelectionGroupUserAssignments(MakeGuid(1), MembershipSet);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UpdateSelectionGroup_Success(bool MembershipStatus)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            Dictionary<Guid, bool> MembershipSet = new Dictionary<Guid, bool>
            {
                { MakeGuid(2), MembershipStatus },
                { MakeGuid(1), MembershipStatus },
            };
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            var view = await controller.UpdateSelectionGroupUserAssignments(MakeGuid(1), MembershipSet);
            int postCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            #endregion

            #region Assert
            Assert.Equal(preCount + (MembershipStatus ? 1 : -1), postCount);
            #endregion
        }

        [Theory]
        [InlineData(999)]
        public async Task DeleteSelectionGroup_ErrorInvalid(int SelectionGroupId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            int groupsPreCount = TestResources.DbContextObject.SelectionGroup.Count();
            int userPreCount = TestResources.DbContextObject.UserInSelectionGroup.Count();

            var view = await controller.DeleteSelectionGroup(MakeGuid(SelectionGroupId));

            int groupsPostCount = TestResources.DbContextObject.SelectionGroup.Count();
            int userPostCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            Assert.Equal(422, (view as StatusCodeResult).StatusCode);
            Assert.Equal(groupsPreCount, groupsPostCount);
            Assert.Equal(userPreCount, userPostCount);
            #endregion
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 3)]  // User has no role in the root content item
        public async Task DeleteSelectionGroup_ErrorUnauthorized(String UserName, int SelectionGroupId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            #endregion

            #region Act
            int groupsPreCount = TestResources.DbContextObject.SelectionGroup.Count();
            int userPreCount = TestResources.DbContextObject.UserInSelectionGroup.Count();

            var view = await controller.DeleteSelectionGroup(MakeGuid(SelectionGroupId));

            int groupsPostCount = TestResources.DbContextObject.SelectionGroup.Count();
            int userPostCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            Assert.Equal(groupsPreCount, groupsPostCount);
            Assert.Equal(userPreCount, userPostCount);
            #endregion
        }

        [Fact]
        public async Task DeleteSelectionGroup_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            var view = await controller.DeleteSelectionGroup(MakeGuid(1));
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Fact]
        public async Task DeleteSelectionGroup_Success()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            int groupsPreCount = TestResources.DbContextObject.SelectionGroup.Count();
            int userPreCount = TestResources.DbContextObject.UserInSelectionGroup.Count();

            var view = await controller.DeleteSelectionGroup(MakeGuid(1));

            int groupsPostCount = TestResources.DbContextObject.SelectionGroup.Count();
            int userPostCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            #endregion

            #region Assert
            Assert.Equal(groupsPreCount, groupsPostCount + 1);
            Assert.Equal(userPreCount, userPostCount + 1);
            #endregion
        }

        [Theory]
        [InlineData(999)]
        public async Task Selections_ErrorInvalid(int SelectionGroupId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            var view = await controller.SelectionGroups(MakeGuid(SelectionGroupId));
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            Assert.Equal(422, (view as StatusCodeResult).StatusCode);
            #endregion
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 3)]  // User has no role in the root content item
        public async Task Selections_ErrorUnauthorized(String UserName, int SelectionGroupId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            #endregion

            #region Act
            var view = await controller.Selections(MakeGuid(SelectionGroupId));
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        [Fact]
        public async Task Selections_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            #endregion

            #region Act
            var view = await controller.Selections(MakeGuid(1));
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
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
            #region Arrange
            Guid SelectionGroupId = MakeGuid(SelectionGroupIdArg);
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            var Selections = HierarchyFieldValueIdArg.HasValue
                ? new Guid[]
                {
                    MakeGuid(HierarchyFieldValueIdArg.Value),
                }
                : new Guid[] { };

            foreach (var Status in Tasks)
            {
                TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
                {
                    ReductionStatus = Status,
                    ContentPublicationRequestId = null,
                    SelectionGroupId = SelectionGroupId,
                    ApplicationUserId = MakeGuid(1)
                });
            }
            #endregion

            #region Act
            int tasksPreCount = TestResources.DbContextObject.ContentReductionTask.Count();
            
            var view = await controller.UpdateSelections(SelectionGroupId, false, Selections);

            int tasksPostCount = TestResources.DbContextObject.ContentReductionTask.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            Assert.Equal(422, (view as StatusCodeResult).StatusCode);
            Assert.Equal(tasksPreCount, tasksPostCount);
            #endregion
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 3)]  // User has no role in the root content item
        public async Task SingleReduction_ErrorUnauthorized(String UserName, int SelectionGroupId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            var Selections = new Guid[]
            {
                MakeGuid(2),
            };
            TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
            {
                ReductionStatus = ReductionStatusEnum.Live,
                ContentPublicationRequestId = null,
                SelectionGroupId = MakeGuid(SelectionGroupId),
                ApplicationUserId = MakeGuid(1)
            });
            #endregion

            #region Act
            int tasksPreCount = TestResources.DbContextObject.ContentReductionTask.Count();

            var view = await controller.UpdateSelections(MakeGuid(SelectionGroupId), false, Selections);

            int tasksPostCount = TestResources.DbContextObject.ContentReductionTask.Count();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            Assert.Equal(tasksPreCount, tasksPostCount);
            #endregion
        }

        [Theory]
        [InlineData(1, null, new ReductionStatusEnum[] { })]                                // No queued tasks exist
        [InlineData(1, null, new ReductionStatusEnum[] { ReductionStatusEnum.Reducing  })]  // "
        [InlineData(1, null, new ReductionStatusEnum[] { ReductionStatusEnum.Reduced   })]  // "
        [InlineData(1, null, new ReductionStatusEnum[] { ReductionStatusEnum.Live      })]  // "
        [InlineData(1, null, new ReductionStatusEnum[] { ReductionStatusEnum.Canceled  })]  // "
        [InlineData(1, null, new ReductionStatusEnum[] { ReductionStatusEnum.Rejected })]  // "
        [InlineData(1, null, new ReductionStatusEnum[] { ReductionStatusEnum.Replaced  })]  // "
        [InlineData(1,    1, new ReductionStatusEnum[] { ReductionStatusEnum.Queued    })]  // The queued task is part of a publication request
        public async Task CancelReduction_ErrorInvalid(int SelectionGroupId, int? ContentPublicationRequestId, ReductionStatusEnum[] Tasks)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            foreach (var Status in Tasks)
            {
                TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
                {
                    ReductionStatus = Status,
                    ContentPublicationRequestId = ContentPublicationRequestId.HasValue ? MakeGuid(ContentPublicationRequestId.Value) : (Guid?)null,
                    SelectionGroupId = MakeGuid(SelectionGroupId),
                    ApplicationUserId = MakeGuid(1)
                });
            }
            #endregion

            #region Act
            int tasksPreCount = TestResources.DbContextObject.ContentReductionTask.Count();

            var view = await controller.CancelReduction(MakeGuid(SelectionGroupId));

            int tasksPostCount = TestResources.DbContextObject.ContentReductionTask.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            Assert.Equal(422, (view as StatusCodeResult).StatusCode);
            Assert.Equal(tasksPreCount, tasksPostCount);
            #endregion
        }

        [Theory]
        [InlineData("user2", 1)]  // User is not content access admin
        [InlineData("user1", 3)]  // User has no role in the root content item
        public async Task CancelReduction_ErrorUnauthorized(String UserName, int SelectionGroupId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
            {
                ReductionStatus = ReductionStatusEnum.Queued,
                ContentPublicationRequestId = null,
                SelectionGroupId = MakeGuid(SelectionGroupId),
                ApplicationUserId = MakeGuid(1)
            });
            #endregion

            #region Act
            int tasksPreCount = TestResources.DbContextObject.ContentReductionTask.Count();

            var view = await controller.CancelReduction(MakeGuid(SelectionGroupId));

            int tasksPostCount = TestResources.DbContextObject.ContentReductionTask.Count();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            Assert.Equal(tasksPreCount, tasksPostCount);
            #endregion
        }

        [Fact]
        public async Task CancelReduction_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
            {
                ReductionStatus = ReductionStatusEnum.Queued,
                ContentPublicationRequestId = null,
                SelectionGroupId = MakeGuid(1),
                ApplicationUserId = MakeGuid(1)
            });
            #endregion

            #region Act
            var view = await controller.CancelReduction(MakeGuid(1));
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Theory]
        [InlineData(1, 1, new ReductionStatusEnum[] { ReductionStatusEnum.Queued })]                                // A queued task created by the requesting user exists
        [InlineData(1, 2, new ReductionStatusEnum[] { ReductionStatusEnum.Queued })]                                // A queued task created by a different user exists
        [InlineData(1, 1, new ReductionStatusEnum[] { ReductionStatusEnum.Canceled, ReductionStatusEnum.Queued })]  // Multiple tasks exist, and at least one is queued
        public async Task CancelReduction_Success(int SelectionGroupId, int UserId, ReductionStatusEnum[] Tasks)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user1");
            foreach (var Status in Tasks)
            {
                TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
                {
                    ReductionStatus = Status,
                    ContentPublicationRequestId = null,
                    SelectionGroupId = MakeGuid(SelectionGroupId),
                    ApplicationUserId = MakeGuid(UserId),
                });
            }
            #endregion

            #region Act
            int tasksPreCount = TestResources.DbContextObject.ContentReductionTask.Count();
            int queuedPreCount = TestResources.DbContextObject.ContentReductionTask
                .Where(crt => crt.SelectionGroupId == MakeGuid(SelectionGroupId))
                .Where(crt => crt.ReductionStatus == ReductionStatusEnum.Queued)
                .Count();

            var view = await controller.CancelReduction(MakeGuid(SelectionGroupId));

            int tasksPostCount = TestResources.DbContextObject.ContentReductionTask.Count();
            int queuedPostCount = TestResources.DbContextObject.ContentReductionTask
                .Where(crt => crt.SelectionGroupId == MakeGuid(SelectionGroupId))
                .Where(crt => crt.ReductionStatus == ReductionStatusEnum.Queued)
                .Count();
            #endregion

            #region Assert
            Assert.Equal(tasksPreCount, tasksPostCount);
            Assert.Equal(1, queuedPreCount);
            Assert.Equal(0, queuedPostCount);
            #endregion
        }

        private Guid MakeGuid(int Val)
        {
            return new Guid(Val, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        }
    }
}
