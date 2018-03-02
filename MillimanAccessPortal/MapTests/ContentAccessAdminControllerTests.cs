/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Unit tests for the content access admin controller
 * DEVELOPER NOTES: 
 */

using MapDbContextLib.Context;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
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
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });
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
                TestResources.UserManagerObject
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
            ContentAccessAdminController controller = await GetControllerForUser("test1");
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
            ContentAccessAdminController controller = await GetControllerForUser("user5");
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
            ContentAccessAdminController controller = await GetControllerForUser("test1");
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
            ContentAccessAdminController controller = await GetControllerForUser("user5");
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
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            var view = await controller.RootContentItems(999);
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            StatusCodeResult viewResult = (StatusCodeResult)view;
            Assert.Equal("422", viewResult.StatusCode.ToString());
            #endregion
        }

        [Fact]
        public async Task RootContentItems_ErrorUnauthorized()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            var view = await controller.RootContentItems(1);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        [Fact]
        public async Task RootContentItems_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            var view = await controller.RootContentItems(8);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Theory]
        [InlineData(999)]
        public async Task SelectionGroups_ErrorInvalid(long RootContentItemId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            var view = await controller.SelectionGroups(RootContentItemId);
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            StatusCodeResult viewResult = (StatusCodeResult)view;
            Assert.Equal("422", viewResult.StatusCode.ToString());
            #endregion
        }

        [Theory]
        [InlineData("user5", 1)]
        [InlineData("user6", 3)]
        public async Task SelectionGroups_ErrorUnauthorized(String UserName, long RootContentItemId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            #endregion

            #region Act
            var view = await controller.SelectionGroups(RootContentItemId);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        [Fact]
        public async Task SelectionGroups_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            var view = await controller.SelectionGroups(3);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Theory]
        [InlineData(999)]
        public async Task CreateSelectionGroup_ErrorInvalid(long RootContentItemId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.SelectionGroup.Count();
            var view = await controller.CreateSelectionGroup(RootContentItemId, "GroupName");
            int postCount = TestResources.DbContextObject.SelectionGroup.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            StatusCodeResult viewResult = (StatusCodeResult)view;
            Assert.Equal("422", viewResult.StatusCode.ToString());
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData("user5", 1)]
        [InlineData("user6", 3)]
        public async Task CreateSelectionGroup_ErrorUnauthorized(String UserName, long RootContentItemId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.SelectionGroup.Count();
            var view = await controller.CreateSelectionGroup(RootContentItemId, "GroupName");
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
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            var view = await controller.CreateSelectionGroup(3, "GroupName");
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Fact]
        public async Task CreateSelectionGroup_Success()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.SelectionGroup.Count();
            var view = await controller.CreateSelectionGroup(3, "GroupName");
            int postCount = TestResources.DbContextObject.SelectionGroup.Count();
            #endregion

            #region Assert
            Assert.Equal(preCount + 1, postCount);
            #endregion
        }

        [Theory]
        [InlineData(999, 3, true)]
        [InlineData(4, 999, true)]
        [InlineData(4, 4, true)]  // user ID does not have appropriate role in root content item
        [InlineData(5, 3, true)]  // user ID already belongs to another selection group for this root content item
        public async Task UpdateSelectionGroup_ErrorInvalid(long SelectionGroupId, long UserId, bool MembershipStatus)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            Dictionary<long, bool> MembershipSet = new Dictionary<long, bool>
            {
                { UserId, MembershipStatus },
                { 5, true }
            };
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            var view = await controller.UpdateSelectionGroupUserAssignments(SelectionGroupId, MembershipSet);
            int postCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            StatusCodeResult viewResult = (StatusCodeResult)view;
            Assert.Equal("422", viewResult.StatusCode.ToString());
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData("user5", 3)]
        [InlineData("user6", 4)]
        public async Task UpdateSelectionGroup_ErrorUnauthorized(String UserName, long SelectionGroupId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            Dictionary<long, bool> MembershipSet = new Dictionary<long, bool>
            {
                { 3, true },
                { 5, true }
            };
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            var view = await controller.UpdateSelectionGroupUserAssignments(SelectionGroupId, MembershipSet);
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
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            Dictionary<long, bool> MembershipSet = new Dictionary<long, bool>
            {
                { 3, true },
                { 5, false }
            };
            #endregion

            #region Act
            var view = await controller.UpdateSelectionGroupUserAssignments(4, MembershipSet);
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
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            Dictionary<long, bool> MembershipSet = new Dictionary<long, bool>
            {
                { 3, MembershipStatus },
                { 5, MembershipStatus },
            };
            #endregion

            #region Act
            int preCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            var view = await controller.UpdateSelectionGroupUserAssignments(4, MembershipSet);
            int postCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            #endregion

            #region Assert
            Assert.Equal(preCount + (MembershipStatus ? 1 : -1), postCount);
            #endregion
        }

        [Theory]
        [InlineData(999)]
        public async Task DeleteSelectionGroup_ErrorInvalid(long RootContentItemId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            int groupsPreCount = TestResources.DbContextObject.SelectionGroup.Count();
            int userPreCount = TestResources.DbContextObject.UserInSelectionGroup.Count();

            var view = await controller.DeleteSelectionGroup(RootContentItemId);

            int groupsPostCount = TestResources.DbContextObject.SelectionGroup.Count();
            int userPostCount = TestResources.DbContextObject.UserInSelectionGroup.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            StatusCodeResult viewResult = (StatusCodeResult)view;
            Assert.Equal("422", viewResult.StatusCode.ToString());
            Assert.Equal(groupsPreCount, groupsPostCount);
            Assert.Equal(userPreCount, userPostCount);
            #endregion
        }

        [Theory]
        [InlineData("user5", 1)]
        [InlineData("user6", 3)]
        public async Task DeleteSelectionGroup_ErrorUnauthorized(String UserName, long RootContentItemId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            #endregion

            #region Act
            int groupsPreCount = TestResources.DbContextObject.SelectionGroup.Count();
            int userPreCount = TestResources.DbContextObject.UserInSelectionGroup.Count();

            var view = await controller.DeleteSelectionGroup(RootContentItemId);

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
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            var view = await controller.DeleteSelectionGroup(4);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Fact]
        public async Task DeleteSelectionGroup_Success()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            int groupsPreCount = TestResources.DbContextObject.SelectionGroup.Count();
            int userPreCount = TestResources.DbContextObject.UserInSelectionGroup.Count();

            var view = await controller.DeleteSelectionGroup(4);

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
        public async Task Selections_ErrorInvalid(long SelectionGroupId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            var view = await controller.SelectionGroups(SelectionGroupId);
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            StatusCodeResult viewResult = (StatusCodeResult)view;
            Assert.Equal("422", viewResult.StatusCode.ToString());
            #endregion
        }

        [Theory]
        [InlineData("user5", 1)]
        [InlineData("user6", 4)]
        public async Task Selections_ErrorUnauthorized(String UserName, long SelectionGroupId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            #endregion

            #region Act
            var view = await controller.Selections(SelectionGroupId);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        [Fact]
        public async Task Selections_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            var view = await controller.Selections(4);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Theory]
        [InlineData(999,   2, new ReductionStatusEnum[] { })]  // Selection group does not exist
        [InlineData(  4, 999, new ReductionStatusEnum[] { })]  // Hierarchy field value does not exist
        [InlineData(  4,   1, new ReductionStatusEnum[] { })]  // Hierarchy field value does not belong to the correct root content item
        [InlineData(  6,   4, new ReductionStatusEnum[] { })]  // Content has not been published for the root content item
        [InlineData(  4,   2, new ReductionStatusEnum[] { ReductionStatusEnum.Queued    })]  // An outstanding reduction task exists for the root content item
        [InlineData(  4,   2, new ReductionStatusEnum[] { ReductionStatusEnum.Reducing  })]  // "
        [InlineData(  4,   2, new ReductionStatusEnum[] { ReductionStatusEnum.Reduced   })]  // "
        public async Task SingleReduction_ErrorInvalid(long SelectionGroupId, long HierarchyFieldValueId, ReductionStatusEnum[] Tasks)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            var Selections = new Dictionary<long, Boolean>
            {
                { HierarchyFieldValueId, true }
            };
            foreach (var Status in Tasks)
            {
                TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
                {
                    ReductionStatus = Status,
                    ContentPublicationRequestId = null,
                    SelectionGroupId = SelectionGroupId,
                    ApplicationUserId = 1
                });
            }
            #endregion

            #region Act
            int tasksPreCount = TestResources.DbContextObject.ContentReductionTask.Count();
            
            var view = await controller.SingleReduction(SelectionGroupId, Selections);

            int tasksPostCount = TestResources.DbContextObject.ContentReductionTask.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            StatusCodeResult viewResult = (StatusCodeResult)view;
            Assert.Equal("422", viewResult.StatusCode.ToString());
            Assert.Equal(tasksPreCount, tasksPostCount);
            #endregion
        }

        [Theory]
        [InlineData("user5", 1)]  // User has no role in the root content item
        [InlineData("user6", 4)]  // User has the incorrect role in the root content item
        public async Task SingleReduction_ErrorUnauthorized(String UserName, long SelectionGroupId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            var Selections = new Dictionary<long, Boolean>
            {
                { 2, true }
            };
            #endregion

            #region Act
            int tasksPreCount = TestResources.DbContextObject.ContentReductionTask.Count();

            var view = await controller.SingleReduction(SelectionGroupId, Selections);

            int tasksPostCount = TestResources.DbContextObject.ContentReductionTask.Count();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            Assert.Equal(tasksPreCount, tasksPostCount);
            #endregion
        }

        [Fact]
        public async Task SingleReduction_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            var Selections = new Dictionary<long, Boolean>
            {
                { 2, true }
            };
            #endregion

            #region Act
            var view = controller.SingleReduction(4, Selections);
            #endregion

            #region Assert
            #endregion
        }

        [Theory]
        [InlineData(4, new ReductionStatusEnum[] { })]                                // No outstanding tasks exist
        [InlineData(4, new ReductionStatusEnum[] { ReductionStatusEnum.Pushed    })]  // "
        [InlineData(4, new ReductionStatusEnum[] { ReductionStatusEnum.Canceled  })]  // "
        [InlineData(4, new ReductionStatusEnum[] { ReductionStatusEnum.Discarded })]  // "
        [InlineData(4, new ReductionStatusEnum[] { ReductionStatusEnum.Replaced  })]  // "
        public async Task SingleReduction_Success(long SelectionGroupId, ReductionStatusEnum[] Tasks)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            var Selections = new Dictionary<long, Boolean>
            {
                { 2, true }
            };
            foreach (var Status in Tasks)
            {
                TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
                {
                    ReductionStatus = Status,
                    ContentPublicationRequestId = null,
                    SelectionGroupId = SelectionGroupId,
                    ApplicationUserId = 1
                });
            }
            #endregion

            #region Act
            int tasksPreCount = TestResources.DbContextObject.ContentReductionTask.Count();

            var view = await controller.SingleReduction(SelectionGroupId, Selections);

            int tasksPostCount = TestResources.DbContextObject.ContentReductionTask.Count();
            #endregion

            #region Assert
            Assert.Equal(tasksPreCount + 1, tasksPostCount);
            #endregion
        }

        [Theory]
        [InlineData(4, new ReductionStatusEnum[] { })]
        [InlineData(4, new ReductionStatusEnum[] { ReductionStatusEnum.Reducing })]
        [InlineData(4, new ReductionStatusEnum[] { ReductionStatusEnum.Reduced })]
        [InlineData(4, new ReductionStatusEnum[] { ReductionStatusEnum.Pushed })]
        [InlineData(4, new ReductionStatusEnum[] { ReductionStatusEnum.Canceled })]
        [InlineData(4, new ReductionStatusEnum[] { ReductionStatusEnum.Discarded })]
        [InlineData(4, new ReductionStatusEnum[] { ReductionStatusEnum.Replaced })]
        public async Task CancelReduction_ErrorInvalid(long SelectionGroupId, ReductionStatusEnum[] Tasks)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            foreach (var Status in Tasks)
            {
                TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
                {
                    ReductionStatus = Status,
                    ContentPublicationRequestId = null,
                    SelectionGroupId = SelectionGroupId,
                    ApplicationUserId = 5
                });
            }
            #endregion

            #region Act
            int tasksPreCount = TestResources.DbContextObject.ContentReductionTask.Count();

            var view = await controller.CancelReduction(SelectionGroupId);

            int tasksPostCount = TestResources.DbContextObject.ContentReductionTask.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            StatusCodeResult viewResult = (StatusCodeResult)view;
            Assert.Equal("422", viewResult.StatusCode.ToString());
            Assert.Equal(tasksPreCount, tasksPostCount);
            #endregion
        }

        [Theory]
        [InlineData("user5", 5, 1)]
        [InlineData("user6", 6, 4)]
        public async Task CancelReduction_ErrorUnauthorized(String UserName, long ApplicationUserId, long SelectionGroupId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
            {
                ReductionStatus = ReductionStatusEnum.Queued,
                ContentPublicationRequestId = null,
                SelectionGroupId = SelectionGroupId,
                ApplicationUserId = ApplicationUserId
            });
            #endregion

            #region Act
            int tasksPreCount = TestResources.DbContextObject.ContentReductionTask.Count();

            var view = await controller.CancelReduction(SelectionGroupId);

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
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
            {
                ReductionStatus = ReductionStatusEnum.Queued,
                ContentPublicationRequestId = null,
                SelectionGroupId = 4,
                ApplicationUserId = 5
            });
            #endregion

            #region Act
            var view = await controller.CancelReduction(4);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Theory]
        [InlineData(4, 5, new ReductionStatusEnum[] { ReductionStatusEnum.Queued })]
        [InlineData(4, 6, new ReductionStatusEnum[] { ReductionStatusEnum.Queued })]
        [InlineData(4, 5, new ReductionStatusEnum[] { ReductionStatusEnum.Canceled, ReductionStatusEnum.Queued })]
        public async Task CancelReduction_Success(long SelectionGroupId, long UserId, ReductionStatusEnum[] Tasks)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            foreach (var Status in Tasks)
            {
                TestResources.DbContextObject.ContentReductionTask.Add(new ContentReductionTask
                {
                    ReductionStatus = Status,
                    ContentPublicationRequestId = null,
                    SelectionGroupId = SelectionGroupId,
                    ApplicationUserId = UserId,
                });
            }
            #endregion

            #region Act
            int tasksPreCount = TestResources.DbContextObject.ContentReductionTask.Count();
            int queuedPreCount = TestResources.DbContextObject.ContentReductionTask
                .Where(crt => crt.SelectionGroupId == SelectionGroupId)
                .Where(crt => crt.ReductionStatus == ReductionStatusEnum.Queued)
                .Count();

            var view = await controller.CancelReduction(SelectionGroupId);

            int tasksPostCount = TestResources.DbContextObject.ContentReductionTask.Count();
            int queuedPostCount = TestResources.DbContextObject.ContentReductionTask
                .Where(crt => crt.SelectionGroupId == SelectionGroupId)
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
}
