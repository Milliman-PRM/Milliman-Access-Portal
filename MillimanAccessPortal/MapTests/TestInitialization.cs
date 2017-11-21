using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MapDbContextLib.Context;
using Moq;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Identity;

namespace MapTests
{
    /// <summary>
    /// Signals which data initialization methods should be run
    /// </summary>
    internal enum DataSelection
    {
        // Important: Keep this enum synchronized with Dictionary DataGenFunctionDict below
        Basic,
    }

    /// <summary>
    /// Methods to support common test initialization tasks
    /// </summary>
    internal class TestInitialization
    {
        // These must be declared at class scope so they can be referenced in any number of selected initialization methods
        private static Mock<DbSet<ApplicationRole>> MoqApplicationRole = null;
        private static Mock<DbSet<ApplicationUser>> MoqApplicationUser = null;
        private static Mock<DbSet<ContentType>> MoqContentType = null;
        private static Mock<DbSet<ProfitCenter>> MoqProfitCenter = null;
        private static Mock<DbSet<Client>> MoqClient = null;
        private static Mock<DbSet<UserRoleInClient>> MoqUserAuthorizationToClient = null;
        private static Mock<DbSet<RootContentItem>> MoqRootContentItem = null;
        private static Mock<DbSet<HierarchyFieldValue>> MoqHierarchyFieldValue = null;
        private static Mock<DbSet<HierarchyField>> MoqHierarchyField = null;
        private static Mock<DbSet<ContentItemUserGroup>> MoqContentItemUserGroup = null;
        private static Mock<DbSet<UserInContentItemUserGroup>> MoqUserInContentItemUserGroup = null;

        /// <summary>
        /// Associates each DataSelection enum value with the function that implements it
        /// </summary>
        private static Dictionary<DataSelection, Action<Mock<ApplicationDbContext>>> DataGenFunctionDict = new Dictionary<DataSelection, Action<Mock<ApplicationDbContext>>>
        {
            // Important: Keep this dictionary synchronized with enum DataSelection above
            { DataSelection.Basic, c => GenerateBasicTestData(ref c) }
        };

        /// <summary>
        /// Initializes a ControllerContext as needed to construct a functioning controller. 
        /// </summary>
        /// <param name="UserName">The user name to be passed to the controller</param>
        /// <returns></returns>
        internal static ControllerContext GenerateControllerContext(string UserName)
        {
            ClaimsPrincipal TestUserPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, UserName) }));

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext() { User = TestUserPrincipal }
            };
        }

        /// <summary>
        /// Construct and return a standard dataset to be used by most tests
        /// </summary>
        /// <returns></returns>
        public static Mock<ApplicationDbContext> GenerateTestDataset(IEnumerable<DataSelection> DataSelections)
        {
            #region Instantiate context and contained DbSet objects
            // Had to implement a parameterless constructor in the context class, I hope this doesn't cause any problem in EF
            var MockContext = new Mock<ApplicationDbContext>();

            // Roles are standard throughout the system, should only be initialized once
            MoqApplicationRole = MockDbSet<ApplicationRole>.New(GetSystemRolesList());
            MockContext.Setup(m => m.ApplicationRole).Returns(MoqApplicationRole.Object);

            MoqApplicationUser = MockDbSet<ApplicationUser>.New(new List<ApplicationUser>());
            MockContext.Setup(m => m.ApplicationUser).Returns(MoqApplicationUser.Object);

            MoqContentType = MockDbSet<ContentType>.New(new List<ContentType>());
            MockContext.Setup(m => m.ContentType).Returns(MoqContentType.Object);

            MoqProfitCenter = MockDbSet<ProfitCenter>.New(new List<ProfitCenter>());
            MockContext.Setup(m => m.ProfitCenter).Returns(MoqProfitCenter.Object);

            MoqClient = MockDbSet<Client>.New(new List<Client>());
            MockContext.Setup(m => m.Client).Returns(MoqClient.Object);

            MoqUserAuthorizationToClient = MockDbSet<UserRoleInClient>.New(new List<UserRoleInClient>());
            MockContext.Setup(m => m.UserRoleForClient).Returns(MoqUserAuthorizationToClient.Object);

            MoqRootContentItem = MockDbSet<RootContentItem>.New(new List<RootContentItem>());
            MockContext.Setup(m => m.RootContentItem).Returns(MoqRootContentItem.Object);

            MoqHierarchyFieldValue = MockDbSet<HierarchyFieldValue>.New(new List<HierarchyFieldValue>());
            MockContext.Setup(m => m.HierarchyFieldValue).Returns(MoqHierarchyFieldValue.Object);

            MoqHierarchyField = MockDbSet<HierarchyField>.New(new List<HierarchyField>());
            MockContext.Setup(m => m.HierarchyField).Returns(MoqHierarchyField.Object);

            MoqContentItemUserGroup = MockDbSet<ContentItemUserGroup>.New(new List<ContentItemUserGroup>());
            MockContext.Setup(m => m.ContentItemUserGroup).Returns(MoqContentItemUserGroup.Object);

            MoqUserInContentItemUserGroup = MockDbSet<UserInContentItemUserGroup>.New(new List<UserInContentItemUserGroup>());
            MockContext.Setup(m => m.UserRoleForContentItemUserGroup).Returns(MoqUserInContentItemUserGroup.Object);
            #endregion

            foreach (DataSelection Selection in DataSelections.Distinct())
            {
                DataGenFunctionDict[Selection](MockContext);
            }

            return MockContext;
        }

        private static void GenerateBasicTestData(ref Mock<ApplicationDbContext> MockContext)
        {
            #region Initialize Users
            MoqApplicationUser.Object.AddRange(new List<ApplicationUser>
                {
                    new ApplicationUser {Id=1, UserName="test1", Email="test1@example.com", Employer ="example", FirstName="FN1",
                                         LastName="LN1", NormalizedEmail="test@example.com".ToUpper(), PhoneNumber="3171234567"},
                    new ApplicationUser {Id=2, UserName="test2", Email="test2@example.com", Employer ="example", FirstName="FN2",
                                         LastName="LN2", NormalizedEmail ="test@example.com".ToUpper(), PhoneNumber="3171234567"},
                });
            #endregion

            #region Initialize ContentType
            MoqContentType.Object.AddRange(new List<ContentType>
                {
                    new ContentType{Id=1, Name="Qlikview", CanReduce=true},
                });
            #endregion

            #region Initialize ProfitCenters
            MoqProfitCenter.Object.AddRange(new List<ProfitCenter>
                {
                    new ProfitCenter {Id=1, Name="Profit Center 1", ProfitCenterCode="pc1" },
                    new ProfitCenter {Id=2, Name="Profit Center 2", ProfitCenterCode="pc2" },
                });
            #endregion

            #region Initialize Clients
            MoqClient.Object.AddRange(new List<Client>
                {
                    new Client {Id=1, Name="Name1", ClientCode="ClientCode1", ProfitCenterId=1, ParentClientId=null },
                    new Client {Id=2, Name="Name2", ClientCode="ClientCode2", ProfitCenterId=1, ParentClientId=1 },
                });
            MockDbSet<Client>.AssignNavigationProperty<ProfitCenter>(MoqClient, "ProfitCenterId", MoqProfitCenter);
            #endregion

            #region Initialize UserRoleForClient
            MoqUserAuthorizationToClient.Object.AddRange(new List<UserRoleInClient>
                {
                    new UserRoleInClient {Id = 1, ClientId=1, RoleId=2, UserId=1},
                });
            MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(MoqUserAuthorizationToClient, "ClientId", MoqClient);
            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(MoqUserAuthorizationToClient, "UserId", MoqApplicationUser);
            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(MoqUserAuthorizationToClient, "RoleId", MoqApplicationRole);
            #endregion

            #region Initialize RootContentItem
            MoqRootContentItem.Object.AddRange(new List<RootContentItem>
                {
                    new RootContentItem{Id = 1, ClientIdList=new long[]{1}, ContentName="RootContent 1"},
                    new RootContentItem{Id = 2, ClientIdList=new long[]{2}, ContentName="RootContent 2"},
                });
            MockDbSet<RootContentItem>.AssignNavigationProperty<ContentType>(MoqRootContentItem, "ContentTypeId", MoqContentType);
            #endregion

            #region Initialize HierarchyFieldValue
            MoqHierarchyFieldValue.Object.AddRange(new List<HierarchyFieldValue>
                {
                    new HierarchyFieldValue {Id = 1, HierarchyLevel=1, ParentHierarchyFieldValueId=null, RootContentItemId=1},
                });
            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty<RootContentItem>(MoqHierarchyFieldValue, "RootContentItemId", MoqRootContentItem);
            #endregion

            #region Initialize HierarchyField
            MoqHierarchyField.Object.AddRange(new List<HierarchyField>
                {
                    new HierarchyField {Id = 1, RootContentItemId=1, HierarchyLevel=0},
                });
            MockDbSet<HierarchyField>.AssignNavigationProperty<RootContentItem>(MoqHierarchyField, "RootContentItemId", MoqRootContentItem);
            #endregion

            #region Initialize ContentItemUserGroups
            MoqContentItemUserGroup.Object.AddRange(new List<ContentItemUserGroup>
                {
                    new ContentItemUserGroup {Id = 1, ClientId=1, ContentInstanceUrl="Folder1/File1", RootContentItemId=1, GroupName="Group1 For Content1"},
                    new ContentItemUserGroup {Id = 2, ClientId=1, ContentInstanceUrl="Folder1/File2", RootContentItemId=1, GroupName="Group2 For Content1"},
                    new ContentItemUserGroup {Id = 3, ClientId=2, ContentInstanceUrl="Folder2/File1", RootContentItemId=2, GroupName="Group1 For Content2"},
                });
            MockDbSet<ContentItemUserGroup>.AssignNavigationProperty<RootContentItem>(MoqContentItemUserGroup, "RootContentItemId", MoqRootContentItem);
            MockDbSet<ContentItemUserGroup>.AssignNavigationProperty<Client>(MoqContentItemUserGroup, "ClientId", MoqClient);
            #endregion

            #region Initialize UserInContentItemUserGroups
            MoqUserInContentItemUserGroup.Object.AddRange(new List<UserInContentItemUserGroup>
                {
                    new UserInContentItemUserGroup {Id = 1, ContentItemUserGroupId=1, UserId=1},
                });
            MockDbSet<UserInContentItemUserGroup>.AssignNavigationProperty<ContentItemUserGroup>(MoqUserInContentItemUserGroup, "ContentItemUserGroupId", MoqContentItemUserGroup);
            MockDbSet<UserInContentItemUserGroup>.AssignNavigationProperty<ApplicationUser>(MoqUserInContentItemUserGroup, "UserId", MoqApplicationUser);
            MockDbSet<UserInContentItemUserGroup>.AssignNavigationProperty<ApplicationRole>(MoqUserInContentItemUserGroup, "RoleId", MoqApplicationRole);
            #endregion
        }

        private static List<ApplicationRole> GetSystemRolesList()
        {
            List<ApplicationRole> ReturnList = new List<ApplicationRole>();

            foreach (var x in ApplicationRole.MapRoles)
            {
                ReturnList.Add(new ApplicationRole { Id = (long)x.Key, RoleEnum = x.Key, Name = x.Value, NormalizedName = x.Value.ToString() });
            }
            return ReturnList;
        }
    }
}
