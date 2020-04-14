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
    [Collection("DatabaseLifetime collection")]
    public class FileDropControllerTests
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;

        public FileDropControllerTests(DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _dbLifeTimeFixture = dbLifeTimeFixture;
        }

        /// <summary>Constructs a controller with the specified active user.</summary>
        /// <param name="Username"></param>
        /// <returns>ContentAccessAdminController</returns>
        private async Task<FileDropController> GetControllerForUser(TestInitialization TestResources, string Username)
        {
            var testController = new FileDropController(
                TestResources.AuditLogger,
                TestResources.AuthorizationService,
                TestResources.DbContext,
                TestResources.FileDropQueries,
                TestResources.FileSystemTasks,
                TestResources.UserManager,
                TestResources.Configuration);

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
        public async Task Clients_NotAuthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.FileDrop))
            {
                #region Arrange
                FileDropController controller = await GetControllerForUser(TestResources, "user8");
                #endregion

                #region Act
                var response = await controller.Clients();
                #endregion

                #region Assert
                var result = Assert.IsType<UnauthorizedResult>(response);
                #endregion
            }
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
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.FileDrop))
            {
                #region Arrange
                FileDropController controller = await GetControllerForUser(TestResources, userName);
                #endregion

                #region Act
                var response = await controller.Clients();
                #endregion

                #region Assert
                Assert.IsNotType<UnauthorizedResult>(response);
                #endregion
            }
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
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.FileDrop))
            {
                #region Arrange
                FileDropController controller = await GetControllerForUser(TestResources, userName);
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
        }

        [Theory]
        [InlineData("user8")] // no role
        [InlineData("user2")] // user only
        public async Task Create_Unauthorized(string userName)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.FileDrop))
            {
                #region Arrange
                FileDropController controller = await GetControllerForUser(TestResources, userName);
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
        }

        [Fact]
        public async Task Create_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.FileDrop))
            {
                #region Arrange
                FileDropController controller = await GetControllerForUser(TestResources, "user1");
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
                FileDropsModel returnModel = Assert.IsType<FileDropsModel>(jsonResult.Value);
                Assert.Equal(model.ClientId, returnModel.ClientCard.Id);

                FileDropCardModel insertedFileDrop = returnModel.FileDrops.Single(d => d.Value.Name == model.Name).Value;

                Assert.Equal(model.Description, insertedFileDrop.Description);
                Assert.Equal(model.ClientId, returnModel.ClientCard.Id);
                Assert.Equal(model.ClientId, insertedFileDrop.ClientId);
                Assert.NotNull(returnModel.CurrentFileDropId);
                Assert.Equal(insertedFileDrop.Id, returnModel.CurrentFileDropId);
                #endregion
            }
        }

        [Fact]
        public async Task Delete_Unauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.FileDrop))
            {
                #region Arrange
                FileDropController controller = await GetControllerForUser(TestResources, "user8");
                Guid fileDropIdToDelete = TestUtil.MakeTestGuid(3);
                bool ExistsAtStart = TestResources.DbContext.FileDrop.Any(d => d.Id == fileDropIdToDelete);
                #endregion

                #region Act
                var result = await controller.DeleteFileDrop(fileDropIdToDelete);
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(result);
                #endregion
            }
        }

        [Fact]
        public async Task Delete_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.FileDrop))
            {
                #region Arrange
                FileDropController controller = await GetControllerForUser(TestResources, "user1");
                Guid fileDropIdToDelete = TestUtil.MakeTestGuid(1);
                bool ExistsAtStart = TestResources.DbContext.FileDrop.Any(d => d.Id == fileDropIdToDelete);
                #endregion

                #region Act
                var result = await controller.DeleteFileDrop(fileDropIdToDelete);
                #endregion

                #region Assert
                JsonResult jsonResult = Assert.IsType<JsonResult>(result);
                FileDropsModel returnModel = Assert.IsType<FileDropsModel>(jsonResult.Value);
                Assert.Empty(returnModel.FileDrops.Where(d => d.Key == fileDropIdToDelete));
                bool ExistsAtEnd = TestResources.DbContext.FileDrop.Any(d => d.Id == fileDropIdToDelete);
                Assert.True(ExistsAtStart);
                Assert.False(ExistsAtEnd);
                Assert.Equal(fileDropIdToDelete, returnModel.CurrentFileDropId);
                #endregion
            }
        }

        [Theory]
        [InlineData("user8")] // no role
        [InlineData("user2")] // user only
        public async Task Update_Unauthorized(string userName)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.FileDrop))
            {
                #region Arrange
                FileDropController controller = await GetControllerForUser(TestResources, userName);
                FileDrop fileDrop = TestResources.DbContext.FileDrop.Single(d => d.Id == TestUtil.MakeTestGuid(1));
                FileDrop newFileDrop = new FileDrop
                {
                    Id = fileDrop.Id,
                    Name = "This name is modified",
                    Description = "This description is modified",
                    RootPath = fileDrop.RootPath,
                    IsSuspended = fileDrop.IsSuspended,
                    SftpAccounts = fileDrop.SftpAccounts,
                    ClientId = fileDrop.ClientId,
                    Client = fileDrop.Client,
                };
                #endregion

                #region Act
                var result = await controller.UpdateFileDrop(newFileDrop);
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(result);
                #endregion
            }
        }

        [Fact]
        public async Task Update_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.FileDrop))
            {
                #region Arrange
                FileDropController controller = await GetControllerForUser(TestResources, "user1");
                FileDrop fileDrop = TestResources.DbContext.FileDrop.Single(d => d.Id == TestUtil.MakeTestGuid(1));
                FileDrop newFileDrop = new FileDrop
                {
                    Id = fileDrop.Id,
                    Name = "This name is modified",
                    Description = "This description is modified",
                    RootPath = fileDrop.RootPath,
                    IsSuspended = fileDrop.IsSuspended,
                    SftpAccounts = fileDrop.SftpAccounts,
                    ClientId = fileDrop.ClientId,
                    Client = fileDrop.Client,
                };
                #endregion

                #region Act
                var result = await controller.UpdateFileDrop(newFileDrop);
                #endregion

                #region Assert
                JsonResult jsonResult = Assert.IsType<JsonResult>(result);
                FileDropsModel returnModel = Assert.IsType<FileDropsModel>(jsonResult.Value);
                Assert.Equal("This name is modified", returnModel.FileDrops[fileDrop.Id].Name);
                Assert.Equal("This description is modified", returnModel.FileDrops[fileDrop.Id].Description);
                Assert.Equal(TestUtil.MakeTestGuid(1), returnModel.ClientCard.Id);
                Assert.Equal(1, returnModel.ClientCard.FileDropCount);
                Assert.NotNull(returnModel.CurrentFileDropId);
                Assert.Equal(newFileDrop.Id, returnModel.CurrentFileDropId);
                #endregion
            }
        }

        [Fact]
        public async Task PermissionGroups_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.FileDrop))
            {
                #region Arrange
                FileDropController controller = await GetControllerForUser(TestResources, "user1");
                FileDrop fileDrop = TestResources.DbContext.FileDrop.Single(d => d.Id == TestUtil.MakeTestGuid(1));
                #endregion

                #region Act
                var result = await controller.PermissionGroups(fileDrop.Id, fileDrop.ClientId);
                #endregion

                #region Assert
                JsonResult jsonResult = Assert.IsType<JsonResult>(result);
                PermissionGroupsModel returnModel = Assert.IsType<PermissionGroupsModel>(jsonResult.Value);
                Assert.Equal(fileDrop.Id, returnModel.FileDropId);
                Assert.Equal(3, returnModel.EligibleUsers.Count);
                Assert.Contains(TestUtil.MakeTestGuid(2), returnModel.EligibleUsers.Keys);
                Assert.Contains(TestUtil.MakeTestGuid(6), returnModel.EligibleUsers.Keys);
                Assert.Contains(TestUtil.MakeTestGuid(7), returnModel.EligibleUsers.Keys);
                Assert.Equal(3, returnModel.PermissionGroups.Count);
                Assert.Contains(TestUtil.MakeTestGuid(1), returnModel.PermissionGroups.Keys);
                Assert.Contains(TestUtil.MakeTestGuid(3), returnModel.PermissionGroups.Keys);
                Assert.Contains(TestUtil.MakeTestGuid(5), returnModel.PermissionGroups.Keys);
                #endregion
            }
        }

        [Fact]
        public async Task PermissionGroups_Unauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.FileDrop))
            {
                #region Arrange
                FileDropController controller = await GetControllerForUser(TestResources, "user8");
                FileDrop fileDrop = TestResources.DbContext.FileDrop.Single(d => d.Id == TestUtil.MakeTestGuid(1));
                #endregion

                #region Act
                var result = await controller.PermissionGroups(fileDrop.Id, fileDrop.ClientId);
                #endregion

                #region Assert
                UnauthorizedResult jsonResult = Assert.IsType<UnauthorizedResult>(result);
                #endregion
            }
        }
    }
}
