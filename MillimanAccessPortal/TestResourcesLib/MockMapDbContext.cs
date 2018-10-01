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
        /// Creates an instance of mocked ApplicationDbContext. 
        /// </summary>
        /// <param name="Initialize">A Func that modifies the mocked context to be returned. Optional, you can add data later too</param>
        /// <returns></returns>
        public static Mock<ApplicationDbContext> New(Func<Mock<ApplicationDbContext>, Mock<ApplicationDbContext>> Initialize = null)
        {
            // Had to implement a parameterless constructor in the context class, I hope this doesn't cause any problem in EF
            Mock<ApplicationDbContext> ReturnMockContext = new Mock<ApplicationDbContext>();
            ReturnMockContext.Object.ApplicationRole = MockDbSet<ApplicationRole>.New(GetSystemRolesList()).Object;
            ReturnMockContext.Object.ApplicationUser = MockDbSet<ApplicationUser>.New(new List<ApplicationUser>()).Object;
            ReturnMockContext.Object.ContentType = MockDbSet<ContentType>.New(new List<ContentType>()).Object;
            ReturnMockContext.Object.ProfitCenter = MockDbSet<ProfitCenter>.New(new List<ProfitCenter>()).Object;
            ReturnMockContext.Object.UserRoleInProfitCenter = MockDbSet<UserRoleInProfitCenter>.New(new List<UserRoleInProfitCenter>()).Object;
            ReturnMockContext.Object.Client = MockDbSet<Client>.New(new List<Client>()).Object;
            ReturnMockContext.Object.HierarchyFieldValue = MockDbSet<HierarchyFieldValue>.New(new List<HierarchyFieldValue>()).Object;
            ReturnMockContext.Object.HierarchyField = MockDbSet<HierarchyField>.New(new List<HierarchyField>()).Object;
            ReturnMockContext.Object.UserRoles = MockDbSet<IdentityUserRole<Guid>>.New(new List<IdentityUserRole<Guid>>()).Object;
            ReturnMockContext.Object.UserClaims = MockDbSet<IdentityUserClaim<Guid>>.New(new List<IdentityUserClaim<Guid>>()).Object;
            ReturnMockContext.Object.Users = ReturnMockContext.Object.ApplicationUser;
            ReturnMockContext.Object.Roles = ReturnMockContext.Object.ApplicationRole;
            ReturnMockContext.Object.FileUpload = MockDbSet<FileUpload>.New(new List<FileUpload>()).Object;
            
            List<ContentPublicationRequest> ContentPublicationRequestData = new List<ContentPublicationRequest>();
            Mock<DbSet<ContentPublicationRequest>> MockContentPublicationRequest = MockDbSet<ContentPublicationRequest>.New(ContentPublicationRequestData);
            MockContentPublicationRequest.Setup(d => d.Add(It.IsAny<ContentPublicationRequest>())).Callback<ContentPublicationRequest>(s =>
            {
                ContentPublicationRequestData.Add(s);
                MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(MockContentPublicationRequest.Object, "ApplicationUserId", ReturnMockContext.Object.ApplicationUser);
                MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(MockContentPublicationRequest.Object, "RootContentItemId", ReturnMockContext.Object.RootContentItem);
            });
            ReturnMockContext.Object.ContentPublicationRequest = MockContentPublicationRequest.Object;

            List<ContentReductionTask> ContentReductionTaskData = new List<ContentReductionTask>();
            Mock<DbSet<ContentReductionTask>> MockContentReductionTask = MockDbSet<ContentReductionTask>.New(ContentReductionTaskData);
            MockContentReductionTask.Setup(d => d.Add(It.IsAny<ContentReductionTask>())).Callback<ContentReductionTask>(s =>
            {
                s.CreateDateTimeUtc = s.ReductionStatus == ReductionStatusEnum.Replaced
                    ? DateTime.FromFileTimeUtc(50)
                    : DateTime.FromFileTimeUtc(200);
                ContentReductionTaskData.Add(s);
                MockDbSet<ContentReductionTask>.AssignNavigationProperty(MockContentReductionTask.Object, "ApplicationUserId", ReturnMockContext.Object.ApplicationUser);
                MockDbSet<ContentReductionTask>.AssignNavigationProperty(MockContentReductionTask.Object, "SelectionGroupId", ReturnMockContext.Object.SelectionGroup);
                MockDbSet<ContentReductionTask>.AssignNavigationProperty(MockContentReductionTask.Object, "ContentPublicationRequestId", ReturnMockContext.Object.ContentPublicationRequest);
            });
            ReturnMockContext.Object.ContentReductionTask = MockContentReductionTask.Object;

            List<RootContentItem> RootContentItemData = new List<RootContentItem>();
            Mock<DbSet<RootContentItem>> MockRootContentItem = MockDbSet<RootContentItem>.New(RootContentItemData);
            MockRootContentItem.Setup(d => d.Add(It.IsAny<RootContentItem>())).Callback<RootContentItem>(s =>
            {
                RootContentItemData.Add(s);
                MockDbSet<RootContentItem>.AssignNavigationProperty(MockRootContentItem.Object, "ContentTypeId", ReturnMockContext.Object.ContentType);
                MockDbSet<RootContentItem>.AssignNavigationProperty(MockRootContentItem.Object, "ClientId", ReturnMockContext.Object.Client);
            });
            ReturnMockContext.Object.RootContentItem = MockRootContentItem.Object;

            List<SelectionGroup> SelectionGroupData = new List<SelectionGroup>();
            Mock<DbSet<SelectionGroup>> MockSelectionGroup = MockDbSet<SelectionGroup>.New(SelectionGroupData);
            MockSelectionGroup.Setup(d => d.Add(It.IsAny<SelectionGroup>())).Callback<SelectionGroup>(s =>
            {
                SelectionGroupData.Add(s);
                MockDbSet<SelectionGroup>.AssignNavigationProperty(MockSelectionGroup.Object, "RootContentItemId", ReturnMockContext.Object.RootContentItem);
            });
            ReturnMockContext.Object.SelectionGroup = MockSelectionGroup.Object;

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

            var userRoleInRootContentItemData = new List<UserRoleInRootContentItem>();
            var mockUserRoleInRootContentItem = MockDbSet<UserRoleInRootContentItem>.New(userRoleInRootContentItemData);
            mockUserRoleInRootContentItem.Setup(d => d.Add(It.IsAny<UserRoleInRootContentItem>())).Callback<UserRoleInRootContentItem>(s =>
            {
                userRoleInRootContentItemData.Add(s);
                MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(mockUserRoleInRootContentItem.Object, "RootContentItemId", ReturnMockContext.Object.RootContentItem);
                MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(mockUserRoleInRootContentItem.Object, "RoleId", ReturnMockContext.Object.Roles);
                MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(mockUserRoleInRootContentItem.Object, "UserId", ReturnMockContext.Object.ApplicationUser);
            });
            mockUserRoleInRootContentItem.Setup(d => d.AddRange(It.IsAny<IEnumerable<UserRoleInRootContentItem>>())).Callback<IEnumerable<UserRoleInRootContentItem>>(s =>
            {
                userRoleInRootContentItemData.AddRange(s);
                MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(mockUserRoleInRootContentItem.Object, "RootContentItemId", ReturnMockContext.Object.RootContentItem);
                MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(mockUserRoleInRootContentItem.Object, "RoleId", ReturnMockContext.Object.Roles);
                MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(mockUserRoleInRootContentItem.Object, "UserId", ReturnMockContext.Object.ApplicationUser);
            });
            ReturnMockContext.Object.UserRoleInRootContentItem = mockUserRoleInRootContentItem.Object;

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

            if (Initialize != null)
            {
                ReturnMockContext = Initialize(ReturnMockContext);
            }

            return ReturnMockContext;
        }

        static object LockObject = new object();
        private static List<ApplicationRole> GetSystemRolesList()
        {
            lock (LockObject)
            {
                bool ResetRoles = ApplicationRole.RoleIds.Count != Enum.GetValues(typeof(RoleEnum)).Length;

                List<ApplicationRole> ReturnList = new List<ApplicationRole>();
                if (ResetRoles)
                {
                    ApplicationRole.RoleIds.Clear();
                }

                foreach (RoleEnum Role in Enum.GetValues(typeof(RoleEnum)))
                {
                    ApplicationRole NewRole = new ApplicationRole { Id = new Guid((int)Role,1,1,1,1,1,1,1,1,1,1), RoleEnum = Role, Name = Role.ToString(), NormalizedName = Role.ToString().ToUpper(), DisplayName = ApplicationRole.RoleDisplayNames[Role] };

                    ReturnList.Add(NewRole);
                    if (ResetRoles)
                    {
                        ApplicationRole.RoleIds.Add(Role, NewRole.Id);
                    }
                }
                return ReturnList;
            }
        }
    }
}
