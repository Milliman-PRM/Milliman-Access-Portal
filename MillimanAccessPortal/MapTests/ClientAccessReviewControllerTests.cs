/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Unit tests for actions in the ClientReviewController
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.ClientAccessReview;
using System;
using System.Collections.Generic;
using System.Text;
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
                testResources.ContentAccessAdminQueries,
                testResources.ClientAccessReviewQueries,
                testResources.UserManager,
                testResources.Configuration);

            // Generating ControllerContext will throw a NullReferenceException if the provided user does not exist
            testController.ControllerContext = testResources.GenerateControllerContext(userName: (await testResources.UserManager.FindByNameAsync(UserName)).UserName);
            testController.HttpContext.Session = new MockSession();

            return testController;
        }

        /// <summary>
        /// Checks that the contents of the PageGlobalData action reflect the application configuration
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
                var configuredClientReviewGracePeriodDays = TestResources.Configuration.GetValue("ClientReviewGracePeriodDays", -1);
                var configuredClientReviewEarlyWarningDays = TestResources.Configuration.GetValue("ClientReviewEarlyWarningDays", -1);
                #endregion

                #region Act
                var view = await controller.PageGlobalData();
                #endregion

                #region Assert
                var result = Assert.IsType<JsonResult>(view);
                var model = Assert.IsType<ClientReviewGlobalDataModel>(result.Value);
                Assert.Equal(configuredClientReviewGracePeriodDays, model.ClientReviewGracePeriodDays);
                Assert.Equal(configuredClientReviewEarlyWarningDays, model.ClientReviewEarlyWarningDays);
                #endregion
            }
        }

        /// <summary>
        /// Checks that the contents of the PageGlobalData action reflect the application configuration
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
        /// Checks that the contents of the PageGlobalData action reflect the application configuration
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
        /// Checks that the contents of the PageGlobalData action reflect the application configuration
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
        /// Checks that the contents of the PageGlobalData action reflect the application configuration
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

    }
}
