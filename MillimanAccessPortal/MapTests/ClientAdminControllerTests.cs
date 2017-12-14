using System;
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
        public ClientAdminController GetControllerForUser(string UserName)
        {
            ClientAdminController testController = new ClientAdminController(TestResources.DbContextObject,
                TestResources.UserManagerObject,
                TestResources.QueriesObj,
                TestResources.AuthorizationService,
                TestResources.LoggerFactory,
                TestResources.AuditLogger,
                TestResources.RoleManagerObject);

            // Generating ControllerContext will throw a NullReferenceException if the provided user does not exist
            testController.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.UserManagerObject.FindByNameAsync(UserName).Result.UserName);
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
                ParentClientId = 1,
                ProfitCenterId = 1
            };
        }

        /// <summary>
        /// Checks whether the Index action returns an UnauthorizedResult when the user is not authorized to view the ClientAdmin page
        /// </summary>
        [Fact]
        public void Index_ErrorWhenUnauthorized()
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("test1");
            #endregion

            #region Act
            var view = controller.Index();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        /// <summary>
        /// Checks whether the Index returns a view for authorized users
        /// </summary>
        [Fact]
        public void Index_ReturnsAView()
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = controller.Index();
            #endregion

            #region Assert
            Assert.IsType<ViewResult>(view);
            #endregion
        }

        /// <summary>
        /// Checks whether ClientFamilyList returns an error when the user is not authorized to view the ClientAdmin page
        /// </summary>
        [Fact]
        public void ClientFamilyList_ErrorWhenUnauthorized()
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("test1");
            #endregion

            #region Act
            var view = controller.ClientFamilyList();
            #endregion

            #region Assert
            Assert.IsType<UnauthorizedResult>(view);
            #endregion
        }

        /// <summary>
        /// Checks whether ClientFamilyList returns a list of clients for authorized users
        /// </summary>
        [Fact]
        public void ClientFamilyList_ReturnsAList()
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = controller.ClientFamilyList();
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            #endregion
        }

        /// <summary>
        /// Checks whether ClientDetail returns an error when the client is not found
        /// </summary>
        [Fact]
        public void ClientDetail_ErrorWhenNotFound()
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("ClientAdmin1");
            #endregion

            #region Act
            var view = controller.ClientDetail(-100);
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
        public void ClientDetail_ErrorWhenUnauthorized(string userArg, long clientIdArg)
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser(userArg);
            #endregion

            #region Act
            var view = controller.ClientDetail(clientIdArg);
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
        public void ClientDetail_ReturnsDetails(string userArg, long clientIdArg)
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser(userArg);
            #endregion

            #region Act
            var view = controller.ClientDetail(clientIdArg);
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
        public void AssignUserToClient_ErrorWhenUnauthorized(string userArg, long clientIdArg, string userAssignArg)
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser(userArg);
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = clientIdArg, UserName = userAssignArg };
            #endregion

            #region Act
            var view = controller.AssignUserToClient(viewModel);
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
        public void AssignUserToClient_ErrorWhenNotFound(string userArg, long clientIdArg, string userAssignArg)
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser(userArg);
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = clientIdArg, UserName = userAssignArg };
            #endregion

            #region Act
            var view = controller.AssignUserToClient(viewModel);
            #endregion

            #region Assert
            Assert.IsType<BadRequestObjectResult>(view);
            #endregion
        }

        /// <summary>
        /// Verify that no data is changed when adding a user to a client they are already assigned to
        /// </summary>
        [Fact]
        public void AssignUserToClient_NoActionWhenAssigned()
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("ClientAdmin1");
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = 1, UserName = "test1" };

            // Before acting on the input data, we need to gather initial data to compare the result to
            JsonResult preView = (JsonResult)controller.ClientDetail(viewModel.ClientId);
            ClientDetailViewModel preViewModel = (ClientDetailViewModel)preView.Value;
            string preActionCount = preViewModel.AssignedUsers.Count.ToString();
            #endregion

            #region Act
            var view = controller.AssignUserToClient(viewModel);

            // Capture the number of users assigned to the client after the call to AssignUserToClient
            JsonResult viewResult = (JsonResult)view;
            ClientDetailViewModel afterViewModel = (ClientDetailViewModel)viewResult.Value;
            string afterActionCount = afterViewModel.AssignedUsers.Count.ToString();
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
        public void AssignUserToClient_ErrorForInvalidEmail()
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("ClientAdmin1");
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = 5, UserName = "test1" };
            #endregion

            #region Act
            var view = controller.AssignUserToClient(viewModel);
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
        public void AssignUserToClient_Success()
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("ClientAdmin1");
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = 5, UserName = "test3" };

            // Before acting on the input data, we need to gather initial data to compare the result to
            int beforeCount = Enumerable.Count(TestResources.DbContextObject.UserClaims);
            int expectedAfterCount = beforeCount + 1;
            #endregion

            #region Act
            var view = controller.AssignUserToClient(viewModel);

            // Capture the number of users assigned to the client after the call to AssignUserToClient
            int afterActionCount = Enumerable.Count(TestResources.DbContextObject.UserClaims);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);
            Assert.Equal<int>(expectedAfterCount, afterActionCount);
            #endregion
        }

        /// <summary>
        /// Validate that an UnauthorizedResult is returned if the user is not authorized to remove users from the requested client
        /// Multiple authorizations are checked, so multiple scenarios should be tested
        /// </summary>
        [Theory]
        [InlineData("ClientAdmin1", 3, "test1")] // User isn't admin on the requested client but is admin on the requested client's profit center
        [InlineData("ClientAdmin1", 4, "test1")] // User is admin on the requested client but isn't admin on the requested client's profit center
        public void RemoveUserFromClient_ErrorWhenUnauthorized(string userArg, long clientIdArg, string userAssignArg)
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser(userArg);
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = clientIdArg, UserName = userAssignArg };
            #endregion

            #region Act
            var view = controller.RemoveUserFromClient(viewModel);
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
        public void RemoveUserFromClient_ErrorWhenNotFound(string userArg, long clientIdArg, string userAssignArg)
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser(userArg);
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = clientIdArg, UserName = userAssignArg };
            #endregion

            #region Act
            var view = controller.RemoveUserFromClient(viewModel);
            #endregion

            #region Assert
            Assert.IsType<BadRequestObjectResult>(view);
            #endregion
        }

        /// <summary>
        /// Validate that a user is removed from a client when a request is made by an authorized user for a valid user & client
        /// </summary>
        [Fact]
        public void RemoveUserFromClient_Success()
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("ClientAdmin1");
            ClientUserAssociationViewModel viewModel = new ClientUserAssociationViewModel { ClientId = 5, UserName = "test2" };

            JsonResult preView = (JsonResult)controller.ClientDetail(viewModel.ClientId);
            ClientDetailViewModel preViewModel = (ClientDetailViewModel)preView.Value;
            int preActionCount = preViewModel.AssignedUsers.Count;
            int expectedAfterActionCount = preActionCount - 1;
            #endregion

            #region Act
            var view = controller.RemoveUserFromClient(viewModel);
            #endregion

            #region Assert
            Assert.IsType<JsonResult>(view);

            // Capture the number of users assigned to the client after the call to RemoveUserFromClient
            JsonResult viewResult = (JsonResult)view;
            ClientDetailViewModel afterViewModel = (ClientDetailViewModel)viewResult.Value;
            int afterActionCount = afterViewModel.AssignedUsers.Count;

            Assert.Equal<int>(expectedAfterActionCount, afterActionCount);
            #endregion
        }

        /// <summary>
        /// Validate that trying to save a client with itself as the test client results in a bad request
        /// </summary>
        [Fact]
        public void SaveNewClient_ErrorWhenParentIdIsClientId()
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("ClientAdmin1");
            Client testClient = GetValidClient();
            #endregion

            #region Act
            testClient.ParentClientId = testClient.Id;
            var view = controller.SaveNewClient(testClient);
            #endregion

            #region Assert
            Assert.IsType<BadRequestObjectResult>(view);
            #endregion
        }

        /// <summary>
        /// Validate that a bad request is returned when ModelState.IsValid returns false
        /// </summary>
        [Fact]
        public void SaveNewClient_ErrorWhenModelStateInvalid()
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("ClientAdmin1");
            Client testClient = GetValidClient();
            #endregion

            #region Act
            controller.ModelState.AddModelError("BadModel", "This is a forced bad model.");
            var view = controller.SaveNewClient(testClient);
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
        public void SaveNewClient_ErrorWhenNotAuthorized(string userArg, int? parentClientIdArg, int? profitCenterIdArg)
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser(userArg);
            Client testClient = GetValidClient();
            #endregion

            #region Act
            testClient.ParentClientId = parentClientIdArg;
            testClient.ProfitCenterId = (long)profitCenterIdArg;
            var view = controller.SaveNewClient(testClient);
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
        public void SaveNewClient_ErrorWhenEmailInvalid(string[] domainListArg, string[] emailListArg)
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("ClientAdmin1");
            Client testClient = GetValidClient();
            #endregion

            #region Act
            if (domainListArg != null)
            {
                testClient.AcceptedEmailDomainList = domainListArg;
            }
            if (emailListArg != null)
            {
                testClient.AcceptedEmailAddressExceptionList = emailListArg;
            }
            var view = controller.SaveNewClient(testClient);
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
        public void SaveNewClient_Success()
        {
            #region Arrange
            ClientAdminController controller = GetControllerForUser("ClientAdmin1");
            Client testClient = GetValidClient();

            int beforeCount = Enumerable.Count(TestResources.DbContextObject.Client);
            int expectedAfterCount = beforeCount + 1;
            #endregion

            #region Act
            var view = controller.SaveNewClient(testClient);
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
        [Fact]
        public void EditClient_ErrorWhenInvalidRequest()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate that unauthorized users receive an UnauthorizedResult
        /// Multiple authorizations are checked and must be tested
        /// </summary>
        [Fact]
        public void EditClient_ErrorWhenUnauthorized()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate that invalid data causes return code 412
        /// Multiple scenarios should cause code 412 and must be tested
        /// </summary>
        [Fact]
        public void EditClient_ErrorWhenInvalid()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate that client edits are made and saved successfully when the model is valid and the user is authorized
        /// </summary>
        [Fact]
        public void EditClient_Success()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void DeleteClient_ErrorWhenBadRequest()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void DeleteClient_ErrorWhenUnauthorized()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void DeleteClient_ErrorWhenInvalid()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void DeleteClient_Success()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void DeleteClient_Failed()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void GetCurrentApplicationUser_ReturnsCorrectUser()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void GetClientAdminIndexModelForUser_ReturnsNullForNullUser()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void GetClientAdminIndexModelForUser_ReturnsValidModel()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void GetCleanClientEmailWhitelistArray_ReturnsCleanArray()
        {
            throw new NotImplementedException();
        }
    }
}
