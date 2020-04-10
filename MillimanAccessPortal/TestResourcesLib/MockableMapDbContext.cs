/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Contains all test data definitions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TestResourcesLib;

namespace TestResourcesLib
{
    /// <summary>
    /// Support for mocking of a ApplicationDbContext instance
    /// </summary>
    public class MockableMapDbContext : ApplicationDbContext
    {
        #region declare mocks behind the context entities
        #region MAP entity mocks
        private Mock<DbSet<Client>> MockClient { get; set; }
        private Mock<DbSet<UserRoleInClient>> MockUserRoleInClient { get; set; }
        private Mock<DbSet<UserRoleInProfitCenter>> MockUserRoleInProfitCenter { get; set; }
        private Mock<DbSet<UserRoleInRootContentItem>> MockUserRoleInRootContentItem { get; set; }
        private Mock<DbSet<UserInSelectionGroup>> MockUserInSelectionGroup { get; set; }
        private Mock<DbSet<SelectionGroup>> MockSelectionGroup { get; set; }
        private Mock<DbSet<RootContentItem>> MockRootContentItem { get; set; }
        private Mock<DbSet<HierarchyField>> MockHierarchyField { get; set; }
        private Mock<DbSet<HierarchyFieldValue>> MockHierarchyFieldValue { get; set; }
        private Mock<DbSet<ContentType>> MockContentType { get; set; }
        private Mock<DbSet<ProfitCenter>> MockProfitCenter { get; set; }
        private Mock<DbSet<ContentReductionTask>> MockContentReductionTask { get; set; }
        private Mock<DbSet<ContentPublicationRequest>> MockContentPublicationRequest { get; set; }
        private Mock<DbSet<FileUpload>> MockFileUpload { get; set; }
        private Mock<DbSet<AuthenticationScheme>> MockAuthenticationScheme { get; set; }
        private Mock<DbSet<NameValueConfiguration>> MockNameValueConfiguration { get; set; }
        private Mock<DbSet<SftpAccount>> MockSftpAccount { get; set; }
        private Mock<DbSet<FileDrop>> MockFileDrop { get; set; }
        private Mock<DbSet<FileDropUserPermissionGroup>> MockFileDropUserPermissionGroup { get; set; }
        private Mock<DbSet<FileDropDirectory>> MockFileDropDirectory { get; set; }
        private Mock<DbSet<FileDropFile>> MockFileDropFile { get; set; }
        #endregion

        #region From (public abstract class IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>)
        private Mock<DbSet<IdentityUserRole<Guid>>> MockUserRoles { get; set; }
        private Mock<DbSet<ApplicationRole>> MockApplicationRole { get; set; }
        public Mock<DbSet<IdentityRoleClaim<Guid>>> MockRoleClaims { get; set; }
        #endregion

        #region From (public abstract class IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken>)
        private Mock<DbSet<ApplicationUser>> MockApplicationUser { get; set; }
        private Mock<DbSet<IdentityUserClaim<Guid>>> MockUserClaims { get; set; }
        private Mock<DbSet<IdentityUserLogin<Guid>>> MockUserLogins { get; set; }
        private Mock<DbSet<IdentityUserToken<Guid>>> MockUserTokens { get; set; }
        #endregion
        #endregion

        #region Declare entity getter properties
        #region MAP entity getters
        public new DbSet<Client> Client { get => MockClient.Object; }
        public new DbSet<UserRoleInClient> UserRoleInClient { get => MockUserRoleInClient.Object; }
        public new DbSet<UserRoleInProfitCenter> UserRoleInProfitCenter { get => MockUserRoleInProfitCenter.Object; }
        public new DbSet<UserRoleInRootContentItem> UserRoleInRootContentItem { get => MockUserRoleInRootContentItem.Object; }
        public new DbSet<UserInSelectionGroup> UserInSelectionGroup { get => MockUserInSelectionGroup.Object; }
        public new DbSet<SelectionGroup> SelectionGroup { get => MockSelectionGroup.Object; }
        public new DbSet<RootContentItem> RootContentItem { get => MockRootContentItem.Object; }
        public new DbSet<HierarchyField> HierarchyField { get => MockHierarchyField.Object; }
        public new DbSet<HierarchyFieldValue> HierarchyFieldValue { get => MockHierarchyFieldValue.Object; }
        public new DbSet<ContentType> ContentType { get => MockContentType.Object; }
        public new DbSet<ProfitCenter> ProfitCenter { get => MockProfitCenter.Object; }
        public new DbSet<ContentReductionTask> ContentReductionTask { get => MockContentReductionTask.Object; }
        public new DbSet<ContentPublicationRequest> ContentPublicationRequest { get => MockContentPublicationRequest.Object; }
        public new DbSet<FileUpload> FileUpload { get => MockFileUpload.Object; }
        public new DbSet<AuthenticationScheme> AuthenticationScheme { get => MockAuthenticationScheme.Object; }
        public new DbSet<NameValueConfiguration> NameValueConfiguration { get => MockNameValueConfiguration.Object; }
        public new DbSet<SftpAccount> SftpAccount { get => MockSftpAccount.Object; }
        public new DbSet<FileDrop> FileDrop { get => MockFileDrop.Object; }
        public new DbSet<FileDropUserPermissionGroup> FileDropUserPermissionGroup { get => MockFileDropUserPermissionGroup.Object; }
        public new DbSet<FileDropDirectory> FileDropDirectory { get => MockFileDropDirectory.Object; }
        public new DbSet<FileDropFile> FileDropFile { get => MockFileDropFile.Object; }
        #endregion

        #region Inherited from (public abstract class IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>)
        public new DbSet<IdentityUserRole<Guid>> UserRoles => MockUserRoles.Object;
        public new DbSet<ApplicationRole> ApplicationRole => MockApplicationRole.Object;
        public new DbSet<IdentityRoleClaim<Guid>> RoleClaims => MockRoleClaims.Object;
        #endregion

        #region Inherited from (public abstract class IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken>)
        public new DbSet<ApplicationUser> ApplicationUser => MockApplicationUser.Object;
        public new DbSet<IdentityUserClaim<Guid>> UserClaims => MockUserClaims.Object;
        public new DbSet<IdentityUserLogin<Guid>> UserLogins => MockUserLogins.Object;
        public new DbSet<IdentityUserToken<Guid>> UserTokens => MockUserTokens.Object;
        #endregion
        #endregion

        public MockableMapDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {}

        public MockableMapDbContext()
        {
            // Had to implement a parameterless constructor in the context class, I hope this doesn't cause any problem in EF
            #region MAP entity mocks
            MockClient = MockDbSet<Client>.New();
            MockUserRoleInClient = MockDbSet<UserRoleInClient>.New();
            MockUserRoleInProfitCenter = MockDbSet<UserRoleInProfitCenter>.New();
            MockUserRoleInRootContentItem = MockDbSet<UserRoleInRootContentItem>.New();
            MockUserInSelectionGroup = MockDbSet<UserInSelectionGroup>.New();
            MockSelectionGroup = MockDbSet<SelectionGroup>.New();
            MockRootContentItem = MockDbSet<RootContentItem>.New();
            MockHierarchyField = MockDbSet<HierarchyField>.New();
            MockHierarchyFieldValue = MockDbSet<HierarchyFieldValue>.New();
            MockContentType = MockDbSet<ContentType>.New();
            MockProfitCenter = MockDbSet<ProfitCenter>.New();
            MockContentReductionTask = MockDbSet<ContentReductionTask>.New();
            MockContentPublicationRequest = MockDbSet<ContentPublicationRequest>.New();
            MockFileUpload = MockDbSet<FileUpload>.New();
            MockAuthenticationScheme = MockDbSet<AuthenticationScheme>.New();
            MockNameValueConfiguration = MockDbSet<NameValueConfiguration>.New();
            MockSftpAccount  = MockDbSet<SftpAccount>.New();
            MockFileDrop = MockDbSet<FileDrop>.New();
            MockFileDropUserPermissionGroup = MockDbSet<FileDropUserPermissionGroup>.New();
            MockFileDropDirectory = MockDbSet<FileDropDirectory>.New();
            MockFileDropFile = MockDbSet<FileDropFile>.New();
            #endregion

            #region From (public abstract class IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>)
            MockUserRoles = MockDbSet<IdentityUserRole<Guid>>.New();
            MockApplicationRole = MockDbSet<ApplicationRole>.New();
            MockRoleClaims = MockDbSet<IdentityRoleClaim<Guid>>.New();

            SetSystemRolesList();
            #endregion

            #region From (public abstract class IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken>)
            MockApplicationUser = MockDbSet<ApplicationUser>.New();
            MockUserClaims = MockDbSet<IdentityUserClaim<Guid>>.New();
            MockUserLogins = MockDbSet<IdentityUserLogin<Guid>>.New();
            MockUserTokens = MockDbSet<IdentityUserToken<Guid>>.New();
            #endregion
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("xyz");
            var opt = optionsBuilder.Options;
            //optionsBuilder.UseInMemoryDatabase(databaseName: "Products Test");
        }

        /// <summary>
        /// Creates all of the member objects of type Mock<DbSet<...>>. 
        /// </summary>
        /// <returns></returns>
        public void HookUpNavigationProperties()
        {
            MockContentPublicationRequest.Setup(d => d.Add(It.IsAny<ContentPublicationRequest>())).Callback<ContentPublicationRequest>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockContentPublicationRequest.As<IHasList<ContentPublicationRequest>>().Object.DataList.Add(s);
                MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(MockContentPublicationRequest.Object, "ApplicationUserId", ApplicationUser);
                MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(MockContentPublicationRequest.Object, "RootContentItemId", RootContentItem);
            });
            MockContentPublicationRequest.Setup(d => d.AddRange(It.IsAny<IEnumerable<ContentPublicationRequest>>())).Callback<IEnumerable<ContentPublicationRequest>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            MockContentReductionTask.Setup(d => d.Add(It.IsAny<ContentReductionTask>())).Callback<ContentReductionTask>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                s.CreateDateTimeUtc = s.ReductionStatus == ReductionStatusEnum.Replaced
                    ? DateTime.FromFileTimeUtc(50)
                    : DateTime.FromFileTimeUtc(200);
                MockContentReductionTask.As<IHasList<ContentReductionTask>>().Object.DataList.Add(s);
                MockDbSet<ContentReductionTask>.AssignNavigationProperty(MockContentReductionTask.Object, "ApplicationUserId", ApplicationUser);
                MockDbSet<ContentReductionTask>.AssignNavigationProperty(MockContentReductionTask.Object, "SelectionGroupId", SelectionGroup);
                MockDbSet<ContentReductionTask>.AssignNavigationProperty(MockContentReductionTask.Object, "ContentPublicationRequestId", ContentPublicationRequest);
            });
            MockContentReductionTask.Setup(d => d.AddRange(It.IsAny<IEnumerable<ContentReductionTask>>())).Callback<IEnumerable<ContentReductionTask>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            MockRootContentItem.Setup(d => d.Add(It.IsAny<RootContentItem>())).Callback<RootContentItem>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockRootContentItem.As<IHasList<RootContentItem>>().Object.DataList.Add(s);
                MockDbSet<RootContentItem>.AssignNavigationProperty(MockRootContentItem.Object, "ContentTypeId", ContentType);
                MockDbSet<RootContentItem>.AssignNavigationProperty(MockRootContentItem.Object, "ClientId", Client);
            });
            MockRootContentItem.Setup(d => d.AddRange(It.IsAny<IEnumerable<RootContentItem>>())).Callback< IEnumerable<RootContentItem>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            MockClient.Setup(d => d.Add(It.IsAny<Client>())).Callback<Client>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockClient.As<IHasList<Client>>().Object.DataList.Add(s);
                MockDbSet<Client>.AssignNavigationProperty(MockClient.Object, "ProfitCenterId", ProfitCenter);
                MockDbSet<Client>.AssignNavigationProperty(MockClient.Object, "ParentClientId", Client);
            });
            MockClient.Setup(d => d.AddRange(It.IsAny<IEnumerable<Client>>())).Callback<IEnumerable<Client>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            MockSelectionGroup.Setup(d => d.Add(It.IsAny<SelectionGroup>())).Callback<SelectionGroup>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockSelectionGroup.As<IHasList<SelectionGroup>>().Object.DataList.Add(s);
                MockDbSet<SelectionGroup>.AssignNavigationProperty(MockSelectionGroup.Object, "RootContentItemId", RootContentItem);
            });
            MockSelectionGroup.Setup(d => d.AddRange(It.IsAny<IEnumerable<SelectionGroup>>())).Callback< IEnumerable<SelectionGroup>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            MockUserRoleInClient.Setup(d => d.Add(It.IsAny<UserRoleInClient>())).Callback<UserRoleInClient>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockUserRoleInClient.As<IHasList<UserRoleInClient>>().Object.DataList.Add(s);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(MockUserRoleInClient.Object, "ClientId", Client);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(MockUserRoleInClient.Object, "UserId", ApplicationUser);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(MockUserRoleInClient.Object, "RoleId", Roles);
            });
            MockUserRoleInClient.Setup(d => d.AddRange(It.IsAny<IEnumerable<UserRoleInClient>>())).Callback<IEnumerable<UserRoleInClient>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            MockUserRoleInRootContentItem.Setup(d => d.Add(It.IsAny<UserRoleInRootContentItem>())).Callback<UserRoleInRootContentItem>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockUserRoleInRootContentItem.As<IHasList<UserRoleInRootContentItem>>().Object.DataList.Add(s);
                MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(MockUserRoleInRootContentItem.Object, "RootContentItemId", RootContentItem);
                MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(MockUserRoleInRootContentItem.Object, "RoleId", ApplicationRole);
                MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(MockUserRoleInRootContentItem.Object, "UserId", ApplicationUser);
            });
            MockUserRoleInRootContentItem.Setup(d => d.AddRange(It.IsAny<IEnumerable<UserRoleInRootContentItem>>())).Callback<IEnumerable<UserRoleInRootContentItem>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            MockUserInSelectionGroup.Setup(d => d.Add(It.IsAny<UserInSelectionGroup>())).Callback<UserInSelectionGroup>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockUserInSelectionGroup.As<IHasList<UserInSelectionGroup>>().Object.DataList.Add(s);
                MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<SelectionGroup>(MockUserInSelectionGroup.Object, "SelectionGroupId", SelectionGroup);
                MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<ApplicationUser>(MockUserInSelectionGroup.Object, "UserId", ApplicationUser);
            });
            MockUserInSelectionGroup.Setup(d => d.AddRange(It.IsAny<IEnumerable<UserInSelectionGroup>>())).Callback<IEnumerable<UserInSelectionGroup>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            MockApplicationUser.Setup(d => d.Add(It.IsAny<ApplicationUser>())).Callback<ApplicationUser>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockApplicationUser.As<IHasList<ApplicationUser>>().Object.DataList.Add(s);
                MockDbSet<ApplicationUser>.AssignNavigationProperty<AuthenticationScheme>(MockApplicationUser.Object, "AuthenticationSchemeId", AuthenticationScheme);
                s.SftpAccounts = new List<SftpAccount>();
            });
            MockApplicationUser.Setup(d => d.AddRange(It.IsAny<IEnumerable<ApplicationUser>>())).Callback<IEnumerable<ApplicationUser>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            MockSftpAccount.Setup(d => d.Add(It.IsAny<SftpAccount>())).Callback<SftpAccount>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockSftpAccount.As<IHasList<SftpAccount>>().Object.DataList.Add(s);
                MockDbSet<SftpAccount>.AssignNavigationProperty<ApplicationUser>(MockSftpAccount.Object, "ApplicationUserId", ApplicationUser);
                MockDbSet<SftpAccount>.AssignNavigationProperty<FileDropUserPermissionGroup>(MockSftpAccount.Object, "FileDropUserPermissionGroupId", FileDropUserPermissionGroup);
                MockDbSet<SftpAccount>.AssignNavigationProperty<FileDrop>(MockSftpAccount.Object, "FileDropId", FileDrop);
                if (s.ApplicationUser != null)
                {
                    Add(s);
                }
            });
            MockSftpAccount.Setup(d => d.AddRange(It.IsAny<IEnumerable<SftpAccount>>())).Callback<IEnumerable<SftpAccount>>(s =>
            {
                foreach (SftpAccount account in s)
                {
                    Add(account);
                }
            });

            MockFileDropUserPermissionGroup.Setup(d => d.Add(It.IsAny<FileDropUserPermissionGroup>())).Callback<FileDropUserPermissionGroup>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockFileDropUserPermissionGroup.As<IHasList<FileDropUserPermissionGroup>>().Object.DataList.Add(s);
                MockDbSet<FileDropUserPermissionGroup>.AssignNavigationProperty<FileDrop>(MockFileDropUserPermissionGroup.Object, "FileDropId", FileDrop);
            });
            MockFileDropUserPermissionGroup.Setup(d => d.AddRange(It.IsAny<IEnumerable<FileDropUserPermissionGroup>>())).Callback<IEnumerable<FileDropUserPermissionGroup>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            MockFileDrop.Setup(d => d.Add(It.IsAny<FileDrop>())).Callback<FileDrop>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockFileDrop.As<IHasList<FileDrop>>().Object.DataList.Add(s);
                MockDbSet<FileDrop>.AssignNavigationProperty<Client>(MockFileDrop.Object, "ClientId", Client);
            });
            MockFileDrop.Setup(d => d.AddRange(It.IsAny<IEnumerable<FileDrop>>())).Callback<IEnumerable<FileDrop>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            MockFileDropFile.Setup(d => d.Add(It.IsAny<FileDropFile>())).Callback<FileDropFile>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockFileDropFile.As<IHasList<FileDropFile>>().Object.DataList.Add(s);
                MockDbSet<FileDropFile>.AssignNavigationProperty<SftpAccount>(MockFileDropFile.Object, "CreatedByAccountId", SftpAccount);
                MockDbSet<FileDropFile>.AssignNavigationProperty<FileDropDirectory>(MockFileDropFile.Object, "DirectoryId", FileDropDirectory);
            });
            MockFileDropFile.Setup(d => d.AddRange(It.IsAny<IEnumerable<FileDropFile>>())).Callback<IEnumerable<FileDropFile>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            MockFileDropDirectory.Setup(d => d.Add(It.IsAny<FileDropDirectory>())).Callback<FileDropDirectory>(s =>
            {
                if (s.Id == default) s.Id = Guid.NewGuid();
                MockFileDropDirectory.As<IHasList<FileDropDirectory>>().Object.DataList.Add(s);
                MockDbSet<FileDropDirectory>.AssignNavigationProperty<FileDrop>(MockFileDropDirectory.Object, "FileDropId", FileDrop);
                MockDbSet<FileDropDirectory>.AssignNavigationProperty<FileDropDirectory>(MockFileDropDirectory.Object, "ParentDirectoryId", FileDropDirectory);
            });
            MockFileDropDirectory.Setup(d => d.AddRange(It.IsAny<IEnumerable<FileDropDirectory>>())).Callback<IEnumerable<FileDropDirectory>>(s =>
            {
                foreach (var instance in s)
                {
                    Add(instance);
                }
            });

            // Mock DbContext.Database.CommitTransaction() as no-op.
            Mock<IDbContextTransaction> DbTransaction = new Mock<IDbContextTransaction>();

            Mock<DatabaseFacade> MockDatabaseFacade = new Mock<DatabaseFacade>(this);
            MockDatabaseFacade.Setup(x => x.BeginTransaction()).Returns(DbTransaction.Object);
            MockDatabaseFacade.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(DbTransaction.Object);
            Mock.Get(this).SetupGet(x => x.Database).Returns(MockDatabaseFacade.Object);

            Users = ApplicationUser;
            Roles = ApplicationRole;
        }

        static readonly object LockObject = new object();
        private void SetSystemRolesList()
        {
            lock (LockObject)
            {
                bool ResetRoles = MapDbContextLib.Identity.ApplicationRole.RoleIds.Count != Enum.GetValues(typeof(RoleEnum)).Length;

                if (ResetRoles)
                {
                    MapDbContextLib.Identity.ApplicationRole.RoleIds.Clear();
                }

                foreach (RoleEnum Role in Enum.GetValues(typeof(RoleEnum)))
                {
                    ApplicationRole NewRole = new ApplicationRole { Id = TestUtil.MakeTestGuid((int)Role), RoleEnum = Role, Name = Role.ToString(), NormalizedName = Role.ToString().ToUpper(), DisplayName = Role.GetDisplayNameString() };

                    if (ResetRoles)
                    {
                        MapDbContextLib.Identity.ApplicationRole.RoleIds.Add(Role, NewRole.Id);
                    }

                    if (!ApplicationRole.Any(r => r.RoleEnum == Role))
                    {
                        ApplicationRole.Add(new MapDbContextLib.Identity.ApplicationRole 
                        { 
                            Id = Guid.NewGuid(), 
                            RoleEnum = Role, 
                            Name = Role.ToString(), 
                            DisplayName = Role.GetDisplayNameString(), 
                            NormalizedName = Role.GetDisplayNameString().ToUpper() 
                        });
                    }
                }
            }
        }
    }
}
