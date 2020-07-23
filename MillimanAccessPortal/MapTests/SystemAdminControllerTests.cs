/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Unit tests for the system admin controller
 * DEVELOPER NOTES: 
 */

using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.SharedModels;
using MillimanAccessPortal.Models.SystemAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestResourcesLib;
using Xunit;

namespace MapTests
{
    [Collection("DatabaseLifetime collection")]
    [LogTestBeginEnd]
    public class SystemAdminControllerTests
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;

        internal TestInitialization TestResources { get; set; }

        public SystemAdminControllerTests(DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _dbLifeTimeFixture = dbLifeTimeFixture;
        }

        /// <summary>Constructs a controller with the specified active user.</summary>
        /// <param name="Username"></param>
        /// <returns>SystemAdminController</returns>
        public async Task<SystemAdminController> GetControllerForUser(string Username)
        {
            var accountController = new AccountController(
                TestResources.DbContext,
                TestResources.UserManager,
                TestResources.RoleManager,
                null,
                TestResources.MessageQueueServicesObject,
                TestResources.AuditLogger,
                TestResources.AuthorizationService,
                TestResources.Configuration,
                TestResources.ScopedServiceProvider,
                null);

            var testController = new SystemAdminController(
                accountController,
                TestResources.AuditLogger,
                TestResources.AuthorizationService,
                TestResources.Configuration,
                TestResources.DbContext,
                TestResources.StandardQueries,
                TestResources.RoleManager,
                TestResources.UserManager,
                TestResources.ScopedServiceProvider,
                TestResources.AuthenticationSchemeProvider);

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
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysUser1");
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
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var view = await controller.Index();
                #endregion

                #region Assert
                Assert.IsType<ViewResult>(view);
                #endregion
            }
        }

        #region query action tests
        #region user queries
        [Fact]
        public async Task Users_Success_FilterNone()
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter { };
                var expectedUsers = 4;  // total number of users in test data
                #endregion

                #region Act
                var json = await controller.Users(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                List<UserInfo> users = Assert.IsType<List<UserInfo>>(result.Value);
                Assert.Equal(expectedUsers, users.Count);
                #endregion
            }
        }
        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 0)]
        public async Task Users_Success_FilterClient(int clientId, int expectedUsers)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                };
                #endregion

                #region Act
                var json = await controller.Users(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                List<UserInfo> userInfoList = Assert.IsType<List<UserInfo>>(result.Value);
                Assert.Equal(expectedUsers, userInfoList.Count);
                #endregion
            }
        }
        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 0)]
        public async Task Users_Success_FilterProfitCenter(int profitCenterId, int expectedUsers)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    ProfitCenterId = TestUtil.MakeTestGuid(profitCenterId),
                };
                #endregion

                #region Act
                var json = await controller.Users(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                List<UserInfo> userInfoList = Assert.IsType<List<UserInfo>>(result.Value);
                Assert.Equal(expectedUsers, userInfoList.Count);
                #endregion
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(-1)]
        public async Task UserDetail_Invalid(int? userId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    UserId = userId.HasValue ? TestUtil.MakeTestGuid(userId.Value) : (Guid?)null,
                };
                #endregion

                #region Act
                var json = await controller.UserDetail(queryFilter);
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                #endregion
            }
        }
        [Theory]
        [InlineData(1)]
        public async Task UserDetail_Success_FilterNone(int userId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    UserId = TestUtil.MakeTestGuid(userId),
                };
                #endregion

                #region Act
                var json = await controller.UserDetail(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                Assert.IsType<UserDetail>(result.Value);
                #endregion
            }
        }
        [Theory]
        [InlineData(1, 1)]
        public async Task UserDetail_Success_FilterClient(int userId, int clientId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    UserId = TestUtil.MakeTestGuid(userId),
                    ClientId = TestUtil.MakeTestGuid(clientId),
                };
                #endregion

                #region Act
                var json = await controller.UserDetail(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                Assert.IsType<UserDetailForClient>(result.Value);
                #endregion
            }
        }
        [Theory]
        [InlineData(1, 1)]
        public async Task UserDetail_Success_FilterProfitCenter(int userId, int profitCenterId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    UserId = TestUtil.MakeTestGuid(userId),
                    ProfitCenterId = TestUtil.MakeTestGuid(profitCenterId),
                };
                #endregion

                #region Act
                var json = await controller.UserDetail(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                Assert.IsType<UserDetailForProfitCenter>(result.Value);
                #endregion
            }
        }
        #endregion

        #region client queries
        [Fact]
        public async Task Clients_Success_FilterNone()
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter { };
                var expectedClients = 2;  // total number of clients in test data
                #endregion

                #region Act
                var json = await controller.Clients(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                BasicTree<ClientInfo> clientInfoTree = Assert.IsType<BasicTree<ClientInfo>>(result.Value);
                Assert.Equal(expectedClients, clientInfoTree.Root.Children.Count);
                #endregion
            }
        }
        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 0)]
        public async Task Clients_Success_FilterUser(int userId, int expectedClients)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    UserId = TestUtil.MakeTestGuid(userId),
                };
                #endregion

                #region Act
                var json = await controller.Clients(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                BasicTree<ClientInfo> clientInfoTree = Assert.IsType<BasicTree<ClientInfo>>(result.Value);
                Assert.Equal(expectedClients, clientInfoTree.Root.Children.Count);
                #endregion
            }
        }
        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 0)]
        public async Task Clients_Success_FilterProfitCenter(int profitCenterId, int expectedClients)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    ProfitCenterId = TestUtil.MakeTestGuid(profitCenterId),
                };
                #endregion

                #region Act
                var json = await controller.Clients(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                BasicTree<ClientInfo> clientInfoTree = Assert.IsType<BasicTree<ClientInfo>>(result.Value);
                Assert.Equal(expectedClients, clientInfoTree.Root.Children.Count);
                #endregion
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(-1)]
        public async Task ClientDetail_Invalid(int? clientId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    ClientId = clientId.HasValue ? TestUtil.MakeTestGuid(clientId.Value) : (Guid?)null,
                };
                #endregion

                #region Act
                var json = await controller.ClientDetail(queryFilter);
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                #endregion
            }
        }
        [Theory]
        [InlineData(1)]
        public async Task ClientDetail_Success_FilterNone(int clientId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                };
                #endregion

                #region Act
                var json = await controller.ClientDetail(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                Assert.IsType<ClientDetail>(result.Value);
                #endregion
            }
        }
        [Theory]
        [InlineData(1, 1)]
        public async Task ClientDetail_Success_FilterUser(int clientId, int userId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                    UserId = TestUtil.MakeTestGuid(userId),
                };
                #endregion

                #region Act
                var json = await controller.ClientDetail(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                Assert.IsType<ClientDetailForUser>(result.Value);
                #endregion
            }
        }
        [Theory]
        [InlineData(1, 1)]
        public async Task ClientDetail_Success_FilterProfitCenter(int clientId, int profitCenterId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                    ProfitCenterId = TestUtil.MakeTestGuid(profitCenterId),
                };
                #endregion

                #region Act
                var json = await controller.ClientDetail(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                Assert.IsType<ClientDetailForProfitCenter>(result.Value);
                #endregion
            }
        }
        #endregion

        #region profit center queries
        [Fact]
        public async Task ProfitCenters_Success_FilterNone()
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter { };
                var expectedProfitCenters = 2;  // total number of profit centers in test data
                #endregion

                #region Act
                var json = await controller.ProfitCenters(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                List<ProfitCenterInfo> profitCenterInfoList = Assert.IsType<List<ProfitCenterInfo>>(result.Value);
                Assert.Equal(expectedProfitCenters, ((json as JsonResult).Value as List<ProfitCenterInfo>).Count);
                #endregion
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(-1)]
        public async Task ProfitCenterDetail_Invalid(int? profitCenterId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    ProfitCenterId = profitCenterId.HasValue ? TestUtil.MakeTestGuid(profitCenterId.Value) : (Guid?)null,
                };
                #endregion

                #region Act
                var json = await controller.ProfitCenterDetail(queryFilter);
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                #endregion
            }
        }
        [Theory]
        [InlineData(1)]
        public async Task ProfitCenterDetail_Success_FilterNone(int profitCenterId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    ProfitCenterId = TestUtil.MakeTestGuid(profitCenterId),
                };
                #endregion

                #region Act
                var json = await controller.ProfitCenterDetail(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                Assert.IsType<ProfitCenterDetail>(result.Value);
                #endregion
            }
        }
        #endregion

        #region root content item queries
        [Theory]
        [InlineData(1, 0)]
        [InlineData(11, 1)]
        public async Task RootContentItems_Success_FilterUser(int userId, int expectedRootContentItems)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    UserId = TestUtil.MakeTestGuid(userId),
                };
                #endregion

                #region Act
                var json = await controller.RootContentItems(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                List<RootContentItemInfo> rootContentItemInfoList = Assert.IsType<List<RootContentItemInfo>>(result.Value);
                Assert.Equal(expectedRootContentItems, rootContentItemInfoList.Count);
                #endregion
            }
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 0)]
        public async Task RootContentItems_Success_FilterClient(int clientId, int expectedRootContentItems)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                };
                #endregion

                #region Act
                var json = await controller.RootContentItems(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                List<RootContentItemInfo> rootContentItemInfoList = Assert.IsType<List<RootContentItemInfo>>(result.Value);
                Assert.Equal(expectedRootContentItems, rootContentItemInfoList.Count);
                #endregion
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(-1)]
        public async Task RootContentItemDetail_Invalid(int? rootContentItemId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    RootContentItemId = rootContentItemId.HasValue ? TestUtil.MakeTestGuid(rootContentItemId.Value) : (Guid?)null,
                };
                #endregion

                #region Act
                var json = await controller.RootContentItemDetail(queryFilter);
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                #endregion
            }
        }
        [Theory]
        [InlineData(1, 1)]
        public async Task RootContentItemDetail_Success_FilterUser(int rootContentItemId, int userId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    RootContentItemId = TestUtil.MakeTestGuid(rootContentItemId),
                    UserId = TestUtil.MakeTestGuid(userId),
                };
                #endregion

                #region Act
                var json = await controller.RootContentItemDetail(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                Assert.IsType<RootContentItemDetailForUser>(result.Value);
                #endregion
            }
        }
        [Theory]
        [InlineData(1, 1)]
        public async Task RootContentItemDetail_Success_FilterRootContentItem(int rootContentItemId, int clientId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var queryFilter = new QueryFilter
                {
                    RootContentItemId = TestUtil.MakeTestGuid(rootContentItemId),
                    ClientId = TestUtil.MakeTestGuid(clientId),
                };
                #endregion

                #region Act
                var json = await controller.RootContentItemDetail(queryFilter);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(json);
                Assert.IsType<RootContentItemDetailForClient>(result.Value);
                #endregion
            }
        }
        #endregion
        #endregion

        #region create action tests
        [Theory]
        [InlineData("invalid_email_address")]
        [InlineData("sysAdmin1@site.domain")]
        public async Task CreateUser_Invalid(string email)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.Users.Count();
                var json = await controller.CreateUser(email);
                var postCount = TestResources.DbContext.Users.Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        /* Disabled until mail sender is testable.
        [Theory]
        [InlineData("sysUser3@site.domain")]
        public async Task CreateUser_Success(string email)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContext.Users.Count();
            var json = await controller.CreateUser(email);
            var postCount = _testResources.DbContext.Users.Count();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json);
            Assert.Equal(preCount + 1, postCount);
            #endregion
        }
        */

        [Theory]
        [InlineData("", null, null, null, null, null)]
        public async Task CreateProfitCenter_Success(string name, string code, string office, string contact, string email, string phone)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var profitCenter = new ProfitCenter
                {
                    Name = name,
                    ProfitCenterCode = code,
                    MillimanOffice = office,
                    ContactName = contact,
                    ContactEmail = email,
                    ContactPhone = phone,
                };
                #endregion

                #region Act
                var preCount = TestResources.DbContext.ProfitCenter.Count();
                var json = await controller.CreateProfitCenter(profitCenter);
                var postCount = TestResources.DbContext.ProfitCenter.Count();
                #endregion

                #region Assert
                Assert.IsType<JsonResult>(json);
                Assert.Equal(preCount + 1, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("invalid_email_address", 1)]
        [InlineData("sysUser1@site.domain", 99)]
        public async Task AddUserToClient_Invalid(string email, int clientId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.UserClaims.Count();
                var json = await controller.AddUserToClient(email, TestUtil.MakeTestGuid(clientId));
                var postCount = TestResources.DbContext.UserClaims.Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("sysUser2@site.domain", 1)]
        // [InlineData("sysUser3@site.domain", 1)]  // Disabled until mail sender is testable.
        public async Task AddUserToClient_Success(string email, int clientId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.UserClaims.Count();
                var json = await controller.AddUserToClient(email, TestUtil.MakeTestGuid(clientId));
                var postCount = TestResources.DbContext.UserClaims.Count();
                #endregion

                #region Assert
                Assert.IsType<JsonResult>(json);
                Assert.Equal(preCount + 1, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("sysUser1@site.domain", 1)]
        public async Task AddUserToClient_NoOp(string email, int clientId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.UserClaims.Count();
                var json = await controller.AddUserToClient(email, TestUtil.MakeTestGuid(clientId));
                var postCount = TestResources.DbContext.UserClaims.Count();
                #endregion

                #region Assert
                Assert.IsType<JsonResult>(json);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("invalid_email_address", 1)]
        [InlineData("sysUser1@site.domain", 99)]
        public async Task AddUserToProfitCenter_Invalid(string email, int profitCenterId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.UserRoleInProfitCenter.Count();
                var json = await controller.AddUserToProfitCenter(email, TestUtil.MakeTestGuid(profitCenterId));
                var postCount = TestResources.DbContext.UserRoleInProfitCenter.Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("sysUser1@site.domain", 1)]
        // [InlineData("sysUser3@site.domain", 1)]  // Disabled until mail sender is testable.
        public async Task AddUserToProfitCenter_Success(string email, int profitCenterId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.UserRoleInProfitCenter.Count();
                var json = await controller.AddUserToProfitCenter(email, TestUtil.MakeTestGuid(profitCenterId));
                var postCount = TestResources.DbContext.UserRoleInProfitCenter.Count();
                #endregion

                #region Assert
                Assert.IsType<JsonResult>(json);
                Assert.Equal(preCount + 1, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData("sysAdmin1@site.domain", 1)]
        public async Task AddUserToProfitCenter_NoOp(string email, int profitCenterId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.UserRoleInProfitCenter.Count();
                var json = await controller.AddUserToProfitCenter(email, TestUtil.MakeTestGuid(profitCenterId));
                var postCount = TestResources.DbContext.UserRoleInProfitCenter.Count();
                #endregion

                #region Assert
                Assert.IsType<JsonResult>(json);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Fact]
        public async Task AddNewAuthenticationScheme_Success()
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var model = new AllAuthenticationSchemes.AuthenticationScheme
                {
                    Name = "testscheme1",
                    DisplayName = "Display Name 1",
                    Properties = new MapDbContextLib.Models.WsFederationSchemeProperties { Wtrealm = "realm1", MetadataAddress = "address1" },
                    Type = AuthenticationType.WsFederation,
                };
                #endregion

                #region Act
                var result = await controller.AddNewAuthenticationScheme(model);
                #endregion

                #region Assert
                Assert.IsType<OkResult>(result);
                var dbScheme = TestResources.DbContext.AuthenticationScheme.Where(c => c.Name == model.Name).ToList();
                Assert.Single(dbScheme);
                Assert.Equal(model.Name, dbScheme.First().Name);
                Assert.Equal(model.DisplayName, dbScheme.First().DisplayName);
                #endregion
            }
        }

        [Fact]
        public async Task AddNewAuthenticationScheme_FailsOnAddingSameName()
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var model1 = new AllAuthenticationSchemes.AuthenticationScheme
                {
                    Name = "testscheme1",
                    DisplayName = "Display Name 1",
                    Properties = new MapDbContextLib.Models.WsFederationSchemeProperties { Wtrealm = "realm1", MetadataAddress = "address1" },
                    Type = AuthenticationType.WsFederation,
                };
                var model2 = new AllAuthenticationSchemes.AuthenticationScheme
                {
                    Name = "testscheme1",
                    DisplayName = "Display Name 2",
                    Properties = new MapDbContextLib.Models.WsFederationSchemeProperties { Wtrealm = "realm2", MetadataAddress = "address2" },
                    Type = AuthenticationType.WsFederation,
                };
                #endregion

                #region Act
                var result1 = await controller.AddNewAuthenticationScheme(model1);
                var result2 = await controller.AddNewAuthenticationScheme(model2);
                #endregion

                #region Assert
                Assert.IsType<OkResult>(result1);

                var typedResult2 = Assert.IsType<StatusCodeResult>(result2);
                Assert.Equal(500, typedResult2.StatusCode);
                var allDbSchemes = TestResources.DbContext.AuthenticationScheme.Where(c => c.Name == model1.Name).ToList();
                Assert.Single(allDbSchemes);
                Assert.Equal(model1.Name, allDbSchemes.First().Name);
                Assert.Equal(model1.DisplayName, allDbSchemes.First().DisplayName);
                #endregion
            }
        }

        [Fact]
        public async Task AddNewAuthenticationScheme_FailsOnDefaultSchemeType()
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var model = new AllAuthenticationSchemes.AuthenticationScheme
                {
                    Name = IdentityConstants.ApplicationScheme,
                    DisplayName = "Default scheme",
                    Properties = new AuthenticationSchemeProperties(),
                    Type = AuthenticationType.Default,
                };
                #endregion

                #region Act
                var result = await controller.AddNewAuthenticationScheme(model);
                #endregion

                #region Assert
                StatusCodeResult typedResult = Assert.IsType<StatusCodeResult>(result);
                Assert.Equal(500, typedResult.StatusCode);
                #endregion
            }
        }

        #endregion

        #region update action tests
        [Theory]
        [InlineData(-1, "Name")]
        public async Task UpdateProfitCenter_Invalid(int profitCenterId, string profitCenterName)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var profitCenter = new ProfitCenter
                {
                    Id = TestUtil.MakeTestGuid(profitCenterId),
                    Name = profitCenterName,
                    ProfitCenterCode = "PC",
                };
                #endregion

                #region Act
                var preCount = TestResources.DbContext.ProfitCenter.Count();
                var json = await controller.UpdateProfitCenter(profitCenter);
                var postCount = TestResources.DbContext.ProfitCenter.Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Fact]
        public async Task UpdateProfitCenter_Success()
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var profitCenter = new ProfitCenter
                {
                    Id = TestUtil.MakeTestGuid(1),
                    Name = "Name",
                    ProfitCenterCode = "PC",
                };
                #endregion

                #region Act
                var preCount = TestResources.DbContext.ProfitCenter.Count();
                var json = await controller.UpdateProfitCenter(profitCenter);
                var postCount = TestResources.DbContext.ProfitCenter.Count();
                #endregion

                #region Assert
                Assert.IsType<JsonResult>(json);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Fact]
        public async Task UpdateAuthenticationScheme_Success()
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");

                WsFederationSchemeProperties modelProperties = new WsFederationSchemeProperties
                {
                    Wtrealm = "new realm",
                    MetadataAddress = "https://newmetadata/",
                };
                var model = new AllAuthenticationSchemes.AuthenticationScheme
                {
                    Name = "prmtest",
                    DisplayName = "New display name",
                    Properties = modelProperties,
                    Type = AuthenticationType.WsFederation,
                    DomainList = new List<string> { "newDomain1.com", "newDomain2.com" },
                };

                var beforeAllSchemes = await TestResources.AuthenticationSchemeProvider.GetAllSchemesAsync();
                #endregion

                #region Act
                var result = await controller.UpdateAuthenticationScheme(model);

                var afterAllSchemes = await TestResources.AuthenticationSchemeProvider.GetAllSchemesAsync();
                Microsoft.AspNetCore.Authentication.AuthenticationScheme afterUpdatedScheme = afterAllSchemes.Single(s => s.Name == model.Name);

                var dbUpdatedSchemeList = TestResources.DbContext.AuthenticationScheme.Where(s => s.Name == model.Name).ToList();
                #endregion

                #region Assert
                Assert.IsType<OkResult>(result);
                Assert.Equal(beforeAllSchemes.Count(), afterAllSchemes.Count());

                Assert.Equal(model.DisplayName, afterUpdatedScheme.DisplayName);

                WsFederationSchemeProperties typedProperties = (WsFederationSchemeProperties)model.Properties;
                Assert.Equal(modelProperties.MetadataAddress, typedProperties.MetadataAddress);
                Assert.Equal(modelProperties.Wtrealm, typedProperties.Wtrealm);
                Assert.Equal(model.DisplayName, afterUpdatedScheme.DisplayName);

                // Assert database updated correctly
                var dbUpdatedScheme = Assert.Single(dbUpdatedSchemeList);
                Assert.IsType<WsFederationSchemeProperties>(dbUpdatedScheme.SchemePropertiesObj);
                WsFederationSchemeProperties typedDbProperties = (WsFederationSchemeProperties)model.Properties;
                Assert.Equal(modelProperties.MetadataAddress, typedDbProperties.MetadataAddress);
                Assert.Equal(modelProperties.Wtrealm, typedDbProperties.Wtrealm);
                Assert.True(model.DomainList.ToHashSet().SetEquals(dbUpdatedScheme.DomainList.ToHashSet()));
                #endregion
            }
        }

        [Fact]
        public async Task UpdateAuthenticationScheme_FailOnDefaultScheme()
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                var model = new AllAuthenticationSchemes.AuthenticationScheme
                {
                    Name = IdentityConstants.ApplicationScheme,
                    DisplayName = "Default scheme",
                    Properties = new AuthenticationSchemeProperties(),
                    Type = AuthenticationType.Default,
                };
                #endregion

                #region Act
                var result = await controller.UpdateAuthenticationScheme(model);
                #endregion

                #region Assert
                StatusCodeResult typedResult = Assert.IsType<StatusCodeResult>(result);
                Assert.Equal(500, typedResult.StatusCode);
                #endregion
            }
        }

        [Fact]
        public async Task UpdateAuthenticationScheme_FailOnNonExistingSchemeName()
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");

                WsFederationSchemeProperties modelProperties = new WsFederationSchemeProperties
                {
                    Wtrealm = "new realm",
                    MetadataAddress = "https://newmetadata/",
                };
                var model = new AllAuthenticationSchemes.AuthenticationScheme
                {
                    Name = "NonExistingSchemeName",
                    DisplayName = "New display name",
                    Properties = modelProperties,
                    Type = AuthenticationType.WsFederation,
                    DomainList = new List<string> { "newDomain1.com", "newDomain2.com" },
                };
                #endregion

                #region Act
                var result = await controller.UpdateAuthenticationScheme(model);
                #endregion

                #region Assert
                StatusCodeResult typedResult = Assert.IsType<StatusCodeResult>(result);
                Assert.Equal(500, typedResult.StatusCode);
                #endregion
            }
        }

        [Fact]
        public async Task UpdateAuthenticationScheme_FailOnSchemeTypeMismatch()
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");

                WsFederationSchemeProperties modelProperties = new WsFederationSchemeProperties
                {
                    Wtrealm = "new realm",
                    MetadataAddress = "https://newmetadata/",
                };
                var model = new AllAuthenticationSchemes.AuthenticationScheme
                {
                    Name = "prmtest",
                    DisplayName = "New display name",
                    Properties = modelProperties,
                    Type = (AuthenticationType)999,
                    DomainList = new List<string> { "newDomain1.com", "newDomain2.com" },
                };
                #endregion

                #region Act
                var result = await controller.UpdateAuthenticationScheme(model);
                #endregion

                #region Assert
                StatusCodeResult typedResult = Assert.IsType<StatusCodeResult>(result);
                Assert.Equal(500, typedResult.StatusCode);
                #endregion
            }
        }

        /// <summary>
        /// Validate that EditClient call with domain list exceeding the limit causes error
        /// </summary>
        [Fact]
        public async Task UpdateClient_ErrorWhenDomainLimitExceeded()
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                SystemAdminController controller = await GetControllerForUser("sysAdmin1");
                Client testClient = TestResources.DbContext.Client.Find(TestUtil.MakeTestGuid(1));
                var updateObj = new ClientUpdate
                {
                    ClientId = testClient.Id,
                    DomainLimitChange = new ClientUpdate.DomainLimitUpdate
                    {
                        DomainLimitRequestedByPersonName = "xyz",
                        DomainLimitReason = "Not telling",
                        NewDomainLimit = 1,
                    }
                };
                #endregion

                #region Act
                var view = await controller.UpdateClient(updateObj);
                #endregion

                #region Assert
                StatusCodeResult result = Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, result.StatusCode);
                #endregion
            }
        }

        #endregion

        #region Remove/delete action tests
        [Theory]
        [InlineData(-1)]
        [InlineData(1)]  // Cannot delete if there are referencing clients
        public async Task DeleteProfitCenter_Invalid(int profitCenterId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.ProfitCenter.Count();
                var json = await controller.DeleteProfitCenter(TestUtil.MakeTestGuid(profitCenterId));
                var postCount = TestResources.DbContext.ProfitCenter.Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(2)]
        public async Task DeleteProfitCenter_Success(int profitCenterId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.ProfitCenter.Count();
                var json = await controller.DeleteProfitCenter(TestUtil.MakeTestGuid(profitCenterId));
                var postCount = TestResources.DbContext.ProfitCenter.Count();
                #endregion

                #region Assert
                Assert.IsType<JsonResult>(json);
                Assert.Equal(preCount - 1, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public async Task RemoveUserFromProfitCenter_Invalid(int userId, int profitCenterId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.UserRoleInProfitCenter.Count();
                var json = await controller.RemoveUserFromProfitCenter(TestUtil.MakeTestGuid(userId), TestUtil.MakeTestGuid(profitCenterId));
                var postCount = TestResources.DbContext.UserRoleInProfitCenter.Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(1, 1)]
        public async Task RemoveUserFromProfitCenter_Success(int userId, int profitCenterId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.UserRoleInProfitCenter.Count();
                var json = await controller.RemoveUserFromProfitCenter(TestUtil.MakeTestGuid(userId), TestUtil.MakeTestGuid(profitCenterId));
                var postCount = TestResources.DbContext.UserRoleInProfitCenter.Count();
                #endregion

                #region Assert
                Assert.IsType<JsonResult>(json);
                Assert.Equal(preCount - 1, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public async Task CancelPublication_Invalid(int rootContentItemId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.ContentPublicationRequest.Where(pr => PublicationStatusExtensions.ActiveStatuses.Contains(pr.RequestStatus)).Count();
                var json = await controller.CancelPublication(TestUtil.MakeTestGuid(rootContentItemId));
                var postCount = TestResources.DbContext.ContentPublicationRequest.Where(pr => PublicationStatusExtensions.ActiveStatuses.Contains(pr.RequestStatus)).Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(1)]
        public async Task CancelPublication_Success(int rootContentItemId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.ContentPublicationRequest.Where(pr => PublicationStatusExtensions.ActiveStatuses.Contains(pr.RequestStatus)).Count();
                var json = await controller.CancelPublication(TestUtil.MakeTestGuid(rootContentItemId));
                var postCount = TestResources.DbContext.ContentPublicationRequest.Where(pr => PublicationStatusExtensions.ActiveStatuses.Contains(pr.RequestStatus)).Count();
                #endregion

                #region Assert
                Assert.IsType<JsonResult>(json);
                Assert.Equal(preCount - 1, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public async Task CancelReduction_Invalid(int selectionGroupId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.ContentReductionTask.Where(rt => ReductionStatusExtensions.activeStatusList.Contains(rt.ReductionStatus)).Count();
                var json = await controller.CancelReduction(TestUtil.MakeTestGuid(selectionGroupId));
                var postCount = TestResources.DbContext.ContentReductionTask.Where(rt => ReductionStatusExtensions.activeStatusList.Contains(rt.ReductionStatus)).Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(1)]
        public async Task CancelReduction_Success(int selectionGroupId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var preCount = TestResources.DbContext.ContentReductionTask.Where(rt => ReductionStatusExtensions.activeStatusList.Contains(rt.ReductionStatus)).Count();
                var json = await controller.CancelReduction(TestUtil.MakeTestGuid(selectionGroupId));
                var postCount = TestResources.DbContext.ContentReductionTask.Where(rt => ReductionStatusExtensions.activeStatusList.Contains(rt.ReductionStatus)).Count();
                #endregion

                #region Assert
                Assert.IsType<JsonResult>(json);
                Assert.Equal(preCount - 1, postCount);
                #endregion
            }
        }
        #endregion

        #region Immediate toggle action tests
        [Theory]
        [InlineData(-1, RoleEnum.Admin)]
        [InlineData(1, RoleEnum.Admin)]  // Cannot remove self as admin
        [InlineData(2, RoleEnum.UserCreator)]
        public async Task SystemRole_Invalid(int userId, RoleEnum role)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var json = await controller.SystemRole(TestUtil.MakeTestGuid(userId), role, false);
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                #endregion
            }
        }
        [Theory]
        [InlineData(2, RoleEnum.Admin, false)]
        public async Task SystemRole_Success(int userId, RoleEnum role, bool expectedValue)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var json1 = await controller.SystemRole(TestUtil.MakeTestGuid(userId), role);
                var json2 = await controller.SystemRole(TestUtil.MakeTestGuid(userId), role, !expectedValue);
                #endregion

                #region Assert
                JsonResult result1 = Assert.IsType<JsonResult>(json1);
                Assert.Equal(expectedValue, (bool)result1.Value);
                JsonResult result2 = Assert.IsType<JsonResult>(json2);
                Assert.Equal(!expectedValue, (bool)result2.Value);
                #endregion
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]  // Cannot suspend self
        public async Task UserSuspension_Invalid(int userId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var json = await controller.UserSuspendedStatus(TestUtil.MakeTestGuid(userId), true);
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                #endregion
            }
        }
        [Theory]
        [InlineData(2, false)]
        public async Task UserSuspension_Success(int userId, bool expectedValue)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var json1 = await controller.UserSuspendedStatus(TestUtil.MakeTestGuid(userId));
                var json2 = await controller.UserSuspendedStatus(TestUtil.MakeTestGuid(userId), !expectedValue);
                #endregion

                #region Assert
                JsonResult result1 = Assert.IsType<JsonResult>(json1);
                Assert.Equal(expectedValue, (bool)result1.Value);
                JsonResult result2 = Assert.IsType<JsonResult>(json2);
                Assert.Equal(!expectedValue, (bool)result2.Value);
                #endregion
            }
        }

        [Theory]
        [InlineData(-1, 1, RoleEnum.Admin)]
        [InlineData(1, -1, RoleEnum.Admin)]
        [InlineData(1, 1, RoleEnum.UserCreator)]
        public async Task UserClientRoles_Invalid(int userId, int clientId, RoleEnum role)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var json = await controller.UserClientRoleAssignment(TestUtil.MakeTestGuid(userId), TestUtil.MakeTestGuid(clientId), role);
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                #endregion
            }
        }
        [Theory]
        [InlineData(1, 1, RoleEnum.Admin, false)]
        [InlineData(1, 1, RoleEnum.ContentAccessAdmin, false)]
        [InlineData(1, 1, RoleEnum.ContentPublisher, false)]
        [InlineData(1, 1, RoleEnum.ContentUser, false)]
        public async Task UserClientRoles_Success(int userId, int clientId, RoleEnum role, bool expectedValue)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var json1 = await controller.UserClientRoleAssignment(TestUtil.MakeTestGuid(userId), TestUtil.MakeTestGuid(clientId), role);
                var json2 = await controller.UserClientRoleAssignment(TestUtil.MakeTestGuid(userId), TestUtil.MakeTestGuid(clientId), role, !expectedValue);
                #endregion

                #region Assert
                JsonResult result1 = Assert.IsType<JsonResult>(json1);
                Assert.Equal(expectedValue, (bool)result1.Value);
                JsonResult result2 = Assert.IsType<JsonResult>(json2);
                Assert.Equal(!expectedValue, (bool)result2.Value);
                #endregion
            }
        }

        [Theory]
        [InlineData(-1)]
        public async Task ContentSuspension_Invalid(int rootContentItemId)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var json = await controller.ContentSuspendedStatus(TestUtil.MakeTestGuid(rootContentItemId));
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(json);
                Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
                #endregion
            }
        }
        [Theory]
        [InlineData(1, false)]
        public async Task ContentSuspension_Success(int rootContentItemId, bool expectedValue)
        {
            using (TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.SystemAdmin))
            {
                #region Arrange
                var controller = await GetControllerForUser("sysAdmin1");
                #endregion

                #region Act
                var json1 = await controller.ContentSuspendedStatus(TestUtil.MakeTestGuid(rootContentItemId));
                var json2 = await controller.ContentSuspendedStatus(TestUtil.MakeTestGuid(rootContentItemId), !expectedValue);
                #endregion

                #region Assert
                JsonResult result1 = Assert.IsType<JsonResult>(json1);
                Assert.Equal(expectedValue, (bool)result1.Value);
                JsonResult result2 = Assert.IsType<JsonResult>(json2);
                Assert.Equal(!expectedValue, (bool)result2.Value);
                #endregion
            }
        }
        #endregion

    }
}
