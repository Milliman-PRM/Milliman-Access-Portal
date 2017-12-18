using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.ClientAdminViewModels;
using System.Linq;
using MapDbContextLib.Context;

namespace MapTests
{
    public class ClientAdminControllerTests
    {
        internal TestInitialization TestResources { get; set; }

        /// <summary>
        /// Constructor is called for each test execution
        /// </summary>
        public ClientAdminControllerTests()
        {
            TestResources = new TestInitialization();
            TestResources.GenerateTestData(new DataSelection[] { DataSelection.Basic });
        }

        /// <summary>
        /// Common controller constructor to be used by all tests
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public async Task<ClientAdminController> GetControllerForUser(string UserName)
        {
            ClientAdminController testController = new ClientAdminController(TestResources.DbContextObject,
                TestResources.UserManagerObject,
                TestResources.QueriesObj,
                TestResources.AuthorizationService,
                TestResources.LoggerFactory,
                TestResources.AuditLogger,
                TestResources.RoleManagerObject);

            // Generating ControllerContext will throw a NullReferenceException if the provided user does not exist
            testController.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: (await TestResources.UserManagerObject.FindByNameAsync(UserName)).UserName);
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
                Id = 0,
                Name = "Placeholder Test Client",
                ClientCode = "Test Client 0001",
                ContactName = "Contact person",
                ContactTitle = "Manager",
                ContactEmail = "manager@placeholder.com",
                ContactPhone = "1234567890",
                ConsultantEmail = "consultant@example.com",
                ConsultantName = "Test Consultant",
                ConsultantOffice = "Indy PRM Testing",
                AcceptedEmailAddressExceptionList = new string[] { },
                AcceptedEmailDomainList = new string[] { "placeholder.com" },
                ParentClientId = 2,
                ProfitCenterId = 1
            };
        }

        /// <summary>
        /// Checks whether the Index action returns an UnauthorizedResult when the user is not authorized to view the ClientAdmin page
        /// </summary>
        [Fact]
        public async Task Index_ErrorWhenUnauthorized()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("test1");
            #endregion

            #region Act
            var view = await controller.Index();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        /// <summary>
        /// Checks whether the Index returns a view for authorized users
        /// </summary>
        [Fact]
        public async Task Index_ReturnsAView()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = await controller.Index();
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            #endregion
        }

        /// <summary>
        /// Checks whether ClientFamilyList returns an error when the user is not authorized to view the ClientAdmin page
        /// </summary>
        [Fact]
        public async Task ClientFamilyList_ErrorWhenUnauthorized()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("test1");
            #endregion

            #region Act
            var view = await controller.ClientFamilyList();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        /// <summary>
        /// Checks whether ClientFamilyList returns a list of clients for authorized users
        /// </summary>
        [Fact]
        public async Task ClientFamilyList_ReturnsAList()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = await controller.ClientFamilyList();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        /// <summary>
        /// Checks whether ClientDetail returns an error when the client is not found
        /// </summary>
        [Fact]
        public async Task ClientDetail_ErrorWhenNotFound()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = await controller.ClientDetail(-100);
            #endregion

            #region Assert
            Assert.IsType<NotFoundResult>(view);
            #endregion
        }

        /// <summary>
        /// Checks whether ClientDetail returns an error when the user is not authorized to view the ClientAdmin page or to admin a related client
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", 3)] // Authorized to client admin, but not the specified client
        [InlineData("test1", 1)] // Not authorized to perform client admin
        public async Task ClientDetail_ErrorWhenUnauthorized(string userArg, long clientIdArg)
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser(userArg);
            #endregion

            #region Act
            var view = await controller.ClientDetail(clientIdArg);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        /// <summary>
        /// Checks whether ClientDetail returns the ClientDetail Json model to authorized users
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", 1)] // Directly authorized to this client
        [InlineData("ClientAdmin1", 2)] // Authorized to a related (parent) client
        public async Task ClientDetail_ReturnsDetails(string userArg, long clientIdArg)
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser(userArg);
            #endregion

            #region Act
            var view = await controller.ClientDetail(clientIdArg);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        /// <summary>
        /// Checks whether AssignUserToClient returns an error for unauthorized users
        /// Multiple authorization checks are made, so multiple users should be tested w/ various rights
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", 3, "test1")] // User isn't admin on the requested client but is admin on the requested client's profit center
        [InlineData("ClientAdmin1", 4, "test1")] // User is admin on the requested client but isn't admin on the requested client's profit center
        public async Task AssignUserToClient_ErrorWhenUnauthorized(string userArg, long clientIdArg, string userAssignArg)
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser(userArg);
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = clientIdArg, UserName = userAssignArg };
            #endregion

            #region Act
            var view = await controller.AssignUserToClient(viewModel);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        /// <summary>
        /// Verify a BadRequestObjectResult is returned when the user or client is not found
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", -1, "test1")] // User exists, but client does not
        [InlineData("ClientAdmin1", 1, "__fake1")] // Client exists, but user does not (User is authorized to specified client & its profit center)
        public async Task AssignUserToClient_ErrorWhenNotFound(string userArg, long clientIdArg, string userAssignArg)
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser(userArg);
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = clientIdArg, UserName = userAssignArg };
            #endregion

            #region Act
            var view = await controller.AssignUserToClient(viewModel);
            #endregion

            #region Assert
            Assert.IsType<BadRequestObjectResult>(view);
            #endregion
        }

        /// <summary>
        /// Verify that no data is changed when adding a user to a client they are already assigned to
        /// </summary>
        [Fact]
        public async Task AssignUserToClient_NoActionWhenAssigned()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = 1, UserName = "test1" };

            // Count users assigned to the client before attempting change
            int preActionCount = Enumerable.Count(TestResources.DbContextObject.UserClaims.Where(c => c.ClaimValue == viewModel.ClientId.ToString()));
            #endregion

            #region Act
            var view = await controller.AssignUserToClient(viewModel);

            // Capture the number of users assigned to the client after the call to AssignUserToClient
            int afterActionCount = Enumerable.Count(TestResources.DbContextObject.UserClaims.Where(c => c.ClaimValue == viewModel.ClientId.ToString()));
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            Assert.Equal(preActionCount, afterActionCount);
            #endregion
        }

        /// <summary>
        /// Verify that Status Code 412 is returned when the requested user's email address is not valid for the selected client
        /// </summary>
        [Fact]
        public async Task AssignUserToClient_ErrorForInvalidEmail()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = 5, UserName = "test1" };
            #endregion

            #region Act
            var view = await controller.AssignUserToClient(viewModel);
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);
            StatusCodeResult viewResult = (StatusCodeResult)view;
            Assert.Equal("412", viewResult.StatusCode.ToString());
            #endregion
        }

        /// <summary>
        /// Validate that the user is assigned to the client correctly when a valid request is made
        /// </summary>
        [Fact]
        public async Task AssignUserToClient_Success()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = 5, UserName = "test3" };

            // Before acting on the input data, we need to gather initial data to compare the result to
            int beforeCount = Enumerable.Count(TestResources.DbContextObject.UserClaims.Where(c => c.ClaimValue == viewModel.ClientId.ToString()));
            #endregion

            #region Act
            var view = await controller.AssignUserToClient(viewModel);

            // Capture the number of users assigned to the client after the call to AssignUserToClient
            int afterActionCount = Enumerable.Count(TestResources.DbContextObject.UserClaims.Where(c => c.ClaimValue == viewModel.ClientId.ToString()));
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            Assert.Equal(beforeCount + 1, afterActionCount);
            #endregion
        }

        /// <summary>
        /// Validate that an UnauthorizedResult is returned if the user is not authorized to remove users from the requested client
        /// Multiple authorizations are checked, so multiple scenarios should be tested
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", 3, "test1")] // User isn't admin on the requested client but is admin on the requested client's profit center
        [InlineData("ClientAdmin1", 4, "test1")] // User is admin on the requested client but isn't admin on the requested client's profit center
        public async Task RemoveUserFromClient_ErrorWhenUnauthorized(string userArg, long clientIdArg, string userAssignArg)
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser(userArg);
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = clientIdArg, UserName = userAssignArg };
            #endregion

            #region Act
            var view = await controller.RemoveUserFromClient(viewModel);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        /// <summary>
        /// Validate that a BadRequestResult is returned if the client, user, or both do not exist
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", -1, "test1")] // User exists, but client does not
        [InlineData("ClientAdmin1", 1, "__fake1")] // Client exists, but user does not (User is authorized to specified client & its profit center)
        public async Task RemoveUserFromClient_ErrorWhenNotFound(string userArg, long clientIdArg, string userAssignArg)
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser(userArg);
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = clientIdArg, UserName = userAssignArg };
            #endregion

            #region Act
            var view = await controller.RemoveUserFromClient(viewModel);
            #endregion

            #region Assert
            Assert.IsType<BadRequestObjectResult>(view);
            #endregion
        }

        /// <summary>
        /// Validate that a user is removed from a client when a request is made by an authorized user for a valid user & client
        /// 
        /// Checks to make sure claims & roles are both removed
        /// </summary>
        [Fact]
        public async Task RemoveUserFromClient_Success()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = 5, UserName = "test2" };

            int preActionCount = Enumerable.Count(TestResources.DbContextObject.UserClaims.Where(c => c.ClaimValue == viewModel.ClientId.ToString()));
            #endregion

            #region Act
            var view = await controller.RemoveUserFromClient(viewModel);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);

            // Capture the number of users assigned to the client after the call to RemoveUserFromClient
            int afterActionCount = Enumerable.Count(TestResources.DbContextObject.UserClaims.Where(c => c.ClaimValue == viewModel.ClientId.ToString()));
            Assert.Equal(preActionCount - 1, afterActionCount);

            // Ensure that the user no longer has roles on the client they were removed from
            int userRoleCountInClient = 
                    Enumerable.Count(TestResources.DbContextObject.UserRoleInClient.Where(ur => 
                        ur.ClientId == viewModel.ClientId && 
                        ur.UserId == TestResources.UserManagerObject.FindByNameAsync(viewModel.UserName).Id));
            Assert.Equal(0, userRoleCountInClient);
            #endregion
        }

        /// <summary>
        /// Validate that trying to save a client with itself as the test client results in a bad request
        /// </summary>
        [Fact]
        public async Task SaveNewClient_ErrorWhenParentIdIsClientId()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
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

        /// <summary>
        /// Validate that a bad request is returned when ModelState.IsValid returns false
        /// </summary>
        [Fact]
        public async Task SaveNewClient_ErrorWhenModelStateInvalid()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            Client testClient = GetValidClient();
            #endregion

            #region Act
            controller.ModelState.AddModelError("BadModel", "This is a forced bad model.");
            var view = await controller.SaveNewClient(testClient);
            #endregion

            #region Assert
            Assert.IsType<BadRequestObjectResult>(view);
            #endregion
        }

        /// <summary>
        /// Validate that an UnauthorizedResult is returned for unauthorized users
        /// Multiple authorization checks are made and must be tested
        /// </summary>
        [Theory]
        [InlineData("test1", null, 1)]// Request new root client; user is not an admin of requested profit center
        [InlineData("ClientAdmin1", 4, 2)]// Request new child client; user is admin of parent client but not profit center
        [InlineData("ClientAdmin1", 3, 1)]// Request new child client; user is admin of profit center but not parent client
        public async Task SaveNewClient_ErrorWhenNotAuthorized(string userArg, long? parentClientIdArg, long? profitCenterIdArg)
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser(userArg);
            Client testClient = GetValidClient();
            #endregion

            #region Act
            testClient.ParentClientId = parentClientIdArg;
            testClient.ProfitCenterId = (long)profitCenterIdArg;
            var view = await controller.SaveNewClient(testClient);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        /// <summary>
        /// Validate that status code 412 is returned when invalid email domains or addresses are provided
        /// </summary>
        [Theory]
        [InlineData(new string[] {"test"}, null)] // invalid domain (no @)
        [InlineData(null, new string[] { "user@test" })] // invalid email address format (no TLD)
        [InlineData(null, new string[] { "test.com" })] // invalid email address format (no user, no @)
        [InlineData(null, new string[] { "@test.com" })] // invalid email address format (no user before @)
        public async Task SaveNewClient_ErrorWhenEmailInvalid(string[] domainListArg, string[] emailListArg)
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            Client testClient = GetValidClient();
            #endregion

            #region Act
            testClient.ParentClientId = null;
            if (domainListArg != null)
            {
                testClient.AcceptedEmailDomainList = domainListArg;
            }
            if (emailListArg != null)
            {
                testClient.AcceptedEmailAddressExceptionList = emailListArg;
            }
            var view = await controller.SaveNewClient(testClient);
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);

            StatusCodeResult viewResult = (StatusCodeResult)view;
            Assert.Equal<int>(412, viewResult.StatusCode);

            #endregion
        }

        /// <summary>
        /// Validate that new clients are added successfully when the model is valid and the user is authorized
        /// </summary>
        [Fact]
        public async Task SaveNewClient_Success()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            Client testClient = GetValidClient();

            int beforeCount = Enumerable.Count(TestResources.DbContextObject.Client);
            int expectedAfterCount = beforeCount + 1;
            #endregion

            #region Act
            testClient.ParentClientId = null;
            testClient.ProfitCenterId = 1;
            var view = await controller.SaveNewClient(testClient);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);

            int afterCount = Enumerable.Count(TestResources.DbContextObject.Client);
            Assert.Equal<int>(expectedAfterCount, afterCount);
            #endregion
        }

        /// <summary>
        /// Validate that an invalid request results in a BadRequestResult
        /// </summary>
        [Theory]
        [InlineData(-1,1)]// Client ID less than 0
        [InlineData(1,1)]// Parent client ID matches client ID
        [InlineData(424242,1)]// Attempt to edit a non-existent client
        public async Task EditClient_ErrorWhenInvalidRequest(long clientIdArg, long parentClientIdArg)
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            Client testClient = GetValidClient();
            #endregion

            #region Act
            testClient.ParentClientId = parentClientIdArg;
            testClient.Id = clientIdArg;
            var view = await controller.EditClient(testClient);
            #endregion

            #region Assert
            Assert.IsType<BadRequestObjectResult>(view);
            #endregion
        }

        /// <summary>
        /// Validate that unauthorized users receive an UnauthorizedResult
        /// Multiple authorizations are checked and must be tested
        /// </summary>
        [Theory]
        [InlineData(2, 1)] // User is not an admin on the edited client
        [InlineData(5, 2)] // User is not an admin on the new profit center
        public async Task EditClient_ErrorWhenUnauthorized(long clientIdArg, long profitCenterIdArg)
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            Client testClient = GetValidClient();
            #endregion

            #region Act
            /*
             * Requirements/Assumptions for the test client:
             *       The test user must be a client admin
             *       The parent client must not be null
             *       The parent client specified must be the current parent of the test client
             */
            testClient.Id = clientIdArg;
            // Ensure we're passing the current parent client, whatever it is
            testClient.ParentClientId = TestResources.DbContextObject.Client.Single(c => c.Id == clientIdArg).ParentClientId;
            testClient.ProfitCenterId = profitCenterIdArg;

            var view = await controller.EditClient(testClient);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        /// <summary>
        /// Validate that changing the parent client is not supported
        /// </summary>
        [Fact]
        public async Task EditClient_UnauthorizedWhenChangingParentClient()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            Client testClient = GetValidClient();
            #endregion

            #region Act
            /*
             * Requirements/Assumptions for the test client:
             *       The test user must be a client admin
             *       The parent client must not be null
             *       The parent client specified must be changed from the client's current parent
             */
            testClient.Id = 6;
            testClient.ParentClientId = 2; // Original value was 1
            
            var view = await controller.EditClient(testClient);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        /// <summary>
        /// Validate that invalid data causes return code 412
        /// Multiple scenarios should cause code 412 and must be tested
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
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            Client testClient = GetValidClient();
            #endregion

            #region Act
            /*
             * Requirements/Assumptions for the test client:
             *       The test user must be a client admin
             *       The parent client must not be null
             *       The parent client specified must be the current parent of the test client
             */
            testClient.Id = 6;
            testClient.ParentClientId = 1;

            #region Manipulate data for test scenarios
            if (!String.IsNullOrEmpty(clientNameArg))
            {
                testClient.Name = clientNameArg;
            }

            if (domainWhitelistArg != null)
            {
                testClient.AcceptedEmailDomainList = domainWhitelistArg;
            }

            if (addressWhitelistArg != null)
            {
                testClient.AcceptedEmailAddressExceptionList = addressWhitelistArg;
            }
            #endregion 

            var view = await controller.EditClient(testClient);
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);

            StatusCodeResult statusCodeResult = (StatusCodeResult)view;
            Assert.Equal<int>(412, statusCodeResult.StatusCode);
            #endregion
        }

        /// <summary>
        /// Validate that client edits are made and saved successfully when the model is valid and the user is authorized
        /// </summary>
        [Fact]
        public async Task EditClient_Success()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            Client testClient = GetValidClient();
            #endregion

            #region Act
            /*
             * Requirements/Assumptions for the test client:
             *       The test user must be a client admin
             *       The parent client must not be null
             *       The parent client specified must be the current parent of the test client
             */
            testClient.Id = 6;
            testClient.ParentClientId = 1;

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
            testClient.AcceptedEmailAddressExceptionList = new string[] { "edit1@example.com", "edit2@example.com" };
            testClient.AcceptedEmailDomainList = new string[] { "editexample.com" };
            #endregion 

            var view = await controller.EditClient(testClient);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);

            #region Check that all updated data now matches
            Client resultClient = TestResources.DbContextObject.Client.Single(c => c.Id == testClient.Id);

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

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task DeleteClient_ErrorWhenClientNotFound()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = await controller.DeleteClient(424242, "password");
            #endregion

            #region Assert
            Assert.IsType<BadRequestResult>(view);
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        [Theory]
        [InlineData(1, null)]// Password check fails
        [InlineData(2, "password")]// User is not authorized as Admin of the client
        [InlineData(4, "password")]// User is not authorized as Admin of the client's profit center
        public async Task DeleteClient_ErrorWhenUnauthorized(long clientIdArg, string passwordArg)
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = await controller.DeleteClient(clientIdArg, passwordArg);
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task DeleteClient_ErrorWhenClientHasChildren()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = await controller.DeleteClient(1, "password");
            #endregion

            #region Assert
            Assert.IsType<StatusCodeResult>(view);

            StatusCodeResult viewResult = (StatusCodeResult)view;
            Assert.Equal<int>(412, viewResult.StatusCode);
            #endregion
        }

        /// <summary>
        /// Verify that a deleted client is removed from persistence
        /// </summary>
        [Fact]
        public async Task DeleteClient_Success()
        {
            #region Arrange
            ClientAdminController controller = await GetControllerForUser("ClientAdmin1");

            int preCount = Enumerable.Count(TestResources.DbContextObject.Client);
            #endregion

            #region Act
            var view = await controller.DeleteClient(6, "password");
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);

            int postCount = Enumerable.Count(TestResources.DbContextObject.Client);
            Assert.Equal<int>((preCount - 1), postCount);
            #endregion
        }
    }
}
