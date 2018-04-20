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
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });
        }

        /// <summary>Constructs a controller with the specified active user.</summary>
        /// <param name="Username"></param>
        /// <returns>ContentAccessAdminController</returns>
        public async Task<ContentPublishingController> GetControllerForUser(string Username)
        {
            var testController = new ContentPublishingController(
                TestResources.AuditLoggerObject,
                TestResources.AuthorizationService,
                TestResources.DbContextObject,
                TestResources.LoggerFactory,
                TestResources.QueriesObj
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
            ContentPublishingController controller = await GetControllerForUser("test1");
            #endregion

            #region Act
            var view = controller.Index();
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            #endregion
        }

        [Fact]
        public async Task RequestContentPublication_Ok()
        {
            #region Arrange
            ContentPublishingController controller = await GetControllerForUser("test1");
            #endregion

            #region Act
            var view = await controller.Publish();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }
    }
}
