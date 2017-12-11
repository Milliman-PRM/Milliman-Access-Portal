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
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;
using AuditLogLib;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

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

        public Mock<RoleManager<ApplicationRole>> MockRoleManager { get; set;  }
        public RoleManager<ApplicationRole> RoleManagerObject { get => MockRoleManager.Object; }

        public IOptions<QlikviewConfig> QvConfig { get; set; }

        public DefaultAuthorizationService AuthorizationService { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public AuditLogger AuditLogger { get; set; }

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
            AuditLoggerConfiguration auditLogConfig = new AuditLogLib.AuditLoggerConfiguration();
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
            MockUserManager = MapTests.MockUserManager.New(MockDbContext);
            MockRoleManager = GenerateRoleManager(MockDbContext);
            LoggerFactory = new LoggerFactory();
            AuthorizationService = GenerateAuthorizationService(DbContextObject, UserManagerObject, LoggerFactory);
            QueriesObj = new StandardQueries(DbContextObject, UserManagerObject);
            QvConfig = BuildQvConfig();
        }

        private IOptions<QlikviewConfig> BuildQvConfig()
        {
            var configurationBuilder = new ConfigurationBuilder();
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            // Determine location to fetch the configuration
            switch (environmentName)
            {
                case "CI":
                case "Production": // Get configuration from Azure Key Vault for Production
                    configurationBuilder.AddJsonFile(path: $"AzureKeyVault.{environmentName}.json", optional: false);

                    var built = configurationBuilder.Build();

                    var store = new X509Store(StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var cert = store.Certificates.Find(X509FindType.FindByThumbprint, built["AzureCertificateThumbprint"], false);

                    configurationBuilder.AddAzureKeyVault(
                        built["AzureVaultName"],
                        built["AzureClientID"],
                        cert.OfType<X509Certificate2>().Single());
                    break;
                    
                default: // Get connection string from user secrets in Development (ASPNETCORE_ENVIRONMENT is not set during local unit tests)
                    configurationBuilder.AddUserSecrets<TestInitialization>();
                    break;
            }

            var configuration = configurationBuilder.Build();
            
            return Options.Create<QlikviewConfig>(new QlikviewConfig
            {
                QvServerHost = configuration["QvServerHost"],
                QvServerAdminUserAuthenticationDomain = configuration["QvServerAdminUserAuthenticationDomain"],
                QvServerAdminUserName = configuration["QvServerAdminUserName"],
                QvServerAdminUserPassword = configuration["QvServerAdminUserPassword"]
            });
        }

        private Mock<ApplicationDbContext> GenerateDbContext()
        {
            // Had to implement a parameterless constructor in the context class, I hope this doesn't cause any problem in EF
            Mock<ApplicationDbContext> ReturnMockContext = new Mock<ApplicationDbContext>();
            ReturnMockContext.Object.ApplicationRole = MockDbSet<ApplicationRole>.New(GetSystemRolesList()).Object;
            ReturnMockContext.Object.ApplicationUser = MockDbSet<ApplicationUser>.New(new List<ApplicationUser>()).Object;
            ReturnMockContext.Object.ContentType = MockDbSet<ContentType>.New(new List<ContentType>()).Object;
            ReturnMockContext.Object.ProfitCenter = MockDbSet<ProfitCenter>.New(new List<ProfitCenter>()).Object;
            ReturnMockContext.Object.UserRoleInProfitCenter = MockDbSet<UserRoleInProfitCenter>.New(new List<UserRoleInProfitCenter>()).Object;
            ReturnMockContext.Object.Client = MockDbSet<Client>.New(new List<Client>()).Object;
            ReturnMockContext.Object.UserRoleInClient = MockDbSet<UserRoleInClient>.New(new List<UserRoleInClient>()).Object;
            ReturnMockContext.Object.RootContentItem = MockDbSet<RootContentItem>.New(new List<RootContentItem>()).Object;
            ReturnMockContext.Object.HierarchyFieldValue = MockDbSet<HierarchyFieldValue>.New(new List<HierarchyFieldValue>()).Object;
            ReturnMockContext.Object.HierarchyField = MockDbSet<HierarchyField>.New(new List<HierarchyField>()).Object;
            ReturnMockContext.Object.ContentItemUserGroup = MockDbSet<ContentItemUserGroup>.New(new List<ContentItemUserGroup>()).Object;
            ReturnMockContext.Object.UserInContentItemUserGroup = MockDbSet<UserInContentItemUserGroup>.New(new List<UserInContentItemUserGroup>()).Object;
            ReturnMockContext.Object.UserRoles = MockDbSet<IdentityUserRole<long>>.New(new List<IdentityUserRole<long>>()).Object;
            ReturnMockContext.Object.UserRoleInRootContentItem = MockDbSet<UserRoleInRootContentItem>.New(new List<UserRoleInRootContentItem>()).Object;
            ReturnMockContext.Object.UserClaims = MockDbSet<IdentityUserClaim<long>>.New(new List<IdentityUserClaim<long>>()).Object;
            ReturnMockContext.Object.Users = ReturnMockContext.Object.ApplicationUser;
            ReturnMockContext.Object.Roles = ReturnMockContext.Object.ApplicationRole;

            return ReturnMockContext;
        }

        /// <summary>
        /// Perform mocking operations for the MockRoleManager
        /// </summary>
        /// <param name="MockDbContextArg"></param>
        /// <returns></returns>
        private Mock<RoleManager<ApplicationRole>> GenerateRoleManager(Mock<ApplicationDbContext> MockDbContextArg)
        {
            Mock<IRoleStore<ApplicationRole>> NewRoleStore = MockRoleStore.NewStore(MockDbContextArg);
            Mock<RoleManager<ApplicationRole>> ReturnMockRoleManager = new Mock<RoleManager<ApplicationRole>>(NewRoleStore.Object, null, null, null, null);
            
            ReturnMockRoleManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).Returns<ApplicationRole>(role => NewRoleStore.Object.FindByIdAsync(role.Id.ToString(), CancellationToken.None));
            ReturnMockRoleManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).Returns<ApplicationRole>(role => NewRoleStore.Object.FindByNameAsync(role.Name, CancellationToken.None));

            return ReturnMockRoleManager;
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
                    new ApplicationUser {Id=3, UserName="ClientAdmin1", Email="clientadmin1@example.com", Employer="example", FirstName="Client",
                                         LastName="Admin1", NormalizedEmail="clientadmin1@example.com".ToUpper(), PhoneNumber="3171234567"}
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

            #region Initialize UserRoleInProfitCenter
            DbContextObject.UserRoleInProfitCenter.AddRange(new List<UserRoleInProfitCenter>
            {
                new UserRoleInProfitCenter {Id=1, ProfitCenterId=1, UserId=3, RoleId=1}
            });
            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInProfitCenter, "RoleId", DbContextObject.ApplicationRole);
            #endregion

            #region Initialize Clients
            DbContextObject.Client.AddRange(new List<Client>
                {
                    new Client {Id=1, Name="Name1", ClientCode="ClientCode1", ProfitCenterId=1, ParentClientId=null },
                    new Client {Id=2, Name="Name2", ClientCode="ClientCode2", ProfitCenterId=1, ParentClientId=1 },
                    new Client {Id=3, Name="Name3", ClientCode="ClientCode3", ProfitCenterId=1, ParentClientId=null},
                    new Client {Id=4, Name="Name4", ClientCode="ClientCode4", ProfitCenterId=2, ParentClientId=3}
                });
            MockDbSet<Client>.AssignNavigationProperty<ProfitCenter>(DbContextObject.Client, "ProfitCenterId", DbContextObject.ProfitCenter);
            #endregion

            #region Initialize User associations with Clients
                /*
                 * There has to be a UserClaim for each user who is associated with a client
                 * 
                 * The number of user claims will not necessarily match the number of UserRoleForClient records, 
                 *      since a user can have multiple roles with a client
                 */
            
                #region Initialize UserRoleForClient
                DbContextObject.UserRoleInClient.AddRange(new List<UserRoleInClient>
                    {
                        new UserRoleInClient {Id = 1, ClientId=1, RoleId=2, UserId=1},
                        new UserRoleInClient {Id = 2, ClientId=1, RoleId=1, UserId=3},
                        new UserRoleInClient {Id=3, ClientId=4, RoleId=1, UserId=3}
                    });
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(DbContextObject.UserRoleInClient, "ClientId", DbContextObject.Client);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInClient, "UserId", DbContextObject.ApplicationUser);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInClient, "RoleId", DbContextObject.ApplicationRole);
                #endregion

                #region Initialize UserClaims
                DbContextObject.UserClaims.AddRange(new List<IdentityUserClaim<long>>
                {
                    new IdentityUserClaim<long>{ Id =1, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = "1", UserId = 3 },
                    new IdentityUserClaim<long>{ Id =2, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = "4", UserId = 3 },
                    new IdentityUserClaim<long>{ Id = 3, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = "1", UserId = 1}
                });
                #endregion
            #endregion 

            #region Initialize RootContentItem
            DbContextObject.RootContentItem.AddRange(new List<RootContentItem>
                {
                    new RootContentItem{Id = 1, ClientIdList=new long[]{1}, ContentName="RootContent 1", ContentTypeId = 1},
                    new RootContentItem{Id = 2, ClientIdList=new long[]{2}, ContentName="RootContent 2", ContentTypeId = 1},
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

            #region Initialize UserRoleInRootContentItem
            DbContextObject.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
            {
                new UserRoleInRootContentItem {Id = 1, RoleId = 5, UserId = 1, RootContentItemId = 1}
            });
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInRootContentItem, "RoleId", DbContextObject.ApplicationRole);
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInRootContentItem, "UserId", DbContextObject.ApplicationUser);
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<RootContentItem>(DbContextObject.UserRoleInRootContentItem, "RootContentItemId", DbContextObject.RootContentItem);
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
