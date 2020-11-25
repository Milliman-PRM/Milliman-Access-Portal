/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Unit tests for actions in the ClientAccessReviewController
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.ClientAccessReview;
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
    public class ClientAccessReviewControllerTests
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;

        public ClientAccessReviewControllerTests(DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _dbLifeTimeFixture = dbLifeTimeFixture;
        }

        /// <summary>
        /// Common controller constructor to be used by all tests
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        private async Task<ClientAccessReviewController> GetControllerForUser(TestInitialization testResources, string UserName = null)
        {
            ClientAccessReviewController testController = new ClientAccessReviewController(
                testResources.AuditLogger,
                testResources.AuthorizationService,
                testResources.ClientAccessReviewQueries,
                testResources.UserManager,
                testResources.Configuration);

            // Generating ControllerContext will throw a NullReferenceException if the provided user does not exist
            testController.ControllerContext = testResources.GenerateControllerContext(userName: (await testResources.UserManager.FindByNameAsync(UserName)).UserName);
            testController.HttpContext.Session = new MockSession();

            return testController;
        }

        /// <summary>
        /// Checks that the PageGlobalData action enforces authorization
        /// </summary>
        [Fact]
        public async Task PageGlobalData_Unauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAccessReviewController controller = await GetControllerForUser(TestResources, "test1");
                #endregion

                #region Act
                var view = await controller.PageGlobalData();
                #endregion

                #region Assert
                var result = Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Checks that the contents of the PageGlobalData action reflect the application configuration
        /// </summary>
        [Fact]
        public async Task PageGlobalData_Valid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAccessReviewController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                var configuredClientReviewEarlyWarningDays = TestResources.Configuration.GetValue("ClientReviewEarlyWarningDays", -1);
                #endregion

                #region Act
                var view = await controller.PageGlobalData();
                #endregion

                #region Assert
                var result = Assert.IsType<JsonResult>(view);
                var model = Assert.IsType<ClientAccessReviewGlobalDataModel>(result.Value);
                Assert.Equal(configuredClientReviewEarlyWarningDays, model.ClientReviewEarlyWarningDays);
                #endregion
            }
        }

        /// <summary>
        /// Checks that the Clients action enforces authorization
        /// </summary>
        [Fact]
        public async Task Clients_Unauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAccessReviewController controller = await GetControllerForUser(TestResources, "test1");
                #endregion

                #region Act
                var view = await controller.Clients();
                #endregion

                #region Assert
                var result = Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Checks that the Clients action returns the appropriate type and properties
        /// </summary>
        [Fact]
        public async Task Clients_Valid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAccessReviewController controller = await GetControllerForUser(TestResources, "AdminOfChildClient");
                #endregion

                #region Act
                var view = await controller.Clients();
                #endregion

                #region Assert
                var result = Assert.IsType<JsonResult>(view);
                var model = Assert.IsType<ClientReviewClientsModel>(result.Value);
                Assert.Equal(2, model.Clients.Count);
                Assert.Single(model.ParentClients);
                #endregion
            }
        }

        /// <summary>
        /// Checks that the Index action enforces authorization
        /// </summary>
        [Fact]
        public async Task Index_Unauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAccessReviewController controller = await GetControllerForUser(TestResources, "test1");
                #endregion

                #region Act
                var view = await controller.Index();
                #endregion

                #region Assert
                var result = Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Checks that the Index action returns the expected type and characteristics
        /// </summary>
        [Fact]
        public async Task Index_Valid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAccessReviewController controller = await GetControllerForUser(TestResources, "AdminOfChildClient");
                #endregion

                #region Act
                var view = await controller.Index();
                #endregion

                #region Assert
                var result = Assert.IsType<ViewResult>(view);
                Assert.Null(result.ViewName);
                #endregion
            }
        }

        /// <summary>
        /// Checks that the ClientSummary action enforces authorization
        /// </summary>
        [Fact]
        public async Task ClientSummary_Unauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAccessReviewController controller = await GetControllerForUser(TestResources, "test1");
                Guid clientId = TestUtil.MakeTestGuid(1);
                #endregion

                #region Act
                var view = await controller.ClientSummary(clientId);
                #endregion

                #region Assert
                var result = Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Checks that the ClientSummary action returns the expected type and characteristics
        /// </summary>
        [Fact]
        public async Task ClientSummary_Valid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAccessReviewController controller = await GetControllerForUser(TestResources, "AdminOfChildClient");
                Guid clientId = TestUtil.MakeTestGuid(2);
                #endregion

                #region Act
                var view = await controller.ClientSummary(clientId);
                #endregion

                #region Assert
                var result = Assert.IsType<JsonResult>(view);
                var model = Assert.IsType<ClientSummaryModel>(result.Value);
                Assert.Equal("Name2", model.ClientName);
                Assert.Equal("ClientCode2", model.ClientCode);
                #endregion
            }
        }

        /// <summary>
        /// Checks that the BeginClientAccessReview action enforces authorization
        /// </summary>
        [Fact]
        public async Task BeginClientAccessReview_Unauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAccessReviewController controller = await GetControllerForUser(TestResources, "test1");
                Guid clientId = TestUtil.MakeTestGuid(1);
                #endregion

                #region Act
                var view = await controller.BeginClientAccessReview(clientId);
                #endregion

                #region Assert
                var result = Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Checks that the BeginClientAccessReview action returns the expected type and characteristics
        /// </summary>
        [Fact]
        public async Task BeginClientAccessReview_Valid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAccessReviewController controller = await GetControllerForUser(TestResources, "AdminOfChildClient");
                Guid clientId = TestUtil.MakeTestGuid(7);
                string expectedAttestationLanguage = TestResources.Configuration.GetValue<string>("ClientReviewAttestationLanguage");
                #endregion

                #region Act
                var view = await controller.BeginClientAccessReview(clientId);
                #endregion

                #region Assert
                var result = Assert.IsType<JsonResult>(view);
                var model = Assert.IsType<ClientAccessReviewModel>(result.Value);
                Assert.Equal("Name7", model.ClientName);
                Assert.Equal("ClientCode7", model.ClientCode);
                Assert.Equal(2, model.MemberUsers.Count);
                Assert.Contains("FN7 LN7", model.MemberUsers.Select(u => u.Name));
                Assert.Contains("Client Admin1", model.MemberUsers.Select(u => u.Name));
                Assert.Single(model.ProfitCenterAdmins);
                Assert.Equal("Client Admin1", model.ProfitCenterAdmins[0].Name);
                Assert.Single(model.ContentItems);
                Assert.Equal("RootContent 6", model.ContentItems[0].ContentItemName);
                Assert.Single(model.FileDrops);
                Assert.Equal("Client 7 File Drop 1", model.FileDrops[0].FileDropName);
                Assert.Equal(2, model.ClientAdmins.Count);
                Assert.Contains("FN7 LN7", model.ClientAdmins.Select(u => u.Name));
                Assert.Contains("Client Admin1", model.ClientAdmins.Select(u => u.Name));
                Assert.Equal("Profit Center 1", model.AssignedProfitCenterName);
                Assert.NotNull(model.AttestationLanguage);
                Assert.Equal(expectedAttestationLanguage, model.AttestationLanguage);
                #endregion
            }
        }

    }
}
