/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Unit tests for the file drop controller
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using System;
using System.Threading.Tasks;
using TestResourcesLib;
using Xunit;

namespace MapTests
{
    public class FileDropControllerTests
    {
        internal TestInitialization TestResources { get; set; }

        /// <summary>Initializes test resources.</summary>
        /// <remarks>This constructor is called before each test.</remarks>
        public FileDropControllerTests()
        {
            TestResources = new TestInitialization();
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });
        }

        /// <summary>Constructs a controller with the specified active user.</summary>
        /// <param name="Username"></param>
        /// <returns>ContentAccessAdminController</returns>
        public async Task<FileDropController> GetControllerForUser(string Username)
        {
            var testController = new FileDropController(
                TestResources.AuditLoggerObject,
                TestResources.AuthorizationService,
                TestResources.MockDbContext.Object,
                TestResources.FileDropQueriesObject,
                TestResources.FileSystemTasksObject,
                TestResources.UserManagerObject,
                TestResources.ConfigurationObject);

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
        public async Task Clients_NotAuthorized()
        {
            #region Arrange
            FileDropController controller = await GetControllerForUser("user5");
            #endregion

            #region Act
            var response = await controller.Clients();
            #endregion

            #region Assert
            var result = Assert.IsType<UnauthorizedResult>(response);
            #endregion
        }

        [Theory]
        [InlineData("test1")]
        [InlineData("test2")]
        [InlineData("ClientAdmin1")]
        public async Task Clients_Authorized(string userName)
        {
            #region Arrange
            FileDropController controller = await GetControllerForUser(userName);
            #endregion

            #region Act
            var response = await controller.Clients();
            #endregion

            #region Assert
            var result = Assert.IsType<JsonResult>(response);
            #endregion
        }

    }
}
