/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Unit tests for the content access admin controller
 * DEVELOPER NOTES: 
 */

using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                TestResources.AuthorizationService,
                TestResources.DbContextObject,
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
            Assert.IsType<BadRequestResult>(view);
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
        [InlineData(999, 3)]
        [InlineData(8, 999)]
        public async Task ReportGroups_ErrorInvalid(long ClientId, long RootContentItemId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            var view = await controller.ReportGroups(ClientId, RootContentItemId);
            #endregion

            #region Assert
            Assert.IsType<BadRequestObjectResult>(view);
            #endregion
        }

        [Theory]
        [InlineData("user5", 1, 1)]
        [InlineData("test1", 8, 3)]
        public async Task ReportGroups_ErrorUnauthorized(String UserName, long ClientId, long RootContentItemId)
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser(UserName);
            #endregion

            #region Act
            var view = await controller.ReportGroups(ClientId, RootContentItemId);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        [Fact]
        public async Task ReportGroups_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            var view = await controller.ReportGroups(8, 3);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Fact]
        public async Task CreateReportGroup_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = await controller.CreateReportGroup(0, 0, "");
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Fact]
        public async Task UpdateReportGroup_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = await controller.UpdateReportGroup(0, null);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Fact]
        public async Task DeleteReportGroup_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = await controller.DeleteReportGroup(0);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Fact]
        public async Task Selections_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = await controller.Selections(0);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        [Fact]
        public async Task UpdateSelections_ReturnsJson()
        {
            #region Arrange
            ContentAccessAdminController controller = await GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = await controller.UpdateSelections(0, null);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

    }
}
