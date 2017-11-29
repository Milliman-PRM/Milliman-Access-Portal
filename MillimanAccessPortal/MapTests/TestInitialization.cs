using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Moq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;

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
        private static Mock<DbSet<IdentityUserRole<long>>> MoqIdentityUserRole = null;

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

            return GenerateControllerContext(TestUserPrincipal);
        }

        /// <summary>
        /// Initializes a ControllerContext as needed to construct a functioning controller. 
        /// </summary>
        /// <param name="TestUserPrincipal"></param>
        /// <returns></returns>
        internal static ControllerContext GenerateControllerContext(ClaimsPrincipal TestUserPrincipal)
        {
            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext() { User = TestUserPrincipal }
            };
        }

        public static void GenerateTestData(IEnumerable<DataSelection> DataSelections, Mock<ApplicationDbContext> MockContext)
        {
            foreach (DataSelection Selection in DataSelections.Distinct())
            {
                DataGenFunctionDict[Selection](MockContext);
            }
        }

        /// <summary>
        /// Construct and return a standard dataset to be used by most tests
        /// </summary>
        /// <returns>A tuple with the context and associated UserManager</returns>
        public static (Mock<ApplicationDbContext> MockContext, Mock<UserManager<ApplicationUser>> MockUserManager, DefaultAuthorizationService AuthorizationService, ILoggerFactory Logger, StandardQueries QueriesObject) 
            GenerateDependencies()
        {
            #region Instantiate context and contained DbSet objects
            // Had to implement a parameterless constructor in the context class, I hope this doesn't cause any problem in EF
            var MockContext = new Mock<ApplicationDbContext>();

            // Roles are standard throughout the system, should only be initialized once
            MoqApplicationRole = MockDbSet<ApplicationRole>.New(GetSystemRolesList());
            //MockContext.Setup(m => m.ApplicationRole).Returns(MoqApplicationRole.Object);
            MockContext.Object.ApplicationRole = MoqApplicationRole.Object;

            MoqApplicationUser = MockDbSet<ApplicationUser>.New(new List<ApplicationUser>());
            //MockContext.Setup(m => m.ApplicationUser).Returns(MoqApplicationUser.Object);
            MockContext.Object.ApplicationUser = MoqApplicationUser.Object;

            MoqContentType = MockDbSet<ContentType>.New(new List<ContentType>());
            //MockContext.Setup(m => m.ContentType).Returns(MoqContentType.Object);
            MockContext.Object.ContentType = MoqContentType.Object;

            MoqProfitCenter = MockDbSet<ProfitCenter>.New(new List<ProfitCenter>());
            //MockContext.Setup(m => m.ProfitCenter).Returns(MoqProfitCenter.Object);
            MockContext.Object.ProfitCenter = MoqProfitCenter.Object;

            MoqClient = MockDbSet<Client>.New(new List<Client>());
            //MockContext.Setup(m => m.Client).Returns(MoqClient.Object);
            MockContext.Object.Client = MoqClient.Object;

            MoqUserAuthorizationToClient = MockDbSet<UserRoleInClient>.New(new List<UserRoleInClient>());
            //MockContext.Setup(m => m.UserRoleInClient).Returns(MoqUserAuthorizationToClient.Object);
            MockContext.Object.UserRoleInClient = MoqUserAuthorizationToClient.Object;

            MoqRootContentItem = MockDbSet<RootContentItem>.New(new List<RootContentItem>());
            //MockContext.Setup(m => m.RootContentItem).Returns(MoqRootContentItem.Object);
            MockContext.Object.RootContentItem = MoqRootContentItem.Object;

            MoqHierarchyFieldValue = MockDbSet<HierarchyFieldValue>.New(new List<HierarchyFieldValue>());
            //MockContext.Setup(m => m.HierarchyFieldValue).Returns(MoqHierarchyFieldValue.Object);
            MockContext.Object.HierarchyFieldValue = MoqHierarchyFieldValue.Object;

            MoqHierarchyField = MockDbSet<HierarchyField>.New(new List<HierarchyField>());
            //MockContext.Setup(m => m.HierarchyField).Returns(MoqHierarchyField.Object);
            MockContext.Object.HierarchyField = MoqHierarchyField.Object;

            MoqContentItemUserGroup = MockDbSet<ContentItemUserGroup>.New(new List<ContentItemUserGroup>());
            //MockContext.Setup(m => m.ContentItemUserGroup).Returns(MoqContentItemUserGroup.Object);
            MockContext.Object.ContentItemUserGroup = MoqContentItemUserGroup.Object;

            MoqUserInContentItemUserGroup = MockDbSet<UserInContentItemUserGroup>.New(new List<UserInContentItemUserGroup>());
            //MockContext.Setup(m => m.UserInContentItemUserGroup).Returns(MoqUserInContentItemUserGroup.Object);
            MockContext.Object.UserInContentItemUserGroup = MoqUserInContentItemUserGroup.Object;

            MoqIdentityUserRole = MockDbSet<IdentityUserRole<long>>.New(new List<IdentityUserRole<long>>());
            MockContext.Object.UserRoles = MoqIdentityUserRole.Object;
            #endregion

            #region Construct a UserManager<ApplicationUser> that binds to the users in the context
            Mock<IUserStore<ApplicationUser>> UserStore = MockUserStore.New(MockContext);
            Mock<UserManager<ApplicationUser>> MockUserManager = new Mock<UserManager<ApplicationUser>>(UserStore.Object, null, null, null, null, null, null, null, null);

            MockUserManager.Setup(m => m.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns<ClaimsPrincipal>(cp => UserStore.Object.FindByIdAsync(cp.Identity.Name, CancellationToken.None).Result.UserName);
            MockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync<ClaimsPrincipal, UserManager<ApplicationUser>, ApplicationUser>(cp => UserStore.Object.FindByIdAsync(cp.Identity.Name, CancellationToken.None).Result);
            // more Setups?
            #endregion

            ILoggerFactory Logger = new LoggerFactory();

            IAuthorizationPolicyProvider PolicyProvider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));
            AuthorizationOptions AuthOptions = new AuthorizationOptions();
            AuthOptions.InvokeHandlersAfterFailure = true;
            AuthOptions.DefaultPolicy = new AuthorizationPolicy(new IAuthorizationRequirement[] { new DenyAnonymousAuthorizationRequirement() }, new string[0]);

            AuthorizationHandler<MapAuthorizationRequirementBase> Handler = new MapAuthorizationHandler(MockContext.Object, MockUserManager.Object);
            I​Authorization​Handler​Provider Authorization​Handler​Provider = new Default​Authorization​Handler​Provider(new IAuthorizationHandler[] { Handler });
            IAuthorizationHandlerContextFactory AuthorizationContext = new Default​Authorization​Handler​Context​Factory();
            DefaultAuthorizationService AuthorizationService = new DefaultAuthorizationService(
                                                                            PolicyProvider,
                                                                            Authorization​Handler​Provider,
                                                                            Logger.CreateLogger<DefaultAuthorizationService>(),
                                                                            AuthorizationContext,
                                                                            new DefaultAuthorizationEvaluator(),
                                                                            new OptionsWrapper<AuthorizationOptions>(AuthOptions));

            StandardQueries QueriesObj = new StandardQueries(MockContext.Object, MockUserManager.Object);

            return (MockContext, MockUserManager, AuthorizationService, Logger, QueriesObj);
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
            #endregion

            #region Initialize UserRoles
            MoqIdentityUserRole.Object.AddRange(new List<IdentityUserRole<long>>
                {
                    new IdentityUserRole<long> { RoleId=1, UserId = 1},
                });
            #endregion
        }

        private static List<ApplicationRole> GetSystemRolesList()
        {
            List<ApplicationRole> ReturnList = new List<ApplicationRole>();

            foreach (RoleEnum x in Enum.GetValues(typeof(RoleEnum)))
            {
                ReturnList.Add(new ApplicationRole { Id = (long)x, RoleEnum = x, Name = x.ToString(), NormalizedName = x.ToString().ToUpper() });
            }
            return ReturnList;
        }
    }
}
