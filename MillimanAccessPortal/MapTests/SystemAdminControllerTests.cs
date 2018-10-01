/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Unit tests for the system admin controller
 * DEVELOPER NOTES: 
 */

using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.SystemAdmin;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestResourcesLib;
using Xunit;

namespace MapTests
{
    public class SystemAdminControllerTests
    {
        internal TestInitialization _testResources { get; set; }

        /// <summary>Initializes test resources.</summary>
        /// <remarks>This constructor is called before each test.</remarks>
        public SystemAdminControllerTests()
        {
            _testResources = new TestInitialization();
            _testResources.GenerateTestData(new DataSelection[] { DataSelection.SystemAdmin });
        }

        /// <summary>Constructs a controller with the specified active user.</summary>
        /// <param name="Username"></param>
        /// <returns>SystemAdminController</returns>
        public async Task<SystemAdminController> GetControllerForUser(string Username)
        {
            var accountController = new AccountController(
                _testResources.DbContextObject,
                _testResources.UserManagerObject,
                _testResources.RoleManagerObject,
                null,
                _testResources.MessageQueueServicesObject,
                _testResources.LoggerFactory,
                _testResources.AuditLoggerObject,
                _testResources.QueriesObj,
                _testResources.AuthorizationService,
                _testResources.ConfigurationObject,
                _testResources.ServiceProviderObject);

            var testController = new SystemAdminController(
                accountController,
                _testResources.AuditLoggerObject,
                _testResources.AuthorizationService,
                _testResources.ConfigurationObject,
                _testResources.DbContextObject,
                _testResources.LoggerFactory,
                _testResources.QueriesObj,
                _testResources.RoleManagerObject,
                _testResources.UserManagerObject);

            try
            {
                Username = (await _testResources.UserManagerObject.FindByNameAsync(Username)).UserName;
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
            var controller = await GetControllerForUser("sysUser1");
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
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var view = await controller.Index();
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            #endregion
        }

        #region query action tests
        #region user queries
        [Fact]
        public async Task Users_Success_FilterNone()
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
            Assert.IsType<JsonResult>(json);
            Assert.Equal(expectedUsers, ((json as JsonResult).Value as List<UserInfo>).Count);
            #endregion
        }
        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 0)]
        public async Task Users_Success_FilterClient(int clientId, int expectedUsers)
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
            Assert.IsType<JsonResult>(json);
            Assert.Equal(expectedUsers, ((json as JsonResult).Value as List<UserInfo>).Count);
            #endregion
        }
        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 0)]
        public async Task Users_Success_FilterProfitCenter(int profitCenterId, int expectedUsers)
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
            Assert.IsType<JsonResult>(json);
            Assert.Equal(expectedUsers, ((json as JsonResult).Value as List<UserInfo>).Count);
            #endregion
        }

        [Theory]
        [InlineData(null)]
        [InlineData(-1)]
        public async Task UserDetail_Invalid(int? userId)
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
        [Theory]
        [InlineData(1)]
        public async Task UserDetail_Success_FilterNone(int userId)
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
            Assert.IsType<JsonResult>(json);
            Assert.IsType<UserDetail>((json as JsonResult).Value);
            #endregion
        }
        [Theory]
        [InlineData(1, 1)]
        public async Task UserDetail_Success_FilterClient(int userId, int clientId)
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
            Assert.IsType<JsonResult>(json);
            Assert.IsType<UserDetailForClient>((json as JsonResult).Value);
            #endregion
        }
        [Theory]
        [InlineData(1, 1)]
        public async Task UserDetail_Success_FilterProfitCenter(int userId, int profitCenterId)
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
            Assert.IsType<JsonResult>(json);
            Assert.IsType<UserDetailForProfitCenter>((json as JsonResult).Value);
            #endregion
        }
        #endregion

        #region client queries
        [Fact]
        public async Task Clients_Success_FilterNone()
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
            Assert.IsType<JsonResult>(json);
            Assert.Equal(expectedClients, ((json as JsonResult).Value as BasicTree<ClientInfo>).Root.Children.Count);
            #endregion
        }
        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 0)]
        public async Task Clients_Success_FilterUser(int userId, int expectedClients)
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
            Assert.IsType<JsonResult>(json);
            Assert.Equal(expectedClients, ((json as JsonResult).Value as BasicTree<ClientInfo>).Root.Children.Count);
            #endregion
        }
        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 0)]
        public async Task Clients_Success_FilterProfitCenter(int profitCenterId, int expectedClients)
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
            Assert.IsType<JsonResult>(json);
            Assert.Equal(expectedClients, ((json as JsonResult).Value as BasicTree<ClientInfo>).Root.Children.Count);
            #endregion
        }

        [Theory]
        [InlineData(null)]
        [InlineData(-1)]
        public async Task ClientDetail_Invalid(int? clientId)
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
        [Theory]
        [InlineData(1)]
        public async Task ClientDetail_Success_FilterNone(int clientId)
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
            Assert.IsType<JsonResult>(json);
            Assert.IsType<ClientDetail>((json as JsonResult).Value);
            #endregion
        }
        [Theory]
        [InlineData(1, 1)]
        public async Task ClientDetail_Success_FilterUser(int clientId, int userId)
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
            Assert.IsType<JsonResult>(json);
            Assert.IsType<ClientDetailForUser>((json as JsonResult).Value);
            #endregion
        }
        [Theory]
        [InlineData(1, 1)]
        public async Task ClientDetail_Success_FilterProfitCenter(int clientId, int profitCenterId)
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
            Assert.IsType<JsonResult>(json);
            Assert.IsType<ClientDetailForProfitCenter>((json as JsonResult).Value);
            #endregion
        }
        #endregion

        #region profit center queries
        [Fact]
        public async Task ProfitCenters_Success_FilterNone()
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
            Assert.IsType<JsonResult>(json);
            Assert.Equal(expectedProfitCenters, ((json as JsonResult).Value as List<ProfitCenterInfo>).Count);
            #endregion
        }

        [Theory]
        [InlineData(null)]
        [InlineData(-1)]
        public async Task ProfitCenterDetail_Invalid(int? profitCenterId)
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
        [Theory]
        [InlineData(1)]
        public async Task ProfitCenterDetail_Success_FilterNone(int profitCenterId)
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
            Assert.IsType<JsonResult>(json);
            Assert.IsType<ProfitCenterDetail>((json as JsonResult).Value);
            #endregion
        }
        #endregion

        #region root content item queries
        [Theory]
        [InlineData(1, 0)]
        [InlineData(11, 1)]
        public async Task RootContentItems_Success_FilterUser(int userId, int expectedRootContentItems)
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
            Assert.IsType<JsonResult>(json);
            Assert.Equal(expectedRootContentItems, ((json as JsonResult).Value as List<RootContentItemInfo>).Count);
            #endregion
        }
        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 0)]
        public async Task RootContentItems_Success_FilterClient(int clientId, int expectedRootContentItems)
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
            Assert.IsType<JsonResult>(json);
            Assert.Equal(expectedRootContentItems, ((json as JsonResult).Value as List<RootContentItemInfo>).Count);
            #endregion
        }

        [Theory]
        [InlineData(null)]
        [InlineData(-1)]
        public async Task RootContentItemDetail_Invalid(int? rootContentItemId)
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
        [Theory]
        [InlineData(1, 1)]
        public async Task RootContentItemDetail_Success_FilterUser(int rootContentItemId, int userId)
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
            Assert.IsType<JsonResult>(json);
            Assert.IsType<RootContentItemDetailForUser>((json as JsonResult).Value);
            #endregion
        }
        [Theory]
        [InlineData(1, 1)]
        public async Task RootContentItemDetail_Success_FilterRootContentItem(int rootContentItemId, int clientId)
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
            Assert.IsType<JsonResult>(json);
            Assert.IsType<RootContentItemDetailForClient>((json as JsonResult).Value);
            #endregion
        }
        #endregion
        #endregion

        #region create action queries
        [Theory]
        [InlineData("invalid_email_address")]
        [InlineData("sysAdmin1@site.domain")]
        public async Task CreateUser_Invalid(string email)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.Users.Count();
            var json = await controller.CreateUser(email);
            var postCount = _testResources.DbContextObject.Users.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(json);
            Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
            Assert.Equal(preCount, postCount);
            #endregion
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
            var preCount = _testResources.DbContextObject.Users.Count();
            var json = await controller.CreateUser(email);
            var postCount = _testResources.DbContextObject.Users.Count();
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
            var preCount = _testResources.DbContextObject.ProfitCenter.Count();
            var json = await controller.CreateProfitCenter(profitCenter);
            var postCount = _testResources.DbContextObject.ProfitCenter.Count();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json);
            Assert.Equal(preCount + 1, postCount);
            #endregion
        }

        [Theory]
        [InlineData("invalid_email_address", 1)]
        [InlineData("sysUser1@site.domain", 99)]
        public async Task AddUserToClient_Invalid(string email, int clientId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.UserClaims.Count();
            var json = await controller.AddUserToClient(email, TestUtil.MakeTestGuid(clientId));
            var postCount = _testResources.DbContextObject.UserClaims.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(json);
            Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData("sysUser2@site.domain", 1)]
        // [InlineData("sysUser3@site.domain", 1)]  // Disabled until mail sender is testable.
        public async Task AddUserToClient_Success(string email, int clientId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.UserClaims.Count();
            var json = await controller.AddUserToClient(email, TestUtil.MakeTestGuid(clientId));
            var postCount = _testResources.DbContextObject.UserClaims.Count();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json);
            Assert.Equal(preCount + 1, postCount);
            #endregion
        }

        [Theory]
        [InlineData("sysUser1@site.domain", 1)]
        public async Task AddUserToClient_NoOp(string email, int clientId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.UserClaims.Count();
            var json = await controller.AddUserToClient(email, TestUtil.MakeTestGuid(clientId));
            var postCount = _testResources.DbContextObject.UserClaims.Count();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData("invalid_email_address", 1)]
        [InlineData("sysUser1@site.domain", 99)]
        public async Task AddUserToProfitCenter_Invalid(string email, int profitCenterId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.UserRoleInProfitCenter.Count();
            var json = await controller.AddUserToProfitCenter(email, TestUtil.MakeTestGuid(profitCenterId));
            var postCount = _testResources.DbContextObject.UserRoleInProfitCenter.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(json);
            Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData("sysUser1@site.domain", 1)]
        // [InlineData("sysUser3@site.domain", 1)]  // Disabled until mail sender is testable.
        public async Task AddUserToProfitCenter_Success(string email, int profitCenterId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.UserRoleInProfitCenter.Count();
            var json = await controller.AddUserToProfitCenter(email, TestUtil.MakeTestGuid(profitCenterId));
            var postCount = _testResources.DbContextObject.UserRoleInProfitCenter.Count();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json);
            Assert.Equal(preCount + 1, postCount);
            #endregion
        }

        [Theory]
        [InlineData("sysAdmin1@site.domain", 1)]
        public async Task AddUserToProfitCenter_NoOp(string email, int profitCenterId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.UserRoleInProfitCenter.Count();
            var json = await controller.AddUserToProfitCenter(email, TestUtil.MakeTestGuid(profitCenterId));
            var postCount = _testResources.DbContextObject.UserRoleInProfitCenter.Count();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json);
            Assert.Equal(preCount, postCount);
            #endregion
        }
        #endregion

        #region update action tests
        [Theory]
        [InlineData(-1, "Name")]
        public async Task UpdateProfitCenter_Invalid(int profitCenterId, string profitCenterName)
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
            var preCount = _testResources.DbContextObject.ProfitCenter.Count();
            var json = await controller.UpdateProfitCenter(profitCenter);
            var postCount = _testResources.DbContextObject.ProfitCenter.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(json);
            Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Fact]
        public async Task UpdateProfitCenter_Success()
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
            var preCount = _testResources.DbContextObject.ProfitCenter.Count();
            var json = await controller.UpdateProfitCenter(profitCenter);
            var postCount = _testResources.DbContextObject.ProfitCenter.Count();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json);
            Assert.Equal(preCount, postCount);
            #endregion
        }
        #endregion

        #region Remove/delete action tests
        [Theory]
        [InlineData(-1)]
        [InlineData(1)]  // Cannot delete if there are referencing clients
        public async Task DeleteProfitCenter_Invalid(int profitCenterId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.ProfitCenter.Count();
            var json = await controller.DeleteProfitCenter(TestUtil.MakeTestGuid(profitCenterId));
            var postCount = _testResources.DbContextObject.ProfitCenter.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(json);
            Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData(2)]
        public async Task DeleteProfitCenter_Success(int profitCenterId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.ProfitCenter.Count();
            var json = await controller.DeleteProfitCenter(TestUtil.MakeTestGuid(profitCenterId));
            var postCount = _testResources.DbContextObject.ProfitCenter.Count();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json);
            Assert.Equal(preCount - 1, postCount);
            #endregion
        }

        [Theory]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public async Task RemoveUserFromProfitCenter_Invalid(int userId, int profitCenterId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.UserRoleInProfitCenter.Count();
            var json = await controller.RemoveUserFromProfitCenter(TestUtil.MakeTestGuid(userId), TestUtil.MakeTestGuid(profitCenterId));
            var postCount = _testResources.DbContextObject.UserRoleInProfitCenter.Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(json);
            Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData(1, 1)]
        public async Task RemoveUserFromProfitCenter_Success(int userId, int profitCenterId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.UserRoleInProfitCenter.Count();
            var json = await controller.RemoveUserFromProfitCenter(TestUtil.MakeTestGuid(userId), TestUtil.MakeTestGuid(profitCenterId));
            var postCount = _testResources.DbContextObject.UserRoleInProfitCenter.Count();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json);
            Assert.Equal(preCount - 1, postCount);
            #endregion
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public async Task CancelPublication_Invalid(int rootContentItemId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.ContentPublicationRequest.Where(pr => pr.RequestStatus.IsActive()).Count();
            var json = await controller.CancelPublication(TestUtil.MakeTestGuid(rootContentItemId));
            var postCount = _testResources.DbContextObject.ContentPublicationRequest.Where(pr => pr.RequestStatus.IsActive()).Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(json);
            Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData(1)]
        public async Task CancelPublication_Success(int rootContentItemId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.ContentPublicationRequest.Where(pr => pr.RequestStatus.IsActive()).Count();
            var json = await controller.CancelPublication(TestUtil.MakeTestGuid(rootContentItemId));
            var postCount = _testResources.DbContextObject.ContentPublicationRequest.Where(pr => pr.RequestStatus.IsActive()).Count();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json);
            Assert.Equal(preCount - 1, postCount);
            #endregion
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public async Task CancelReduction_Invalid(int selectionGroupId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.ContentReductionTask.Where(rt => rt.ReductionStatus.IsActive()).Count();
            var json = await controller.CancelReduction(TestUtil.MakeTestGuid(selectionGroupId));
            var postCount = _testResources.DbContextObject.ContentReductionTask.Where(rt => rt.ReductionStatus.IsActive()).Count();
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(json);
            Assert.Equal(422, ((StatusCodeResult)json).StatusCode);
            Assert.Equal(preCount, postCount);
            #endregion
        }

        [Theory]
        [InlineData(1)]
        public async Task CancelReduction_Success(int selectionGroupId)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var preCount = _testResources.DbContextObject.ContentReductionTask.Where(rt => rt.ReductionStatus.IsActive()).Count();
            var json = await controller.CancelReduction(TestUtil.MakeTestGuid(selectionGroupId));
            var postCount = _testResources.DbContextObject.ContentReductionTask.Where(rt => rt.ReductionStatus.IsActive()).Count();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json);
            Assert.Equal(preCount - 1, postCount);
            #endregion
        }
        #endregion

        #region Immediate toggle action tests
        [Theory]
        [InlineData(-1, RoleEnum.Admin)]
        [InlineData(1, RoleEnum.Admin)]  // Cannot remove self as admin
        [InlineData(2, RoleEnum.UserCreator)]
        public async Task SystemRole_Invalid(int userId, RoleEnum role)
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
        [Theory]
        [InlineData(2, RoleEnum.Admin, false)]
        public async Task SystemRole_Success(int userId, RoleEnum role, bool expectedValue)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var json1 = await controller.SystemRole(TestUtil.MakeTestGuid(userId), role);
            var json2 = await controller.SystemRole(TestUtil.MakeTestGuid(userId), role, !expectedValue);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json1);
            Assert.Equal(expectedValue, (bool)(json1 as JsonResult).Value);
            Assert.IsType<JsonResult>(json2);
            Assert.Equal(!expectedValue, (bool)(json2 as JsonResult).Value);
            #endregion
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]  // Cannot suspend self
        public async Task UserSuspension_Invalid(int userId)
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
        [Theory]
        [InlineData(2, false)]
        public async Task UserSuspension_Success(int userId, bool expectedValue)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var json1 = await controller.UserSuspendedStatus(TestUtil.MakeTestGuid(userId));
            var json2 = await controller.UserSuspendedStatus(TestUtil.MakeTestGuid(userId), !expectedValue);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json1);
            Assert.Equal(expectedValue, (bool)(json1 as JsonResult).Value);
            Assert.IsType<JsonResult>(json2);
            Assert.Equal(!expectedValue, (bool)(json2 as JsonResult).Value);
            #endregion
        }

        [Theory]
        [InlineData(-1, 1, RoleEnum.Admin)]
        [InlineData(1, -1, RoleEnum.Admin)]
        [InlineData(1, 1, RoleEnum.UserCreator)]
        public async Task UserClientRoles_Invalid(int userId, int clientId, RoleEnum role)
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
        [Theory]
        [InlineData(1, 1, RoleEnum.Admin, false)]
        [InlineData(1, 1, RoleEnum.ContentAccessAdmin, false)]
        [InlineData(1, 1, RoleEnum.ContentPublisher, false)]
        [InlineData(1, 1, RoleEnum.ContentUser, false)]
        public async Task UserClientRoles_Success(int userId, int clientId, RoleEnum role, bool expectedValue)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var json1 = await controller.UserClientRoleAssignment(TestUtil.MakeTestGuid(userId), TestUtil.MakeTestGuid(clientId), role);
            var json2 = await controller.UserClientRoleAssignment(TestUtil.MakeTestGuid(userId), TestUtil.MakeTestGuid(clientId), role, !expectedValue);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json1);
            Assert.Equal(expectedValue, (bool)(json1 as JsonResult).Value);
            Assert.IsType<JsonResult>(json2);
            Assert.Equal(!expectedValue, (bool)(json2 as JsonResult).Value);
            #endregion
        }

        [Theory]
        [InlineData(-1)]
        public async Task ContentSuspension_Invalid(int rootContentItemId)
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
        [Theory]
        [InlineData(1, false)]
        public async Task ContentSuspension_Success(int rootContentItemId, bool expectedValue)
        {
            #region Arrange
            var controller = await GetControllerForUser("sysAdmin1");
            #endregion

            #region Act
            var json1 = await controller.ContentSuspendedStatus(TestUtil.MakeTestGuid(rootContentItemId));
            var json2 = await controller.ContentSuspendedStatus(TestUtil.MakeTestGuid(rootContentItemId), !expectedValue);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(json1);
            Assert.Equal(expectedValue, (bool)(json1 as JsonResult).Value);
            Assert.IsType<JsonResult>(json2);
            Assert.Equal(!expectedValue, (bool)(json2 as JsonResult).Value);
            #endregion
        }
        #endregion

    }
}
