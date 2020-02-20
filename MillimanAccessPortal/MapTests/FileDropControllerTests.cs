/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Unit tests for the file drop controller
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.FileDropModels;
using Newtonsoft.Json;
using System;
using System.Linq;
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
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.FileDrop });
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
            FileDropController controller = await GetControllerForUser("user8");
            #endregion

            #region Act
            var response = await controller.Clients();
            #endregion

            #region Assert
            var result = Assert.IsType<UnauthorizedResult>(response);
            #endregion
        }

        [Theory]
        [InlineData("user1")]
        [InlineData("user2")]
        [InlineData("user3")]
        [InlineData("user4")]
        [InlineData("user5")]
        [InlineData("user6")]
        [InlineData("user7")]
        public async Task Clients_Authorized(string userName)
        {
            #region Arrange
            FileDropController controller = await GetControllerForUser(userName);
            #endregion

            #region Act
            var response = await controller.Clients();
            #endregion

            #region Assert
            Assert.IsNotType<UnauthorizedResult>(response);
            #endregion
        }

        /// <summary>
        /// Client1 is parent, Client2 is child
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="numClient1"></param>
        /// <param name="numClient2"></param>
        /// <param name="numClient3"></param>
        /// <returns></returns>
        [Theory]
        [InlineData("user1", 1, 0, 0)] // 1-admin, 2-no role
        [InlineData("user2", 1, 0, 0)] // 1-user, 2-no role
        [InlineData("user3", 1, 1, 0)] // 1-no role, 2-admin
        [InlineData("user4", 0, 1, 0)] // 1-no role, 2-user
        [InlineData("user5", 1, 1, 0)] // 1-admin, 2-admin
        [InlineData("user6", 1, 1, 0)] // 1-user, 2-user
        [InlineData("user7", 1, 1, 0)] // 1-both, 2-both
        public async Task Clients_CorrectResponse(string userName, int numClient1, int numClient2, int numClient3)
        {
            #region Arrange
            FileDropController controller = await GetControllerForUser(userName);
            #endregion

            #region Act
            var response = await controller.Clients();
            #endregion

            #region Assert
            JsonResult result = Assert.IsType<JsonResult>(response);
            ClientsModel model = Assert.IsType<ClientsModel>(result.Value);
            Assert.Equal(numClient1, model.Clients.Values.Where(c => c.Id == TestUtil.MakeTestGuid(1)).Count());
            Assert.Equal(numClient2, model.Clients.Values.Where(c => c.Id == TestUtil.MakeTestGuid(2)).Count());
            Assert.Equal(numClient3, model.Clients.Values.Where(c => c.Id == TestUtil.MakeTestGuid(3)).Count());
            #endregion
        }

        [Fact]
        public async Task Create_Unauthorized()
        {
            #region Arrange
            FileDropController controller = await GetControllerForUser("user8");
            FileDrop model = new FileDrop
            {
                ClientId = TestUtil.MakeTestGuid(1),
                Name = "Test FileDrop",
                Description = null,
            };
            #endregion

            #region Act
            var result = await controller.CreateFileDrop(model);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(result);
            #endregion
        }

        [Fact]
        public async Task Create_Success()
        {
            #region Arrange
            FileDropController controller = await GetControllerForUser("user1");
            FileDrop model = new FileDrop
            {
                ClientId = TestUtil.MakeTestGuid(1),
                Name = "Test FileDrop",
                Description = null,
            };
            #endregion

            #region Act
            var result = await controller.CreateFileDrop(model);
            #endregion

            #region Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            FileDrop returnModel = Assert.IsType<FileDrop>(jsonResult.Value);
            Assert.Equal(model.ClientId, returnModel.ClientId);
            Assert.Equal(model.Description, returnModel.Description);
            Assert.Equal(model.Name, returnModel.Name);

            Assert.NotEqual(Guid.Empty, returnModel.ClientId);
            Assert.False(string.IsNullOrWhiteSpace(returnModel.RootPath));
            Assert.Null(returnModel.SftpAccounts);
            #endregion
        }

        [Fact]
        public async Task Delete_Unauthorized()
        {
            #region Arrange
            FileDropController controller = await GetControllerForUser("user8");
            Guid fileDropIdToDelete = TestUtil.MakeTestGuid(3);
            bool ExistsAtStart = TestResources.DbContextObject.FileDrop.Any(d => d.Id == fileDropIdToDelete);
            #endregion

            #region Act
            var result = await controller.DeleteFileDrop(fileDropIdToDelete);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(result);
            #endregion
        }

        [Fact]
        public async Task Delete_Success()
        {
            #region Arrange
            FileDropController controller = await GetControllerForUser("user1");
            Guid fileDropIdToDelete = TestUtil.MakeTestGuid(1);
            bool ExistsAtStart = TestResources.DbContextObject.FileDrop.Any(d => d.Id == fileDropIdToDelete);
            #endregion

            #region Act
            var result = await controller.DeleteFileDrop(fileDropIdToDelete);
            #endregion

            #region Assert
            JsonResult jsonResult = Assert.IsType<JsonResult>(result);
            FileDropsModel returnModel = Assert.IsType<FileDropsModel>(jsonResult.Value);
            Assert.Empty(returnModel.FileDrops.Where(d => d.Key == fileDropIdToDelete));
            bool ExistsAtEnd = TestResources.DbContextObject.FileDrop.Any(d => d.Id == fileDropIdToDelete);
            Assert.True(ExistsAtStart);
            Assert.False(ExistsAtEnd);
            #endregion
        }
    }
}
