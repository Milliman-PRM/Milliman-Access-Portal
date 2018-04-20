/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Contains all test data definitions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore.Storage;

namespace TestResourcesLib
{
    /// <summary>
    /// Support for mocking of a ApplicationDbContext instance
    /// </summary>
    public static class MockMapDbContext
    {
        /// <summary>
        /// Creates an instance of mocked ApplicationDbContext with no data
        /// </summary>
        public static Mock<ApplicationDbContext> New()
        {
            // Had to implement a parameterless constructor in the context class, I hope this doesn't cause any problem in EF
            Mock<ApplicationDbContext> ReturnMockContext = new Mock<ApplicationDbContext>();
            ReturnMockContext.Object.ApplicationRole = MockDbSet<ApplicationRole>.New(GetSystemRolesList()).Object;
            ReturnMockContext.Object.ApplicationUser = MockDbSet<ApplicationUser>.New(new List<ApplicationUser>()).Object;
            ReturnMockContext.Object.ContentType = MockDbSet<ContentType>.New(new List<ContentType>()).Object;
            ReturnMockContext.Object.ProfitCenter = MockDbSet<ProfitCenter>.New(new List<ProfitCenter>()).Object;
            ReturnMockContext.Object.UserRoleInProfitCenter = MockDbSet<UserRoleInProfitCenter>.New(new List<UserRoleInProfitCenter>()).Object;
            ReturnMockContext.Object.Client = MockDbSet<Client>.New(new List<Client>()).Object;
            ReturnMockContext.Object.RootContentItem = MockDbSet<RootContentItem>.New(new List<RootContentItem>()).Object;
            ReturnMockContext.Object.HierarchyFieldValue = MockDbSet<HierarchyFieldValue>.New(new List<HierarchyFieldValue>()).Object;
            ReturnMockContext.Object.HierarchyField = MockDbSet<HierarchyField>.New(new List<HierarchyField>()).Object;
            ReturnMockContext.Object.SelectionGroup = MockDbSet<SelectionGroup>.New(new List<SelectionGroup>()).Object;
            ReturnMockContext.Object.UserRoles = MockDbSet<IdentityUserRole<long>>.New(new List<IdentityUserRole<long>>()).Object;
            ReturnMockContext.Object.UserRoleInRootContentItem = MockDbSet<UserRoleInRootContentItem>.New(new List<UserRoleInRootContentItem>()).Object;
            ReturnMockContext.Object.UserClaims = MockDbSet<IdentityUserClaim<long>>.New(new List<IdentityUserClaim<long>>()).Object;
            ReturnMockContext.Object.ContentPublicationRequest = MockDbSet<ContentPublicationRequest>.New(new List<ContentPublicationRequest>()).Object;
            ReturnMockContext.Object.ContentReductionTask = MockDbSet<ContentReductionTask>.New(new List<ContentReductionTask>()).Object;
            ReturnMockContext.Object.Users = ReturnMockContext.Object.ApplicationUser;
            ReturnMockContext.Object.Roles = ReturnMockContext.Object.ApplicationRole;

            // Give UserRoleInClient an additional Add() callback since it accesses properties of objects from Include()
            List<UserRoleInClient> UserRoleInClientData = new List<UserRoleInClient>();
            Mock<DbSet<UserRoleInClient>> MockUserRoleInClient = MockDbSet<UserRoleInClient>.New(UserRoleInClientData);
            MockUserRoleInClient.Setup(d => d.Add(It.IsAny<UserRoleInClient>())).Callback<UserRoleInClient>(s =>
            {
                UserRoleInClientData.Add(s);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(MockUserRoleInClient.Object, "ClientId", ReturnMockContext.Object.Client);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(MockUserRoleInClient.Object, "UserId", ReturnMockContext.Object.ApplicationUser);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(MockUserRoleInClient.Object, "RoleId", ReturnMockContext.Object.ApplicationRole);
            });
            ReturnMockContext.Object.UserRoleInClient = MockUserRoleInClient.Object;

            List<UserInSelectionGroup> UserInSelectionGroupData = new List<UserInSelectionGroup>();
            Mock<DbSet<UserInSelectionGroup>> MockUserInSelectionGroup = MockDbSet<UserInSelectionGroup>.New(UserInSelectionGroupData);
            MockUserInSelectionGroup.Setup(d => d.AddRange(It.IsAny<IEnumerable<UserInSelectionGroup>>())).Callback<IEnumerable<UserInSelectionGroup>>(s =>
            {
                UserInSelectionGroupData.AddRange(s);
                MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<SelectionGroup>(MockUserInSelectionGroup.Object, "SelectionGroupId", ReturnMockContext.Object.SelectionGroup);
                MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<ApplicationUser>(MockUserInSelectionGroup.Object, "UserId", ReturnMockContext.Object.ApplicationUser);
            });
            ReturnMockContext.Object.UserInSelectionGroup = MockUserInSelectionGroup.Object;

            // Mock DbContext.Database.CommitTransaction() as no ops.
            Mock<IDbContextTransaction> DbTransaction = new Mock<IDbContextTransaction>();

            Mock<DatabaseFacade> MockDatabaseFacade = new Mock<DatabaseFacade>(ReturnMockContext.Object);
            MockDatabaseFacade.Setup(x => x.BeginTransaction()).Returns(DbTransaction.Object);
            ReturnMockContext.SetupGet(x => x.Database).Returns(MockDatabaseFacade.Object);

            return ReturnMockContext;
        }

        private static List<ApplicationRole> GetSystemRolesList()
        {
            List<ApplicationRole> ReturnList = new List<ApplicationRole>();

            foreach (RoleEnum Role in Enum.GetValues(typeof(RoleEnum)))
            {
                ReturnList.Add(new ApplicationRole { Id = (long)Role, RoleEnum = Role, Name = Role.ToString(), NormalizedName = Role.ToString().ToUpper() });
            }
            return ReturnList;
        }
    }
}
