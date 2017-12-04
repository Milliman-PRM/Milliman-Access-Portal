using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.ClientAdminViewModels;

namespace MapTests
{
    public class ClientAdminControllerTests
    {

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
    }
}
