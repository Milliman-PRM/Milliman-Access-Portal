/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ContentReductionLib;
using TestResourcesLib;
using Moq;

namespace ContentReductionServiceTests
{
    public class MapDbJobMonitorTests
    {
        DateTime StartTime = DateTime.MaxValue;

        //private Mock<ApplicationDbContext> GenerateBasicTestData(Mock<ApplicationDbContext> MockDbContext)
        //{
        //    #region Initialize Users
        //    MockDbContext.Object.ApplicationUser.AddRange(new List<ApplicationUser>
        //        {
        //            new ApplicationUser {
        //                Id = 1,
        //                UserName = "test1",
        //                Email = "test1@example.com",
        //                Employer = "example",
        //                FirstName = "FN1",
        //                LastName = "LN1",
        //                NormalizedEmail = "test@example.com".ToUpper(),
        //                PhoneNumber = "3171234567"
        //            },
        //            new ApplicationUser {
        //                Id = 2,
        //                UserName = "test2",
        //                Email = "test2@example.com",
        //                Employer = "example",
        //                FirstName = "FN2",
        //                LastName = "LN2",
        //                NormalizedEmail = "test2@example.com".ToUpper(),
        //                PhoneNumber = "3171234567",
        //            },
        //            new ApplicationUser {
        //                Id = 3,
        //                UserName = "ClientAdmin1",
        //                Email = "clientadmin1@example2.com",
        //                Employer = "example",
        //                FirstName = "Client",
        //                LastName = "Admin1",
        //                NormalizedEmail = "clientadmin1@example2.com".ToUpper(),
        //                PhoneNumber = "3171234567",
        //            },
        //            new ApplicationUser {
        //                Id = 4,
        //                UserName = "test3",
        //                Email = "test3@example2.com",
        //                Employer = "example",
        //                FirstName = "FN3",
        //                LastName = "LN3",
        //                NormalizedEmail = "test3@example2.com".ToUpper(),
        //                PhoneNumber = "3171234567",
        //            },
        //            new ApplicationUser {
        //                Id = 5,
        //                UserName = "user5",
        //                Email = "user5@example.com",
        //                Employer = "example",
        //                FirstName = "FN5",
        //                LastName = "LN5",
        //                NormalizedEmail = "user5@example.com".ToUpper(),
        //                PhoneNumber = "1234567890",
        //            },
        //            new ApplicationUser {
        //                Id = 6,
        //                UserName = "user6",
        //                Email = "user6@example.com",
        //                Employer = "example",
        //                FirstName = "FN6",
        //                LastName = "LN6",
        //                NormalizedEmail = "user6@example.com".ToUpper(),
        //                PhoneNumber = "1234567890",
        //            },
        //    });
        //    #endregion

        //    #region Initialize ContentType
        //    MockDbContext.ContentType.AddRange(new List<ContentType>
        //        {
        //            new ContentType{ Id=1, Name="Qlikview", CanReduce=true },
        //        });
        //    #endregion

        //    #region Initialize ProfitCenters
        //    MockDbContext.ProfitCenter.AddRange(new List<ProfitCenter>
        //        {
        //            new ProfitCenter { Id=1, Name="Profit Center 1", ProfitCenterCode="pc1" },
        //            new ProfitCenter { Id=2, Name="Profit Center 2", ProfitCenterCode="pc2" },
        //        });
        //    #endregion

        //    #region Initialize UserRoleInProfitCenter
        //    MockDbContext.UserRoleInProfitCenter.AddRange(new List<UserRoleInProfitCenter>
        //    {
        //        new UserRoleInProfitCenter { Id=1, ProfitCenterId=1, UserId=3, RoleId=1 }
        //    });
        //    MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ApplicationRole>(MockDbContext.UserRoleInProfitCenter, "RoleId", MockDbContext.ApplicationRole);
        //    MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ProfitCenter>(MockDbContext.UserRoleInProfitCenter, "ProfitCenterId", MockDbContext.ProfitCenter);
        //    MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ApplicationUser>(MockDbContext.UserRoleInProfitCenter, "UserId", MockDbContext.ApplicationUser);
        //    #endregion

        //    #region Initialize Clients
        //    MockDbContext.Client.AddRange(new List<Client>
        //        {
        //            new Client { Id=1, Name="Name1", ClientCode="ClientCode1", ProfitCenterId=1, ParentClientId=null, AcceptedEmailDomainList=new string[] { "example.com" }  },
        //            new Client { Id=2, Name="Name2", ClientCode="ClientCode2", ProfitCenterId=1, ParentClientId=1,    AcceptedEmailDomainList=new string[] { "example.com" }  },
        //            new Client { Id=3, Name="Name3", ClientCode="ClientCode3", ProfitCenterId=1, ParentClientId=null, AcceptedEmailDomainList=new string[] { "example2.com" } },
        //            new Client { Id=4, Name="Name4", ClientCode="ClientCode4", ProfitCenterId=2, ParentClientId=null, AcceptedEmailDomainList=new string[] { "example2.com" } },
        //            new Client { Id=5, Name="Name5", ClientCode="ClientCode5", ProfitCenterId=1, ParentClientId=null, AcceptedEmailDomainList=new string[] { "example2.com" } },
        //            new Client { Id=6, Name="Name6", ClientCode="ClientCode6", ProfitCenterId=1, ParentClientId=1,    AcceptedEmailDomainList=new string[] { "example2.com" } },
        //            new Client { Id=7, Name="Name7", ClientCode="ClientCode7", ProfitCenterId=1, ParentClientId=null, AcceptedEmailDomainList=new string[] { "example.com" } },
        //            new Client { Id=8, Name="Name8", ClientCode="ClientCode8", ProfitCenterId=1, ParentClientId=7,    AcceptedEmailDomainList=new string[] { "example.com" } },
        //        });
        //    MockDbSet<Client>.AssignNavigationProperty<ProfitCenter>(MockDbContext.Client, "ProfitCenterId", MockDbContext.ProfitCenter);
        //    #endregion

        //    #region Initialize User associations with Clients
        //    /*
        //     * There has to be a UserClaim for each user who is associated with a client
        //     * 
        //     * The number of user claims will not necessarily match the number of UserRoleForClient records, 
        //     *      since a user can have multiple roles with a client
        //     */

        //    #region Initialize UserRoleInClient
        //    MockDbContext.UserRoleInClient.AddRange(new List<UserRoleInClient>
        //            {
        //                new UserRoleInClient { Id=1, ClientId=1, RoleId=2, UserId=1 },
        //                new UserRoleInClient { Id=2, ClientId=1, RoleId=1, UserId=3 },
        //                new UserRoleInClient { Id=3, ClientId=4, RoleId=1, UserId=3 },
        //                new UserRoleInClient { Id=4, ClientId=5, RoleId=1, UserId=3 },
        //                new UserRoleInClient { Id=5, ClientId=6, RoleId=1, UserId=3 },
        //                new UserRoleInClient { Id=6, ClientId=5, RoleId=5, UserId=2 },
        //                new UserRoleInClient { Id=7, ClientId=7, RoleId=1, UserId=3 },
        //                new UserRoleInClient { Id=8, ClientId=8, RoleId=1, UserId=3 },
        //                new UserRoleInClient { Id=9, ClientId=8, RoleId=3, UserId=5 },
        //                new UserRoleInClient { Id=10, ClientId=8, RoleId=3, UserId=6 },
        //            });
        //    MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(MockDbContext.UserRoleInClient, "ClientId", MockDbContext.Client);
        //    MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(MockDbContext.UserRoleInClient, "UserId", MockDbContext.ApplicationUser);
        //    MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(MockDbContext.UserRoleInClient, "RoleId", MockDbContext.ApplicationRole);
        //    #endregion

        //    #region Initialize UserClaims
        //    MockDbContext.UserClaims.AddRange(new List<IdentityUserClaim<long>>
        //        {
        //            new IdentityUserClaim<long>{ Id=1, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="1", UserId=3 },
        //            new IdentityUserClaim<long>{ Id=2, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="4", UserId=3 },
        //            new IdentityUserClaim<long>{ Id=3, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="5", UserId=3 },
        //            new IdentityUserClaim<long>{ Id=4, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="1", UserId=1 },
        //            new IdentityUserClaim<long>{ Id=5, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="5", UserId=2 },
        //            new IdentityUserClaim<long>{ Id=6, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="6", UserId=3 },
        //            new IdentityUserClaim<long>{ Id=7, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="7", UserId=3 },
        //            new IdentityUserClaim<long>{ Id=8, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="8", UserId=3 },
        //            new IdentityUserClaim<long>{ Id=9, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="8", UserId=5 },
        //            new IdentityUserClaim<long>{ Id=10, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="8", UserId=6 },
        //        });
        //    #endregion
        //    #endregion

        //    #region Initialize RootContentItem
        //    MockDbContext.RootContentItem.AddRange(new List<RootContentItem>
        //        {
        //            new RootContentItem{ Id=1, ClientId=1, ContentName="RootContent 1", ContentTypeId=1 },
        //            new RootContentItem{ Id=2, ClientId=2, ContentName="RootContent 2", ContentTypeId=1 },
        //            new RootContentItem{ Id=3, ClientId=8, ContentName="RootContent 3", ContentTypeId=1 },
        //        });
        //    MockDbSet<RootContentItem>.AssignNavigationProperty<ContentType>(MockDbContext.RootContentItem, "ContentTypeId", MockDbContext.ContentType);
        //    MockDbSet<RootContentItem>.AssignNavigationProperty<Client>(MockDbContext.RootContentItem, "ClientId", MockDbContext.Client);
        //    #endregion

        //    #region Initialize HierarchyField
        //    MockDbContext.HierarchyField.AddRange(new List<HierarchyField>
        //        {
        //            new HierarchyField { Id=1, RootContentItemId=1, FieldName="Field1", FieldDisplayName="DisplayName1", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
        //        });
        //    MockDbSet<HierarchyField>.AssignNavigationProperty<RootContentItem>(MockDbContext.HierarchyField, "RootContentItemId", MockDbContext.RootContentItem);
        //    #endregion

        //    #region Initialize HierarchyFieldValue
        //    MockDbContext.HierarchyFieldValue.AddRange(new List<HierarchyFieldValue>
        //        {
        //            new HierarchyFieldValue { Id=1, HierarchyFieldId=1,  Value="Value 1" },
        //        });
        //    MockDbSet<HierarchyFieldValue>.AssignNavigationProperty<HierarchyField>(MockDbContext.HierarchyFieldValue, "HierarchyFieldId", MockDbContext.HierarchyField);
        //    #endregion

        //    #region Initialize SelectionGroups
        //    MockDbContext.SelectionGroup.AddRange(new List<SelectionGroup>
        //        {
        //            new SelectionGroup { Id=1, ContentInstanceUrl="Folder1/File1", RootContentItemId=1, GroupName="Group1 For Content1" },
        //            new SelectionGroup { Id=2, ContentInstanceUrl="Folder1/File2", RootContentItemId=1, GroupName="Group2 For Content1" },
        //            new SelectionGroup { Id=3, ContentInstanceUrl="Folder2/File1", RootContentItemId=2, GroupName="Group1 For Content2" },
        //            new SelectionGroup { Id=4, ContentInstanceUrl="Folder3/File1", RootContentItemId=3, GroupName="Group1 For Content3" },
        //            new SelectionGroup { Id=5, ContentInstanceUrl="Folder3/File2", RootContentItemId=3, GroupName="Group2 For Content3" },
        //        });
        //    MockDbSet<SelectionGroup>.AssignNavigationProperty<RootContentItem>(MockDbContext.SelectionGroup, "RootContentItemId", MockDbContext.RootContentItem);
        //    #endregion

        //    #region Initialize UserInSelectionGroups
        //    MockDbContext.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
        //        {
        //            new UserInSelectionGroup { Id=1, SelectionGroupId=1, UserId=1 },
        //            new UserInSelectionGroup { Id=2, SelectionGroupId=4, UserId=3 },
        //        });
        //    MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<SelectionGroup>(MockDbContext.UserInSelectionGroup, "SelectionGroupId", MockDbContext.SelectionGroup);
        //    MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<ApplicationUser>(MockDbContext.UserInSelectionGroup, "UserId", MockDbContext.ApplicationUser);
        //    #endregion

        //    #region Initialize UserRoles
        //    MockDbContext.UserRoles.AddRange(new List<IdentityUserRole<long>>
        //        {
        //            new IdentityUserRole<long> { RoleId=((long) RoleEnum.Admin), UserId=1 },
        //        });
        //    #endregion

        //    #region Initialize UserRoleInRootContentItem
        //    MockDbContext.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
        //    {
        //        new UserRoleInRootContentItem { Id=1, RoleId=5, UserId=1, RootContentItemId=1 },
        //        new UserRoleInRootContentItem { Id=2, RoleId=5, UserId=3, RootContentItemId=3 },
        //        new UserRoleInRootContentItem { Id=3, RoleId=5, UserId=5, RootContentItemId=3 },
        //        new UserRoleInRootContentItem { Id=4, RoleId=3, UserId=5, RootContentItemId=3 },
        //        new UserRoleInRootContentItem { Id=5, RoleId=5, UserId=6, RootContentItemId=3 },
        //    });
        //    MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationRole>(MockDbContext.UserRoleInRootContentItem, "RoleId", MockDbContext.ApplicationRole);
        //    MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationUser>(MockDbContext.UserRoleInRootContentItem, "UserId", MockDbContext.ApplicationUser);
        //    MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<RootContentItem>(MockDbContext.UserRoleInRootContentItem, "RootContentItemId", MockDbContext.RootContentItem);
        //    #endregion
        //}


        [Fact]
        public void TestMethod1()
        {
            MapDbJobMonitor JobMonitor = new MapDbJobMonitor { UseMockForTesting = true,
                                                               InitializationFunc = InitializeTests.Initialize, };

            CancellationToken Token = new CancellationToken();
            Task MonitorTask = JobMonitor.Start(Token);
            while (!MonitorTask.IsCompleted)
            {
                Thread.Sleep(1000);
            }

            Assert.False(DateTime.UtcNow - StartTime > JobMonitor.TaskAgeBeforeExecution);

            Assert.NotEqual<TaskStatus>(MonitorTask.Status, TaskStatus.Faulted);
        }

        [Fact]
        public void CorrectStatusAfterCancelWhileIdle()
        {
            #region arrange
            MapDbJobMonitor JobMonitor = new MapDbJobMonitor
            {
                UseMockForTesting = true,
                InitializationFunc = InitializeTests.Initialize,
            };

            CancellationTokenSource TokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task MonitorTask = JobMonitor.Start(TokenSource.Token);
            Thread.Sleep(new TimeSpan(0, 0, 5));
            #endregion

            #region Assert
            Assert.Equal<TaskStatus>(MonitorTask.Status, TaskStatus.Running);
            #endregion

            #region Act again
            DateTime CancelTime = DateTime.UtcNow;
            TokenSource.Cancel();
            Task.WaitAll(new Task[] { MonitorTask }, new TimeSpan(0, 0, 40));
            #endregion

            #region Assert
            Assert.Equal<TaskStatus>(MonitorTask.Status, TaskStatus.RanToCompletion);
            Assert.True(DateTime.UtcNow - CancelTime < new TimeSpan(0,0,30), "MapDbJobMonitor took too long to be canceled while idle");
            #endregion
        }

    }
}
