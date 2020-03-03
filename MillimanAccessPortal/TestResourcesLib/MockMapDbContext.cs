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
using System.Threading;

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
            ReturnMockContext.Object.FileUpload = MockDbSet<FileUpload>.New(new List<FileUpload>()).Object;
            ReturnMockContext.Object.AuthenticationScheme = MockDbSet<AuthenticationScheme>.New(new List<AuthenticationScheme>()).Object;
            ReturnMockContext.Object.SftpAccount = MockDbSet<SftpAccount>.New(new List<SftpAccount>()).Object;
            ReturnMockContext.Object.FileDrop = MockDbSet<FileDrop>.New(new List<FileDrop>()).Object;
            ReturnMockContext.Object.FileDropUserPermissionGroup = MockDbSet<FileDropUserPermissionGroup>.New(new List<FileDropUserPermissionGroup>()).Object;
            ReturnMockContext.Object.FileDropDirectory = MockDbSet<FileDropDirectory>.New(new List<FileDropDirectory>()).Object;
            ReturnMockContext.Object.FileDropFile = MockDbSet<FileDropFile>.New(new List<FileDropFile>()).Object;

            List<ContentPublicationRequest> ContentPublicationRequestData = new List<ContentPublicationRequest>();
            Mock<DbSet<ContentPublicationRequest>> MockContentPublicationRequest = MockDbSet<ContentPublicationRequest>.New(ContentPublicationRequestData);
            MockContentPublicationRequest.Setup(d => d.Add(It.IsAny<ContentPublicationRequest>())).Callback<ContentPublicationRequest>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                ContentPublicationRequestData.Add(s);
                MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(MockContentPublicationRequest.Object, "ApplicationUserId", ReturnMockContext.Object.ApplicationUser);
                MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(MockContentPublicationRequest.Object, "RootContentItemId", ReturnMockContext.Object.RootContentItem);
            });
            MockContentPublicationRequest.Setup(d => d.AddRange(It.IsAny<IEnumerable<ContentPublicationRequest>>())).Callback<IEnumerable<ContentPublicationRequest>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.ContentPublicationRequest.Add(instance);
                }
            });
            ReturnMockContext.Object.ContentPublicationRequest = MockContentPublicationRequest.Object;

            List<ContentReductionTask> ContentReductionTaskData = new List<ContentReductionTask>();
            Mock<DbSet<ContentReductionTask>> MockContentReductionTask = MockDbSet<ContentReductionTask>.New(ContentReductionTaskData);
            MockContentReductionTask.Setup(d => d.Add(It.IsAny<ContentReductionTask>())).Callback<ContentReductionTask>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                s.CreateDateTimeUtc = s.ReductionStatus == ReductionStatusEnum.Replaced
                    ? DateTime.FromFileTimeUtc(50)
                    : DateTime.FromFileTimeUtc(200);
                ContentReductionTaskData.Add(s);
                MockDbSet<ContentReductionTask>.AssignNavigationProperty(MockContentReductionTask.Object, "ApplicationUserId", ReturnMockContext.Object.ApplicationUser);
                MockDbSet<ContentReductionTask>.AssignNavigationProperty(MockContentReductionTask.Object, "SelectionGroupId", ReturnMockContext.Object.SelectionGroup);
                MockDbSet<ContentReductionTask>.AssignNavigationProperty(MockContentReductionTask.Object, "ContentPublicationRequestId", ReturnMockContext.Object.ContentPublicationRequest);
            });
            MockContentReductionTask.Setup(d => d.AddRange(It.IsAny<IEnumerable<ContentReductionTask>>())).Callback<IEnumerable<ContentReductionTask>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.ContentReductionTask.Add(instance);
                }
            });
            ReturnMockContext.Object.ContentReductionTask = MockContentReductionTask.Object;

            List<RootContentItem> RootContentItemData = new List<RootContentItem>();
            Mock<DbSet<RootContentItem>> MockRootContentItem = MockDbSet<RootContentItem>.New(RootContentItemData);
            MockRootContentItem.Setup(d => d.Add(It.IsAny<RootContentItem>())).Callback<RootContentItem>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                RootContentItemData.Add(s);
                MockDbSet<RootContentItem>.AssignNavigationProperty(MockRootContentItem.Object, "ContentTypeId", ReturnMockContext.Object.ContentType);
                MockDbSet<RootContentItem>.AssignNavigationProperty(MockRootContentItem.Object, "ClientId", ReturnMockContext.Object.Client);
            });
            MockRootContentItem.Setup(d => d.AddRange(It.IsAny<IEnumerable<RootContentItem>>())).Callback< IEnumerable<RootContentItem>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.RootContentItem.Add(instance);
                }
            });
            ReturnMockContext.Object.RootContentItem = MockRootContentItem.Object;

            List<Client> ClientData = new List<Client>();
            Mock<DbSet<Client>> MockClient = MockDbSet<Client>.New(ClientData);
            MockClient.Setup(d => d.Add(It.IsAny<Client>())).Callback<Client>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                ClientData.Add(s);
                MockDbSet<Client>.AssignNavigationProperty(MockClient.Object, nameof(Client.ProfitCenterId), ReturnMockContext.Object.ProfitCenter);
                MockDbSet<Client>.AssignNavigationProperty(MockClient.Object, nameof(Client.ParentClientId), ReturnMockContext.Object.Client);
            });
            MockClient.Setup(d => d.AddRange(It.IsAny<IEnumerable<Client>>())).Callback<IEnumerable<Client>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.Client.Add(instance);
                }
            });
            ReturnMockContext.Object.Client = MockClient.Object;

            List<SelectionGroup> SelectionGroupData = new List<SelectionGroup>();
            Mock<DbSet<SelectionGroup>> MockSelectionGroup = MockDbSet<SelectionGroup>.New(SelectionGroupData);
            MockSelectionGroup.Setup(d => d.Add(It.IsAny<SelectionGroup>())).Callback<SelectionGroup>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                SelectionGroupData.Add(s);
                MockDbSet<SelectionGroup>.AssignNavigationProperty(MockSelectionGroup.Object, "RootContentItemId", ReturnMockContext.Object.RootContentItem);
            });
            MockSelectionGroup.Setup(d => d.AddRange(It.IsAny<IEnumerable<SelectionGroup>>())).Callback< IEnumerable<SelectionGroup>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.SelectionGroup.Add(instance);
                }
            });
            ReturnMockContext.Object.SelectionGroup = MockSelectionGroup.Object;

            // Give UserRoleInClient an additional Add() callback since it accesses properties of objects from Include()
            List<UserRoleInClient> UserRoleInClientData = new List<UserRoleInClient>();
            Mock<DbSet<UserRoleInClient>> MockUserRoleInClient = MockDbSet<UserRoleInClient>.New(UserRoleInClientData);
            MockUserRoleInClient.Setup(d => d.Add(It.IsAny<UserRoleInClient>())).Callback<UserRoleInClient>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                UserRoleInClientData.Add(s);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(MockUserRoleInClient.Object, "ClientId", ReturnMockContext.Object.Client);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(MockUserRoleInClient.Object, "UserId", ReturnMockContext.Object.ApplicationUser);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(MockUserRoleInClient.Object, "RoleId", ReturnMockContext.Object.Roles);
            });
            MockUserRoleInClient.Setup(d => d.AddRange(It.IsAny<IEnumerable<UserRoleInClient>>())).Callback<IEnumerable<UserRoleInClient>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.UserRoleInClient.Add(instance);
                }
            });
            ReturnMockContext.Object.UserRoleInClient = MockUserRoleInClient.Object;

            var userRoleInRootContentItemData = new List<UserRoleInRootContentItem>();
            var mockUserRoleInRootContentItem = MockDbSet<UserRoleInRootContentItem>.New(userRoleInRootContentItemData);
            mockUserRoleInRootContentItem.Setup(d => d.Add(It.IsAny<UserRoleInRootContentItem>())).Callback<UserRoleInRootContentItem>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                userRoleInRootContentItemData.Add(s);
                MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(mockUserRoleInRootContentItem.Object, "RootContentItemId", ReturnMockContext.Object.RootContentItem);
                MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(mockUserRoleInRootContentItem.Object, "RoleId", ReturnMockContext.Object.Roles);
                MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(mockUserRoleInRootContentItem.Object, "UserId", ReturnMockContext.Object.ApplicationUser);
            });
            mockUserRoleInRootContentItem.Setup(d => d.AddRange(It.IsAny<IEnumerable<UserRoleInRootContentItem>>())).Callback<IEnumerable<UserRoleInRootContentItem>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.UserRoleInRootContentItem.Add(instance);
                }
            });
            ReturnMockContext.Object.UserRoleInRootContentItem = mockUserRoleInRootContentItem.Object;

            List<UserInSelectionGroup> UserInSelectionGroupData = new List<UserInSelectionGroup>();
            Mock<DbSet<UserInSelectionGroup>> MockUserInSelectionGroup = MockDbSet<UserInSelectionGroup>.New(UserInSelectionGroupData);
            MockUserInSelectionGroup.Setup(d => d.Add(It.IsAny<UserInSelectionGroup>())).Callback<UserInSelectionGroup>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                UserInSelectionGroupData.Add(s);
                MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<SelectionGroup>(MockUserInSelectionGroup.Object, "SelectionGroupId", ReturnMockContext.Object.SelectionGroup);
                MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<ApplicationUser>(MockUserInSelectionGroup.Object, "UserId", ReturnMockContext.Object.ApplicationUser);
            });
            MockUserInSelectionGroup.Setup(d => d.AddRange(It.IsAny<IEnumerable<UserInSelectionGroup>>())).Callback<IEnumerable<UserInSelectionGroup>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.UserInSelectionGroup.Add(instance);
                }
            });
            ReturnMockContext.Object.UserInSelectionGroup = MockUserInSelectionGroup.Object;

            List<ApplicationUser> ApplicationUserData = new List<ApplicationUser>();
            Mock<DbSet<ApplicationUser>> MockApplicationUser = MockDbSet<ApplicationUser>.New(ApplicationUserData);
            MockApplicationUser.Setup(d => d.Add(It.IsAny<ApplicationUser>())).Callback<ApplicationUser>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                ApplicationUserData.Add(s);
                MockDbSet<ApplicationUser>.AssignNavigationProperty<AuthenticationScheme>(MockApplicationUser.Object, "AuthenticationSchemeId", ReturnMockContext.Object.AuthenticationScheme);
                s.SftpAccounts = new List<SftpAccount>();
            });
            MockApplicationUser.Setup(d => d.AddRange(It.IsAny<IEnumerable<ApplicationUser>>())).Callback<IEnumerable<ApplicationUser>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.ApplicationUser.Add(instance);
                }
            });
            ReturnMockContext.Object.ApplicationUser = MockApplicationUser.Object;

            List<SftpAccount> SftpAccountData = new List<SftpAccount>();
            Mock<DbSet<SftpAccount>> MockSftpAccount = MockDbSet<SftpAccount>.New(SftpAccountData);
            MockSftpAccount.Setup(d => d.Add(It.IsAny<SftpAccount>())).Callback<SftpAccount>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                SftpAccountData.Add(s);
                MockDbSet<SftpAccount>.AssignNavigationProperty<ApplicationUser>(MockSftpAccount.Object, nameof(SftpAccount.ApplicationUserId), ReturnMockContext.Object.ApplicationUser);
                MockDbSet<SftpAccount>.AssignNavigationProperty<FileDropUserPermissionGroup>(MockSftpAccount.Object, nameof(SftpAccount.FileDropUserPermissionGroupId), ReturnMockContext.Object.FileDropUserPermissionGroup);
                MockDbSet<SftpAccount>.AssignNavigationProperty<FileDrop>(MockSftpAccount.Object, nameof(SftpAccount.FileDropId), ReturnMockContext.Object.FileDrop);
                if (s.ApplicationUser != null)
                {
                    ((List<SftpAccount>)s.ApplicationUser.SftpAccounts).Add(s);
                }
            });
            MockSftpAccount.Setup(d => d.AddRange(It.IsAny<IEnumerable<SftpAccount>>())).Callback<IEnumerable<SftpAccount>>(s =>
            {
                foreach (SftpAccount account in s)
                {
                    ReturnMockContext.Object.SftpAccount.Add(account);
                }
            });
            ReturnMockContext.Object.SftpAccount = MockSftpAccount.Object;

            List<FileDropUserPermissionGroup> FileDropUserPermissionGroupData = new List<FileDropUserPermissionGroup>();
            Mock<DbSet<FileDropUserPermissionGroup>> MockFileDropUserPermissionGroup = MockDbSet<FileDropUserPermissionGroup>.New(FileDropUserPermissionGroupData);
            MockFileDropUserPermissionGroup.Setup(d => d.Add(It.IsAny<FileDropUserPermissionGroup>())).Callback<FileDropUserPermissionGroup>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                FileDropUserPermissionGroupData.Add(s);
                MockDbSet<FileDropUserPermissionGroup>.AssignNavigationProperty<FileDrop>(MockFileDropUserPermissionGroup.Object, nameof(FileDropUserPermissionGroup.FileDropId), ReturnMockContext.Object.FileDrop);
            });
            MockFileDropUserPermissionGroup.Setup(d => d.AddRange(It.IsAny<IEnumerable<FileDropUserPermissionGroup>>())).Callback<IEnumerable<FileDropUserPermissionGroup>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.FileDropUserPermissionGroup.Add(instance);
                }
            });
            ReturnMockContext.Object.FileDropUserPermissionGroup = MockFileDropUserPermissionGroup.Object;

            List<FileDrop> FileDropData = new List<FileDrop>();
            Mock<DbSet<FileDrop>> MockFileDrop = MockDbSet<FileDrop>.New(FileDropData);
            MockFileDrop.Setup(d => d.Add(It.IsAny<FileDrop>())).Callback<FileDrop>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                FileDropData.Add(s);
                MockDbSet<FileDrop>.AssignNavigationProperty<Client>(MockFileDrop.Object, nameof(FileDrop.ClientId), ReturnMockContext.Object.Client);
            });
            MockFileDrop.Setup(d => d.AddRange(It.IsAny<IEnumerable<FileDrop>>())).Callback<IEnumerable<FileDrop>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.FileDrop.Add(instance);
                }
            });
            ReturnMockContext.Object.FileDrop = MockFileDrop.Object;

            List<FileDropFile> FileDropFileData = new List<FileDropFile>();
            Mock<DbSet<FileDropFile>> MockFileDropFile = MockDbSet<FileDropFile>.New(FileDropFileData);
            MockFileDropFile.Setup(d => d.Add(It.IsAny<FileDropFile>())).Callback<FileDropFile>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                FileDropFileData.Add(s);
                MockDbSet<FileDropFile>.AssignNavigationProperty<SftpAccount>(MockFileDropFile.Object, nameof(FileDropFile.CreatedByAccountId), ReturnMockContext.Object.SftpAccount);
                MockDbSet<FileDropFile>.AssignNavigationProperty<FileDropDirectory>(MockFileDropFile.Object, nameof(FileDropFile.DirectoryId), ReturnMockContext.Object.FileDropDirectory);
            });
            MockFileDropFile.Setup(d => d.AddRange(It.IsAny<IEnumerable<FileDropFile>>())).Callback<IEnumerable<FileDropFile>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.FileDropFile.Add(instance);
                }
            });
            ReturnMockContext.Object.FileDropFile = MockFileDropFile.Object;

            List<FileDropDirectory> FileDropDirectoryData = new List<FileDropDirectory>();
            Mock<DbSet<FileDropDirectory>> MockFileDropDirectory = MockDbSet<FileDropDirectory>.New(FileDropDirectoryData);
            MockFileDropDirectory.Setup(d => d.Add(It.IsAny<FileDropDirectory>())).Callback<FileDropDirectory>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                FileDropDirectoryData.Add(s);
                MockDbSet<FileDropDirectory>.AssignNavigationProperty<FileDrop>(MockFileDropDirectory.Object, nameof(FileDropDirectory.FileDropId), ReturnMockContext.Object.FileDrop);
                MockDbSet<FileDropDirectory>.AssignNavigationProperty<FileDropDirectory>(MockFileDropDirectory.Object, nameof(FileDropDirectory.ParentDirectoryId), ReturnMockContext.Object.FileDropDirectory);
            });
            MockFileDropDirectory.Setup(d => d.AddRange(It.IsAny<IEnumerable<FileDropDirectory>>())).Callback<IEnumerable<FileDropDirectory>>(s =>
            {
                foreach (var instance in s)
                {
                    ReturnMockContext.Object.FileDropDirectory.Add(instance);
                }
            });
            ReturnMockContext.Object.FileDropDirectory = MockFileDropDirectory.Object;

            // Mock DbContext.Database.CommitTransaction() as no-op.
            Mock<IDbContextTransaction> DbTransaction = new Mock<IDbContextTransaction>();

            Mock<DatabaseFacade> MockDatabaseFacade = new Mock<DatabaseFacade>(ReturnMockContext.Object);
            MockDatabaseFacade.Setup(x => x.BeginTransaction()).Returns(DbTransaction.Object);
            MockDatabaseFacade.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(DbTransaction.Object);
            ReturnMockContext.SetupGet(x => x.Database).Returns(MockDatabaseFacade.Object);

            if (Initialize != null)
            {
                ReturnMockContext = Initialize(ReturnMockContext);
            }

            ReturnMockContext.Object.Users = ReturnMockContext.Object.ApplicationUser;
            ReturnMockContext.Object.Roles = ReturnMockContext.Object.ApplicationRole;

            return ReturnMockContext;
        }

        static readonly object LockObject = new object();
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
                    ApplicationRole NewRole = new ApplicationRole { Id = TestUtil.MakeTestGuid((int)Role), RoleEnum = Role, Name = Role.ToString(), NormalizedName = Role.ToString().ToUpper(), DisplayName = Role.GetDisplayNameString() };

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
