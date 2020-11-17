/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Unit tests for Client admin controller actions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.ClientAdminViewModels;
using MillimanAccessPortal.Models.ContentAccessAdmin;
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
    public class ClientAdminControllerTests
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;

        public ClientAdminControllerTests(DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _dbLifeTimeFixture = dbLifeTimeFixture;
        }

        /// <summary>
        /// Common controller constructor to be used by all tests
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        private async Task<ClientAdminController> GetControllerForUser(TestInitialization testResources, string UserName = null)
        {
            ClientAdminController testController = new ClientAdminController(testResources.DbContext,
                testResources.AuditLogger,
                testResources.AuthorizationService,
                testResources.MessageQueueServicesObject,
                testResources.RoleManager,
                testResources.StandardQueries,
                testResources.UserManager,
                testResources.Configuration,
                null, // AccountController
                testResources.ClientAdminQueries);

        // Generating ControllerContext will throw a NullReferenceException if the provided user does not exist
        testController.ControllerContext = testResources.GenerateControllerContext(userName: (await testResources.UserManager.FindByNameAsync(UserName)).UserName);
            testController.HttpContext.Session = new MockSession();

            return testController;
        }

        /// <summary>
        /// Convenience method to build a client object for the various tests that need one.
        /// Tests will modify the returned client as needed to perform their test actions.
        /// </summary>
        /// <returns></returns>
        public Client GetValidClient()
        {
            return new Client {
                Id = Guid.Empty,
                Name = "Placeholder Test Client",
                ClientCode = "Test Client 0001",
                ContactName = "Contact person",
                ContactTitle = "Manager",
                ContactEmail = "manager@placeholder.com",
                ContactPhone = "1234567890",
                ConsultantEmail = "consultant@example.com",
                ConsultantName = "Test Consultant",
                ConsultantOffice = "Indy PRM Testing",
                AcceptedEmailDomainList = new List<string> { "placeholder.com" },
                ParentClientId = TestUtil.MakeTestGuid(2),
                ProfitCenterId = TestUtil.MakeTestGuid(1)
            };
        }

        /// <summary>
        /// Checks whether the Index action returns an UnauthorizedResult when the user is not authorized to view the ClientAdmin page
        /// </summary>
        [Fact]
        public async Task Index_ErrorWhenUnauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "test1");
                #endregion

                #region Act
                var view = await controller.Index();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Checks whether the Index returns a view for authorized users
        /// </summary>
        [Fact]
        public async Task Index_ReturnsAView()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                #endregion

                #region Act
                var view = await controller.Index();
                #endregion

                #region Assert
                Assert.IsType<ViewResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Checks whether ClientFamilyList returns an error when the user is not authorized to view the ClientAdmin page
        /// </summary>
        [Fact]
        public async Task ClientFamilyList_ErrorWhenUnauthorized()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "test1");
                #endregion

                #region Act
                var view = await controller.ClientFamilyList();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Checks whether ClientFamilyList returns a list of clients for authorized users
        /// </summary>
        [Fact]
        public async Task ClientFamilyList_ReturnsAList()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                #endregion

                #region Act
                var view = await controller.ClientFamilyList();
                #endregion

                #region Assert
                JsonResult typedResult = Assert.IsType<JsonResult>(view);
                Assert.IsType<ClientAdminIndexViewModel>(typedResult.Value);
                #endregion
            }
        }

        /// <summary>
        /// Checks whether ClientDetail returns an error when the client is not found
        /// </summary>
        [Fact]
        public async Task ClientDetail_ErrorWhenNotFound()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                #endregion

                #region Act
                var view = await controller.ClientDetail(TestUtil.MakeTestGuid(-100));
                #endregion

                #region Assert
                Assert.IsType<NotFoundResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Checks whether ClientDetail returns an error when the user is not authorized to view the ClientAdmin page or to admin a related client
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", 3)] // Authorized to client admin, but not the specified client
        [InlineData("test1", 1)] // Not authorized to perform client admin
        public async Task ClientDetail_ErrorWhenUnauthorized(string userArg, int clientIdArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, userArg);
                #endregion

                #region Act
                var view = await controller.ClientDetail(TestUtil.MakeTestGuid(clientIdArg));
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Checks whether ClientDetail returns the ClientDetail Json model to authorized users
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", 1)] // Directly authorized to this client
        [InlineData("ClientAdmin1", 2)] // Authorized to a related (parent) client
        public async Task ClientDetail_ReturnsDetails(string userArg, int clientIdArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, userArg);
                #endregion

                #region Act
                var view = await controller.ClientDetail(TestUtil.MakeTestGuid(clientIdArg));
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<ClientDetailViewModel>(result.Value);
                #endregion
            }
        }

        /// <summary>
        /// Checks whether AssignUserToClient returns an error for unauthorized users
        /// Multiple authorization checks are made, so multiple users should be tested w/ various rights
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", 3, 1)] // User isn't admin on the requested client but is admin on the requested client's profit center
        [InlineData("ClientAdmin1", 4, 1)] // User is admin on the requested client but isn't admin on the requested client's profit center
        public async Task AssignUserToClient_ErrorWhenUnauthorized(string userArg, int clientIdArg, int userIdArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, userArg);
                ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = TestUtil.MakeTestGuid(clientIdArg), UserId = TestUtil.MakeTestGuid(userIdArg) };
                #endregion

                #region Act
                var view = await controller.AssignUserToClient(viewModel);
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Verify a BadRequestObjectResult is returned when the user or client is not found
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", -1, 1)] // User exists, but client does not
        [InlineData("ClientAdmin1", 1, -1)] // Client exists, but user does not (User is authorized to specified client & its profit center)
        public async Task AssignUserToClient_ErrorWhenNotFound(string userArg, int clientIdArg, int userIdArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, userArg);
                ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = TestUtil.MakeTestGuid(clientIdArg), UserId = TestUtil.MakeTestGuid(userIdArg) };
                #endregion

                #region Act
                var view = await controller.AssignUserToClient(viewModel);
                #endregion

                #region Assert
                Assert.IsType<BadRequestObjectResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Verify that no data is changed when adding a user to a client they are already assigned to
        /// </summary>
        [Fact]
        public async Task AssignUserToClient_NoActionWhenAssigned()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(1) };

                // Count users assigned to the client before attempting change
                int preActionCount = TestResources.DbContext.UserClaims.Where(c => c.ClaimValue == viewModel.ClientId.ToString()).Count();
                #endregion

                #region Act
                var view = await controller.AssignUserToClient(viewModel);

                // Capture the number of users assigned to the client after the call to AssignUserToClient
                int afterActionCount = TestResources.DbContext.UserClaims.Where(c => c.ClaimValue == viewModel.ClientId.ToString()).Count();
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<ClientDetailViewModel>(result.Value);
                Assert.Equal(preActionCount, afterActionCount);
                #endregion
            }
        }

        /// <summary>
        /// Verify that Status Code 422 is returned when the requested user's email address is not valid for the selected client
        /// </summary>
        [Fact]
        public async Task AssignUserToClient_ErrorForInvalidEmail()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = TestUtil.MakeTestGuid(5), UserId = TestUtil.MakeTestGuid(1) };
                #endregion

                #region Act
                var view = await controller.AssignUserToClient(viewModel);
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, (view as StatusCodeResult).StatusCode);
                #endregion
            }
        }

        /// <summary>
        /// Validate that the user is assigned to the client correctly when a valid request is made
        /// </summary>
        [Fact]
        public async Task AssignUserToClient_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = TestUtil.MakeTestGuid(5), UserId = TestUtil.MakeTestGuid(4) };

                // Before acting on the input data, we need to gather initial data to compare the result to
                int beforeCount = TestResources.DbContext.UserClaims.Where(c => c.ClaimValue == viewModel.ClientId.ToString()).Count();
                #endregion

                #region Act
                var view = await controller.AssignUserToClient(viewModel);

                // Capture the number of users assigned to the client after the call to AssignUserToClient
                int afterActionCount = TestResources.DbContext.UserClaims.Where(c => c.ClaimValue == viewModel.ClientId.ToString()).Count();
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<ClientDetailViewModel>(result.Value);
                Assert.Equal(beforeCount + 1, afterActionCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(-5, "ClientAdmin1")]  // client doesn't exist
        [InlineData(1, "test2")]
        [InlineData(2, "ClientAdmin1")]
        public async Task SetUserRoleInClient_ErrorWhenUnauthorized(int clientId, string userName)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, userName);
                var requestModel = new SetUserRoleInClientRequestModel
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                    UserId = TestUtil.MakeTestGuid(1),
                    RoleEnum = RoleEnum.Admin,
                    IsAssigned = true,
                };
                #endregion

                #region Act
                int preCount = TestResources.DbContext.UserRoleInClient.Count();
                var view = await controller.SetUserRoleInClient(requestModel);
                int postCount = TestResources.DbContext.UserRoleInClient.Count();
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(1, 2)]
        public async Task SetUserRoleInClient_ErrorWhenInvalid(int clientId, int userId)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                var requestModel = new SetUserRoleInClientRequestModel
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                    UserId = TestUtil.MakeTestGuid(userId),
                    RoleEnum = RoleEnum.Admin,
                    IsAssigned = true,
                };
                #endregion

                #region Act
                int preCount = TestResources.DbContext.UserRoleInClient.Count();
                var view = await controller.SetUserRoleInClient(requestModel);
                int postCount = TestResources.DbContext.UserRoleInClient.Count();
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, (view as StatusCodeResult).StatusCode);
                Assert.Equal(preCount, postCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(1, 5, RoleEnum.ContentUser)]
        public async Task SetUserRoleInClient_Success(int clientId, int userId, RoleEnum role)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                var requestModelAdd = new SetUserRoleInClientRequestModel
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                    UserId = TestUtil.MakeTestGuid(userId),
                    RoleEnum = role,
                    IsAssigned = true,
                };
                var requestModelRemove = new SetUserRoleInClientRequestModel
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                    UserId = TestUtil.MakeTestGuid(userId),
                    RoleEnum = role,
                    IsAssigned = false,
                };
                #endregion

                #region Act
                int preAddCount = TestResources.DbContext.UserRoleInClient.Count();
                var viewAdd = await controller.SetUserRoleInClient(requestModelAdd);
                int postAddCount = TestResources.DbContext.UserRoleInClient.Count();

                int preRemoveCount = postAddCount;
                var viewRemove = await controller.SetUserRoleInClient(requestModelRemove);
                int postRemoveCount = TestResources.DbContext.UserRoleInClient.Count();
                #endregion

                #region Assert
                var addResult = Assert.IsType<JsonResult>(viewAdd);
                Assert.IsType<SetUserRoleInClientResponseModel>(addResult.Value);
                var removeResult = Assert.IsType<JsonResult>(viewRemove);
                Assert.IsType<SetUserRoleInClientResponseModel>(removeResult.Value);
                Assert.Equal(preAddCount + 1, postAddCount);
                Assert.Equal(preRemoveCount - 1, postRemoveCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(1, 5, RoleEnum.Admin)]
        public async Task SetUserRoleInClient_Success_Pair(int clientId, int userId, RoleEnum role)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                var requestModelAdd = new SetUserRoleInClientRequestModel
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                    UserId = TestUtil.MakeTestGuid(userId),
                    RoleEnum = role,
                    IsAssigned = true,
                };
                var requestModelRemove = new SetUserRoleInClientRequestModel
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                    UserId = TestUtil.MakeTestGuid(userId),
                    RoleEnum = role,
                    IsAssigned = false,
                };
                #endregion

                #region Act
                int preAddCount = TestResources.DbContext.UserRoleInClient.Count();
                var viewAdd = await controller.SetUserRoleInClient(requestModelAdd);
                int postAddCount = TestResources.DbContext.UserRoleInClient.Count();

                int preRemoveCount = postAddCount;
                var viewRemove = await controller.SetUserRoleInClient(requestModelRemove);
                int postRemoveCount = TestResources.DbContext.UserRoleInClient.Count();
                #endregion

                #region Assert
                var addResult = Assert.IsType<JsonResult>(viewAdd);
                Assert.IsType<SetUserRoleInClientResponseModel>(addResult.Value);
                var removeResult = Assert.IsType<JsonResult>(viewRemove);
                Assert.IsType<SetUserRoleInClientResponseModel>(removeResult.Value);
                Assert.Equal(preAddCount + 2, postAddCount);
                Assert.Equal(preRemoveCount - 2, postRemoveCount);
                #endregion
            }
        }

        [Theory]
        [InlineData(1, 5, RoleEnum.ContentAccessAdmin)]
        [InlineData(1, 5, RoleEnum.ContentPublisher)]
        public async Task SetUserRoleInClient_Success_Content(int clientId, int userId, RoleEnum role)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                var requestModelAdd = new SetUserRoleInClientRequestModel
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                    UserId = TestUtil.MakeTestGuid(userId),
                    RoleEnum = role,
                    IsAssigned = true,
                };
                var requestModelRemove = new SetUserRoleInClientRequestModel
                {
                    ClientId = TestUtil.MakeTestGuid(clientId),
                    UserId = TestUtil.MakeTestGuid(userId),
                    RoleEnum = role,
                    IsAssigned = false,
                };

                int relatedRootContentItemCount = TestResources.DbContext.RootContentItem.Count(i => i.ClientId == TestUtil.MakeTestGuid(clientId));
                #endregion

                #region Act
                int preAddCount_Client = TestResources.DbContext.UserRoleInClient.Count();
                int preAddCount_Content = TestResources.DbContext.UserRoleInRootContentItem.Count();
                var viewAdd = await controller.SetUserRoleInClient(requestModelAdd);
                int postAddCount_Client = TestResources.DbContext.UserRoleInClient.Count();
                int postAddCount_Content = TestResources.DbContext.UserRoleInRootContentItem.Count();

                int preRemoveCount_Client = postAddCount_Client;
                int preRemoveCount_Content = postAddCount_Content;
                var viewRemove = await controller.SetUserRoleInClient(requestModelRemove);
                int postRemoveCount_Client = TestResources.DbContext.UserRoleInClient.Count();
                int postRemoveCount_Content = TestResources.DbContext.UserRoleInRootContentItem.Count();
                #endregion

                #region Assert
                var addResult = Assert.IsType<JsonResult>(viewAdd);
                Assert.IsType<SetUserRoleInClientResponseModel>(addResult.Value);
                var removeResult = Assert.IsType<JsonResult>(viewRemove);
                Assert.IsType<SetUserRoleInClientResponseModel>(removeResult.Value);
                Assert.Equal(preAddCount_Client + 1, postAddCount_Client);
                Assert.Equal(preRemoveCount_Client - 1, postRemoveCount_Client);
                Assert.Equal(preAddCount_Content + relatedRootContentItemCount, postAddCount_Content);
                Assert.Equal(preRemoveCount_Content - relatedRootContentItemCount, postRemoveCount_Content);
                #endregion
            }
        }

        /// <summary>
        /// Validate that an UnauthorizedResult is returned if the user is not authorized to remove users from the requested client
        /// Multiple authorizations are checked, so multiple scenarios should be tested
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", 3, 1)] // User isn't admin on the requested client
        public async Task RemoveUserFromClient_ErrorWhenUnauthorized(string userArg, int clientIdArg, int userIdArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, userArg);
                ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = TestUtil.MakeTestGuid(clientIdArg), UserId = TestUtil.MakeTestGuid(userIdArg) };
                #endregion

                #region Act
                var view = await controller.RemoveUserFromClient(viewModel);
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Validate that a BadRequestResult is returned if the client, user, or both do not exist
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", -1, 1)] // User exists, but client does not
        [InlineData("ClientAdmin1", 1, -1)] // Client exists, but user does not (User is authorized to specified client & its profit center)
        public async Task RemoveUserFromClient_ErrorWhenNotFound(string userArg, int clientIdArg, int userIdArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, userArg);
                ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = TestUtil.MakeTestGuid(clientIdArg), UserId = TestUtil.MakeTestGuid(userIdArg) };
                #endregion

                #region Act
                var view = await controller.RemoveUserFromClient(viewModel);
                #endregion

                #region Assert
                Assert.IsType<BadRequestObjectResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Validate that a user is removed from a client when a request is made by an authorized user for a valid user & client
        /// Checks to make sure claims & roles are both removed
        /// </summary>
        [Fact]
        public async Task RemoveUserFromClient_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = TestUtil.MakeTestGuid(5), UserId = TestUtil.MakeTestGuid(2) };

                int preActionCount = TestResources.DbContext.UserClaims.Where(c => c.ClaimValue == viewModel.ClientId.ToString() && c.UserId == viewModel.UserId).Count();
                #endregion

                #region Act
                var view = await controller.RemoveUserFromClient(viewModel);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<ClientDetailViewModel>(result.Value);

                // Capture the number of users assigned to the client after the call to RemoveUserFromClient
                int afterActionCount = TestResources.DbContext.UserClaims.Where(c => c.ClaimValue == viewModel.ClientId.ToString() && c.UserId == viewModel.UserId).Count();
                Assert.Equal(preActionCount - 1, afterActionCount);

                // Ensure that the user no longer has roles on the client they were removed from
                int userRoleCountInClient =
                        TestResources.DbContext.UserRoleInClient.Where(ur =>
                            ur.ClientId == viewModel.ClientId &&
                            ur.UserId == viewModel.UserId).Count();
                Assert.Equal(0, userRoleCountInClient);
                #endregion
            }
        }

        /// <summary>
        /// Validate that trying to save a client with itself as the test client results in a bad request
        /// </summary>
        [Fact]
        public async Task SaveNewClient_ErrorWhenParentIdIsClientId()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                Client testClient = GetValidClient();
                #endregion

                #region Act
                testClient.ParentClientId = testClient.Id;
                var view = await controller.SaveNewClient(testClient);
                #endregion

                #region Assert
                Assert.IsType<BadRequestObjectResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Validate that a bad request is returned when ModelState.IsValid returns false
        /// </summary>
        [Fact]
        public async Task SaveNewClient_ErrorWhenModelStateInvalid()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                Client testClient = GetValidClient();
                #endregion

                #region Act
                controller.ModelState.AddModelError("BadModel", "This is a forced bad model.");
                var view = await controller.SaveNewClient(testClient);
                #endregion

                #region Assert
                Assert.IsType<BadRequestResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Validate that an UnauthorizedResult is returned for unauthorized users
        /// Multiple authorization checks are made and must be tested
        /// </summary>
        [Theory]
        [InlineData("test1", null, 1)]// Request new root client; user is not an admin of requested profit center
        [InlineData("ClientAdmin1", 4, 2)]// Request new child client; user is admin of parent client but not profit center
        [InlineData("ClientAdmin1", 3, 1)]// Request new child client; user is admin of profit center but not parent client
        public async Task SaveNewClient_ErrorWhenNotAuthorized(string userArg, int? parentClientIdArg, int profitCenterIdArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, userArg);
                Client testClient = GetValidClient();
                #endregion

                #region Act
                testClient.ParentClientId = parentClientIdArg.HasValue ? TestUtil.MakeTestGuid(parentClientIdArg.Value) : (Guid?)null;
                testClient.ProfitCenterId = TestUtil.MakeTestGuid(profitCenterIdArg);
                var view = await controller.SaveNewClient(testClient);
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Validate that status code 422 is returned when invalid email domains or addresses are provided
        /// </summary>
        [Theory]
        [InlineData(new string[] {"test"}, null)] // invalid domain (no @)
        [InlineData(null, new string[] { "user@test" })] // invalid email address format (no TLD)
        [InlineData(null, new string[] { "test.com" })] // invalid email address format (no user, no @)
        [InlineData(null, new string[] { "@test.com" })] // invalid email address format (no user before @)
        public async Task SaveNewClient_ErrorWhenEmailInvalid(string[] domainListArg, string[] emailListArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                Client testClient = GetValidClient();
                #endregion

                #region Act
                testClient.ParentClientId = null;
                if (domainListArg != null)
                {
                    testClient.AcceptedEmailDomainList = domainListArg.ToList();
                }
                if (emailListArg != null)
                {
                    testClient.AcceptedEmailAddressExceptionList = emailListArg.ToList();
                }
                var view = await controller.SaveNewClient(testClient);
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, (view as StatusCodeResult).StatusCode);
                #endregion
            }
        }

        /// <summary>
        /// Validate that status code 422 is returned when excessive domains are requested
        /// </summary>
        [Fact]
        public async Task SaveNewClient_ErrorWhenDomainLimitExceeded()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                Client testClient = GetValidClient();
                testClient.ParentClientId = null;
                testClient.AcceptedEmailDomainList = new List<string> { "test1.com", "test2.com", "test3.com", "test4.com" };
                #endregion

                #region Act
                var view = await controller.SaveNewClient(testClient);
                #endregion

                #region Assert
                StatusCodeResult result = Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, result.StatusCode);
                #endregion
            }
        }

        /// <summary>
        /// Validate that new clients are added successfully when the model is valid and the user is authorized
        /// </summary>
        [Fact]
        public async Task SaveNewClient_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                Client testClient = GetValidClient();

                int beforeCount = TestResources.DbContext.Client.Count();
                int expectedAfterCount = beforeCount + 1;
                #endregion

                #region Act
                testClient.ParentClientId = null;
                testClient.ProfitCenterId = TestUtil.MakeTestGuid(1);
                var view = await controller.SaveNewClient(testClient);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<SaveNewClientResponseModel>(result.Value);

                int afterCount = TestResources.DbContext.Client.Count();
                Assert.Equal<int>(expectedAfterCount, afterCount);
                #endregion
            }
        }

        /// <summary>
        /// Validate that an invalid request results in a BadRequestResult
        /// </summary>
        [Theory]
        [InlineData(-1,1)]// Client ID less than 0
        [InlineData(1,1)]// Parent client ID matches client ID
        [InlineData(424242,1)]// Attempt to edit a non-existent client
        public async Task EditClient_ErrorWhenInvalidRequest(int clientIdArg, int parentClientIdArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                Client testClient = GetValidClient();
                #endregion

                #region Act
                testClient.ParentClientId = TestUtil.MakeTestGuid(parentClientIdArg);
                testClient.Id = TestUtil.MakeTestGuid(clientIdArg);
                var view = await controller.EditClient(testClient);
                #endregion

                #region Assert
                Assert.IsType<BadRequestObjectResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Validate that unauthorized users receive an UnauthorizedResult
        /// Multiple authorizations are checked and must be tested
        /// </summary>
        [Theory]
        [InlineData(2, 1)] // User is not an admin on the edited client
        [InlineData(5, 2)] // User is not an admin on the new profit center
        public async Task EditClient_ErrorWhenUnauthorized(int clientIdArg, int profitCenterIdArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                Client testClient = GetValidClient();
                #endregion

                #region Act
                /*
                 * Requirements/Assumptions for the test client:
                 *       The test user must be a client admin
                 *       The parent client must not be null
                 *       The parent client specified must be the current parent of the test client
                 */
                testClient.Id = TestUtil.MakeTestGuid(clientIdArg);
                // Ensure we're passing the current parent client, whatever it is
                testClient.ParentClientId = TestResources.DbContext.Client.Single(c => c.Id == TestUtil.MakeTestGuid(clientIdArg)).ParentClientId;
                testClient.ProfitCenterId = TestUtil.MakeTestGuid(profitCenterIdArg);

                var view = await controller.EditClient(testClient);
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Validate that EditClient call with domain list exceeding the limit causes 422 error
        /// </summary>
        [Fact]
        public async Task EditClient_ErrorWhenDomainLimitExceeded()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                Client testClient = GetValidClient();
                testClient.AcceptedEmailDomainList = new List<string> { "test1.com", "test2.com", "test3.com", "test4.com" };
                testClient.Id = TestUtil.MakeTestGuid(1);
                testClient.ParentClientId = TestResources.DbContext.Client.Find(testClient.Id).ParentClientId;
                testClient.ProfitCenterId = TestResources.DbContext.Client.Find(testClient.Id).ProfitCenterId;
                #endregion

                #region Act
                var view = await controller.EditClient(testClient);
                #endregion

                #region Assert
                StatusCodeResult result = Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, result.StatusCode);
                #endregion
            }
        }

        /// <summary>
        /// Validate that changing the parent client is not supported
        /// </summary>
        [Fact]
        public async Task EditClient_UnauthorizedWhenChangingParentClient()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                Client testClient = GetValidClient();
                #endregion

                #region Act
                /*
                 * Requirements/Assumptions for the test client:
                 *       The test user must be a client admin
                 *       The parent client must not be null
                 *       The parent client specified must be changed from the client's current parent
                 */
                testClient.Id = TestUtil.MakeTestGuid(6);
                testClient.ParentClientId = TestUtil.MakeTestGuid(2); // Original value was 1

                var view = await controller.EditClient(testClient);
                #endregion

                #region Assert
                Assert.IsType<UnauthorizedResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// Validate that invalid data causes return code 422
        /// Multiple scenarios should cause code 422 and must be tested
        /// 
        /// Providing a null value for an argument will retain the current value for the corresponding property
        /// </summary>
        [Theory]
        [InlineData("Name1", null, null)]// Client name already exists for other client
        [InlineData(null, new string[] { "test" }, null)]// Email domain whitelist invalid (no TLD)
        [InlineData(null, null, new string[] { "test" })] // Email address whitelist invalid (no @, no tld)
        [InlineData(null, null, new string[] { "test.com" })] // Email address whitelist invalid (no @)
        [InlineData(null, null, new string[] { "@test.com" })] // Email address whitelist invalid (no user)
        public async Task EditClient_ErrorWhenInvalid(string clientNameArg, string[] domainWhitelistArg, string[] addressWhitelistArg)
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                Client testClient = GetValidClient();
                #endregion

                #region Act
                /*
                 * Requirements/Assumptions for the test client:
                 *       The test user must be a client admin
                 *       The parent client must not be null
                 *       The parent client specified must be the current parent of the test client
                 */
                testClient.Id = TestUtil.MakeTestGuid(6);
                testClient.ParentClientId = TestUtil.MakeTestGuid(1);

                #region Manipulate data for test scenarios
                if (!String.IsNullOrEmpty(clientNameArg))
                {
                    testClient.Name = clientNameArg;
                }

                if (domainWhitelistArg != null)
                {
                    testClient.AcceptedEmailDomainList = domainWhitelistArg.ToList();
                }

                if (addressWhitelistArg != null)
                {
                    testClient.AcceptedEmailAddressExceptionList = addressWhitelistArg.ToList();
                }
                #endregion

                var view = await controller.EditClient(testClient);
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, (view as StatusCodeResult).StatusCode);
                #endregion
            }
        }

        /// <summary>
        /// Validate that client edits are made and saved successfully when the model is valid and the user is authorized
        /// </summary>
        [Fact]
        public async Task EditClient_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                Client testClient = GetValidClient();
                #endregion

                #region Act
                /*
                 * Requirements/Assumptions for the test client:
                 *       The test user must be a client admin
                 *       The parent client must not be null
                 *       The parent client specified must be the current parent of the test client
                 */
                testClient.Id = TestUtil.MakeTestGuid(6);
                testClient.ParentClientId = TestUtil.MakeTestGuid(1);

                // Change some data that can be validated after the edit
                #region Manipulate model data
                testClient.Name = "Edit Client Name";
                testClient.ClientCode = "Edit client code";
                testClient.ContactName = "Edit contact name";
                testClient.ContactEmail = "edit@example.com";
                testClient.ContactPhone = "0987654321";
                testClient.ContactTitle = "Edit contact title";
                testClient.ConsultantEmail = "editconsultant@example2.com";
                testClient.ConsultantName = "Edit consultant name";
                testClient.ConsultantOffice = "Edit consultant office";
                testClient.AcceptedEmailAddressExceptionList = new List<string> { "edit1@example.com,edit2@example.com", "edit3@example.com" };
                testClient.AcceptedEmailDomainList = new List<string> { "editexample.com", "example2.com" };
                #endregion

                var view = await controller.EditClient(testClient);
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<ClientsResponseModel>(result.Value);

                #region Check that all updated data now matches
                Client resultClient = TestResources.DbContext.Client.Single(c => c.Id == testClient.Id);

                Assert.Equal(testClient.Name, resultClient.Name);
                Assert.Equal(testClient.ClientCode, resultClient.ClientCode);
                Assert.Equal(testClient.ContactName, resultClient.ContactName);
                Assert.Equal(testClient.ContactEmail, resultClient.ContactEmail);
                Assert.Equal(testClient.ContactPhone, resultClient.ContactPhone);
                Assert.Equal(testClient.ContactTitle, resultClient.ContactTitle);
                Assert.Equal(testClient.ConsultantEmail, resultClient.ConsultantEmail);
                Assert.Equal(testClient.ConsultantName, resultClient.ConsultantName);
                Assert.Equal(testClient.ConsultantOffice, resultClient.ConsultantOffice);
                Assert.Equal(testClient.AcceptedEmailAddressExceptionList, resultClient.AcceptedEmailAddressExceptionList);
                Assert.Equal(testClient.AcceptedEmailDomainList, resultClient.AcceptedEmailDomainList);
                #endregion
                #endregion
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task DeleteClient_ErrorWhenClientNotFound()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                #endregion

                #region Act
                var view = await controller.DeleteClient(TestUtil.MakeTestGuid(424242));
                #endregion

                #region Assert
                Assert.IsType<BadRequestResult>(view);
                #endregion
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task DeleteClient_ErrorWhenClientHasChildren()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                ApplicationUser AppUser = await TestResources.UserManager.FindByNameAsync("ClientAdmin1");
                #endregion

                #region Act
                var view = await controller.DeleteClient(TestUtil.MakeTestGuid(7));
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, (view as StatusCodeResult).StatusCode);
                #endregion
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task DeleteClient_ErrorWhenClientHasRootContentItems()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                ApplicationUser AppUser = await TestResources.UserManager.FindByNameAsync("ClientAdmin1");
                #endregion

                #region Act
                var view = await controller.DeleteClient(TestUtil.MakeTestGuid(8));
                #endregion

                #region Assert
                Assert.IsType<StatusCodeResult>(view);
                Assert.Equal(422, (view as StatusCodeResult).StatusCode);
                #endregion
            }
        }

        [Fact]
        public async Task DeleteClient_ErrorWhenClientHasFileDrops()
        {
          using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
          {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
            ApplicationUser AppUser = await TestResources.UserManager.FindByNameAsync("ClientAdmin1");
            #endregion

            #region Act
            var view = await controller.DeleteClient(TestUtil.MakeTestGuid(9));
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            Assert.Equal(422, (view as StatusCodeResult).StatusCode);
            #endregion
          }
        }

    /// <summary>
    /// Verify that a deleted client is removed from persistence
    /// </summary>
    [Fact]
        public async Task DeleteClient_Success()
        {
            using (var TestResources = await TestInitialization.Create(_dbLifeTimeFixture, DataSelection.Basic))
            {
                #region Arrange
                ClientAdminController controller = await GetControllerForUser(TestResources, "ClientAdmin1");
                ApplicationUser AppUser = await TestResources.UserManager.FindByNameAsync("ClientAdmin1");

                int clientPreCount = TestResources.DbContext.Client.Count();
                int claimsPreCount = TestResources.DbContext.UserClaims.Count();
                #endregion

                #region Act
                var view = await controller.DeleteClient(TestUtil.MakeTestGuid(6));
                #endregion

                #region Assert
                JsonResult result = Assert.IsType<JsonResult>(view);
                Assert.IsType<ClientsResponseModel>(result.Value);

                int clientPostCount = TestResources.DbContext.Client.Count();
                int claimsPostCount = TestResources.DbContext.UserClaims.Count();
                Assert.Equal<int>((clientPreCount - 1), clientPostCount);
                Assert.Equal<int>((claimsPreCount - 1), claimsPostCount);
                #endregion
            }
        }

    }
}
