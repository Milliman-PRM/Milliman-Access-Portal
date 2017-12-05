using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.ClientAdminViewModels;
using System.Linq;

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

            testController.ControllerContext = TestInitialization.GenerateControllerContext(UserAsUserName: TestResources.UserManagerObject.FindByNameAsync("test1").Result.UserName);
            testController.HttpContext.Session = new MockSession();

            return testController;
        }

        /// <summary>
        /// Checks whether the Index action returns an UnauthorizedResult when the user is not authorized to view the ClientAdmin page
        /// </summary>
        [Fact]
        public void Index_ErrorWhenUnauthorized()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks whether ClientFamilyList returns an error when the user is not authorized to view the ClientAdmin page
        /// </summary>
        [Fact]
        public void ClientFamilyList_ErrorWhenUnauthorized()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks whether ClientFamilyList returns a list of clients for authorized users
        /// </summary>
        [Fact]
        public void ClientFamilyList_ReturnsAList()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks whether ClientDetail returns an error when the client is not found
        /// </summary>
        [Fact]
        public void ClientDetail_ErrorWhenNotFound()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks whether ClientDetail returns an error when the user is not authorized to view the ClientAdmin page
        /// </summary>
        [Fact]
        public void ClientDetail_ErrorWhenUnauthorized()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks whether ClientDetail returns the ClientDetail Json model to authorized users
        /// </summary>
        [Fact]
        public void ClientDetail_ReturnsDetails()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks whether AssignUserToClient returns an error for unauthorized users
        /// Multiple authorization checks are made, so multiple users should be tested w/ various rights
        /// </summary>
        [Fact]
        public void AssignUserToClient_ErrorWhenUnauthorized()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Verify a NotFoundResult is returned when the user or client is not found
        /// </summary>
        [Fact]
        public void AssignUserToClient_ErrorWhenNotFound()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Verify that a warning is raised when adding a user to a client they are already assigned to
        /// </summary>
        [Fact]
        public void AssignUserToClient_WarningWhenAssigned()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Verify that all the various email domain checks function correctly
        /// Multiple checks are made, so multiple users should be tested w/ various email addresses & domains
        /// Return code from the request should be 412 - Precondition Failed
        /// </summary>
        [Fact]
        public void AssignUserToClient_ErrorForInvalidEmail()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate that the user is assigned to the client correctly when a valid request is made
        /// </summary>
        [Fact]
        public void AssignUserToClient_Success()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate that an UnauthorizedResult is returned if the user is not authorized to remove users from the requested client
        /// Multiple authorizations are checked, so multiple scenarios should be tested
        /// </summary>
        [Fact]
        public void RemoveUserFromClient_ErrorWhenUnauthorized()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate that a BadRequestResult is returned if the client, user, or both do not exist
        /// </summary>
        [Fact]
        public void RemoveUserFromClient_ErrorWhenNotFound()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate that a user is removed from a client when a request is made by an authorized user for a valid user & client
        /// </summary>
        [Fact]
        public void RemoveUserFromClient_Success()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate that an invalid request returns a BadRequestResult
        /// Multiple types of validations are checked, so multiple tests should be run
        /// </summary>
        [Fact]
        public void SaveNewClient_ErrorWhenInvalidRequest()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate that an UnauthorizedResult is returned for unauthorized users
        /// Multiple authorization checks are made and must be tested
        /// </summary>
        [Fact]
        public void SaveNewClient_ErrorWhenNothAuthorized()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate that status code 412 is returned when invalid email domains or addresses are provided
        /// </summary>
        [Fact]
        public void SaveNewClient_ErrorWhenEmailInvalid()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate that new clients are added successfully when the model is valid and the user is authorized
        /// </summary>
        [Fact]
        public void SaveNewClient_Success()
        {
            throw new NotImplementedException();
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
