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
using QlikviewLib;

namespace MapTests
{
    /// <summary>
    /// Signals which data initialization methods should be run
    /// </summary>
    internal enum DataSelection
    {
        // Important: Keep this enum synchronized with Dictionary DataGenFunctionDict in the constructor
        Basic,
    }

    /// <summary>
    /// Methods to support common test initialization tasks
    /// </summary>
    internal class TestInitialization
    {
        #region declarations of managed dependencies
        public Mock<ApplicationDbContext> MockDbContext { get; set; }
        public ApplicationDbContext DbContextObject { get => MockDbContext.Object; }

        public Mock<UserManager<ApplicationUser>> MockUserManager { get; set; }
        public UserManager<ApplicationUser> UserManagerObject { get => MockUserManager.Object; }

        public Mock<IOptions<QlikviewConfig>> MockQlikViewConfig { get; set; }
        public IOptions<QlikviewConfig> QlikViewConfigObject { get => MockQlikViewConfig.Object; }

        public DefaultAuthorizationService AuthorizationService { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public StandardQueries QueriesObj { get; set; }
        #endregion
        
        /// <summary>
        /// Associates each DataSelection enum value with the function that implements it
        /// </summary>
        private Dictionary<DataSelection, Action> DataGenFunctionDict;

        /// <summary>
        /// Constructor, initiates construction of functional but empty dependencies
        /// </summary>
        public TestInitialization()
        {
            GenerateDependencies();

            #region Configure AuditLogger
            AuditLogLib.AuditLoggerConfiguration auditLogConfig = new AuditLogLib.AuditLoggerConfiguration();
            auditLogConfig.AuditLogConnectionString = "";
            AuditLogLib.AuditLogger.Config = auditLogConfig;
            #endregion

            DataGenFunctionDict = new Dictionary<DataSelection, Action>
            {
                // Important: Keep this dictionary synchronized with enum DataSelection above
                { DataSelection.Basic, GenerateBasicTestData }
            };
        }
        /// <summary>
        /// Initializes a ControllerContext based on a user name. 
        /// </summary>
        /// <param name="UserAsUserName">The user to be impersonated in the ControllerContext</param>
        /// <returns></returns>
        internal static ControllerContext GenerateControllerContext(string UserAsUserName)
        {
            ClaimsPrincipal TestUserClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, UserAsUserName) }));

            return GenerateControllerContext(TestUserClaimsPrincipal);
        }

        /// <summary>
        /// Initializes a ControllerContext as needed to construct a functioning controller. 
        /// </summary>
        /// <param name="UserAsClaimsPrincipal">The user to be impersonated in the ControllerContext</param>
        /// <returns></returns>
        internal static ControllerContext GenerateControllerContext(ClaimsPrincipal UserAsClaimsPrincipal)
        {
            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext() { User = UserAsClaimsPrincipal }
            };
        }

        public void GenerateTestData(IEnumerable<DataSelection> DataSelections)
        {
            foreach (DataSelection Selection in DataSelections.Distinct())
            {
                DataGenFunctionDict[Selection]();
            }
        }

        /// <summary>
        /// Build dependencies used throughout MAP to be used as needed by tests
        /// </summary>
        private void GenerateDependencies()
        {
            MockDbContext = GenerateDbContext();
            MockUserManager = GenerateUserManager(MockDbContext);
            LoggerFactory = new LoggerFactory();
            AuthorizationService = GenerateAuthorizationService(DbContextObject, UserManagerObject, LoggerFactory);
            QueriesObj = new StandardQueries(DbContextObject, UserManagerObject);
            MockQlikViewConfig = new Mock<IOptions<QlikviewConfig>>();
        }

        private Mock<ApplicationDbContext> GenerateDbContext()
        {
            // Had to implement a parameterless constructor in the context class, I hope this doesn't cause any problem in EF
            Mock<ApplicationDbContext> ReturnMockContext = new Mock<ApplicationDbContext>();
            ReturnMockContext.Object.ApplicationRole = MockDbSet<ApplicationRole>.New(GetSystemRolesList()).Object;
            ReturnMockContext.Object.ApplicationUser = MockDbSet<ApplicationUser>.New(new List<ApplicationUser>()).Object;
            ReturnMockContext.Object.ContentType = MockDbSet<ContentType>.New(new List<ContentType>()).Object;
            ReturnMockContext.Object.ProfitCenter = MockDbSet<ProfitCenter>.New(new List<ProfitCenter>()).Object;
            ReturnMockContext.Object.Client = MockDbSet<Client>.New(new List<Client>()).Object;
            ReturnMockContext.Object.UserRoleInClient = MockDbSet<UserRoleInClient>.New(new List<UserRoleInClient>()).Object;
            ReturnMockContext.Object.RootContentItem = MockDbSet<RootContentItem>.New(new List<RootContentItem>()).Object;
            ReturnMockContext.Object.HierarchyFieldValue = MockDbSet<HierarchyFieldValue>.New(new List<HierarchyFieldValue>()).Object;
            ReturnMockContext.Object.HierarchyField = MockDbSet<HierarchyField>.New(new List<HierarchyField>()).Object;
            ReturnMockContext.Object.ContentItemUserGroup = MockDbSet<ContentItemUserGroup>.New(new List<ContentItemUserGroup>()).Object;
            ReturnMockContext.Object.UserInContentItemUserGroup = MockDbSet<UserInContentItemUserGroup>.New(new List<UserInContentItemUserGroup>()).Object;
            ReturnMockContext.Object.UserRoles = MockDbSet<IdentityUserRole<long>>.New(new List<IdentityUserRole<long>>()).Object;

            return ReturnMockContext;
        }

        private Mock<UserManager<ApplicationUser>> GenerateUserManager(Mock<ApplicationDbContext> MockDbContextArg)
        {
            Mock<IUserStore<ApplicationUser>> UserStore = MockUserStore.New(MockDbContextArg);
            Mock<UserManager<ApplicationUser>> ReturnMockUserManager = new Mock<UserManager<ApplicationUser>>(UserStore.Object, null, null, null, null, null, null, null, null);
            ReturnMockUserManager.Setup(m => m.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns<ClaimsPrincipal>(cp => UserStore.Object.FindByNameAsync(cp.Identity.Name, CancellationToken.None).Result.UserName);
            ReturnMockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync<ClaimsPrincipal, UserManager<ApplicationUser>, ApplicationUser>(cp => UserStore.Object.FindByNameAsync(cp.Identity.Name, CancellationToken.None).Result);
            ReturnMockUserManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync<string, UserManager<ApplicationUser>, ApplicationUser>(name => UserStore.Object.FindByNameAsync(name, CancellationToken.None).Result);
            // more Setups as needed

            return ReturnMockUserManager;
        }

        private DefaultAuthorizationService GenerateAuthorizationService(ApplicationDbContext ContextArg, UserManager<ApplicationUser> UserMgrArg, ILoggerFactory LoggerFactoryArg)
        {
            IAuthorizationPolicyProvider PolicyProvider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));
            AuthorizationOptions AuthOptions = new AuthorizationOptions();
            AuthOptions.InvokeHandlersAfterFailure = true;
            AuthOptions.DefaultPolicy = new AuthorizationPolicy(new IAuthorizationRequirement[] { new DenyAnonymousAuthorizationRequirement() }, new string[0]);

            AuthorizationHandler<MapAuthorizationRequirementBase> Handler = new MapAuthorizationHandler(ContextArg, UserMgrArg);
            I​Authorization​Handler​Provider Authorization​Handler​Provider = new Default​Authorization​Handler​Provider(new IAuthorizationHandler[] { Handler });
            IAuthorizationHandlerContextFactory AuthorizationContext = new Default​Authorization​Handler​Context​Factory();
            DefaultAuthorizationService ReturnService = new DefaultAuthorizationService(
                                                                            PolicyProvider,
                                                                            Authorization​Handler​Provider,
                                                                            LoggerFactoryArg.CreateLogger<DefaultAuthorizationService>(),
                                                                            AuthorizationContext,
                                                                            new DefaultAuthorizationEvaluator(),
                                                                            new OptionsWrapper<AuthorizationOptions>(AuthOptions));

            return ReturnService;
        }

        private void GenerateBasicTestData()
        {
            #region Initialize Users
            DbContextObject.ApplicationUser.AddRange(new List<ApplicationUser>
                {
                    new ApplicationUser {Id=1, UserName="test1", Email="test1@example.com", Employer ="example", FirstName="FN1",
                                         LastName="LN1", NormalizedEmail="test@example.com".ToUpper(), PhoneNumber="3171234567"},
                    new ApplicationUser {Id=2, UserName="test2", Email="test2@example.com", Employer ="example", FirstName="FN2",
                                         LastName="LN2", NormalizedEmail ="test@example.com".ToUpper(), PhoneNumber="3171234567"},
                });
            #endregion

            #region Initialize ContentType
            DbContextObject.ContentType.AddRange(new List<ContentType>
                {
                    new ContentType{Id=1, Name="Qlikview", CanReduce=true},
                });
            #endregion

            #region Initialize ProfitCenters
            DbContextObject.ProfitCenter.AddRange(new List<ProfitCenter>
                {
                    new ProfitCenter {Id=1, Name="Profit Center 1", ProfitCenterCode="pc1" },
                    new ProfitCenter {Id=2, Name="Profit Center 2", ProfitCenterCode="pc2" },
                });
            #endregion

            #region Initialize Clients
            DbContextObject.Client.AddRange(new List<Client>
                {
                    new Client {Id=1, Name="Name1", ClientCode="ClientCode1", ProfitCenterId=1, ParentClientId=null },
                    new Client {Id=2, Name="Name2", ClientCode="ClientCode2", ProfitCenterId=1, ParentClientId=1 },
                });
            MockDbSet<Client>.AssignNavigationProperty<ProfitCenter>(DbContextObject.Client, "ProfitCenterId", DbContextObject.ProfitCenter);
            #endregion

            #region Initialize UserRoleForClient
            DbContextObject.UserRoleInClient.AddRange(new List<UserRoleInClient>
                {
                    new UserRoleInClient {Id = 1, ClientId=1, RoleId=2, UserId=1},
                });
            MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(DbContextObject.UserRoleInClient, "ClientId", DbContextObject.Client);
            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInClient, "UserId", DbContextObject.ApplicationUser);
            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInClient, "RoleId", DbContextObject.ApplicationRole);
            #endregion

            #region Initialize RootContentItem
            DbContextObject.RootContentItem.AddRange(new List<RootContentItem>
                {
                    new RootContentItem{Id = 1, ClientIdList=new long[]{1}, ContentName="RootContent 1"},
                    new RootContentItem{Id = 2, ClientIdList=new long[]{2}, ContentName="RootContent 2"},
                });
            MockDbSet<RootContentItem>.AssignNavigationProperty<ContentType>(DbContextObject.RootContentItem, "ContentTypeId", DbContextObject.ContentType);
            #endregion

            #region Initialize HierarchyFieldValue
            DbContextObject.HierarchyFieldValue.AddRange(new List<HierarchyFieldValue>
                {
                    new HierarchyFieldValue {Id = 1, HierarchyLevel=1, ParentHierarchyFieldValueId=null, RootContentItemId=1},
                });
            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty<RootContentItem>(DbContextObject.HierarchyFieldValue, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize HierarchyField
            DbContextObject.HierarchyField.AddRange(new List<HierarchyField>
                {
                    new HierarchyField {Id = 1, RootContentItemId=1, HierarchyLevel=0},
                });
            MockDbSet<HierarchyField>.AssignNavigationProperty<RootContentItem>(DbContextObject.HierarchyField, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize ContentItemUserGroups
            DbContextObject.ContentItemUserGroup.AddRange(new List<ContentItemUserGroup>
                {
                    new ContentItemUserGroup {Id = 1, ClientId=1, ContentInstanceUrl="Folder1/File1", RootContentItemId=1, GroupName="Group1 For Content1"},
                    new ContentItemUserGroup {Id = 2, ClientId=1, ContentInstanceUrl="Folder1/File2", RootContentItemId=1, GroupName="Group2 For Content1"},
                    new ContentItemUserGroup {Id = 3, ClientId=2, ContentInstanceUrl="Folder2/File1", RootContentItemId=2, GroupName="Group1 For Content2"},
                });
            MockDbSet<ContentItemUserGroup>.AssignNavigationProperty<RootContentItem>(DbContextObject.ContentItemUserGroup, "RootContentItemId", DbContextObject.RootContentItem);
            MockDbSet<ContentItemUserGroup>.AssignNavigationProperty<Client>(DbContextObject.ContentItemUserGroup, "ClientId", DbContextObject.Client);
            #endregion

            #region Initialize UserInContentItemUserGroups
            DbContextObject.UserInContentItemUserGroup.AddRange(new List<UserInContentItemUserGroup>
                {
                    new UserInContentItemUserGroup {Id = 1, ContentItemUserGroupId=1, UserId=1},
                });
            MockDbSet<UserInContentItemUserGroup>.AssignNavigationProperty<ContentItemUserGroup>(DbContextObject.UserInContentItemUserGroup, "ContentItemUserGroupId", DbContextObject.ContentItemUserGroup);
            MockDbSet<UserInContentItemUserGroup>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserInContentItemUserGroup, "UserId", DbContextObject.ApplicationUser);
            #endregion

            #region Initialize UserRoles
            DbContextObject.UserRoles.AddRange(new List<IdentityUserRole<long>>
                {
                    new IdentityUserRole<long> { RoleId = (long)RoleEnum.Admin, UserId = 1},
                });
            #endregion
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
