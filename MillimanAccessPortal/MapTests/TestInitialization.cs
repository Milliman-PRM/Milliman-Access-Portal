/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE: Contains all test data definitions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ContentPublishing;
using MillimanAccessPortal.Services;
using Moq;
using QlikviewLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using TestResourcesLib;

namespace MapTests
{
    /// <summary>
    /// Signals which data initialization methods should be run
    /// </summary>
    internal enum DataSelection
    {
        // Important: Keep this enum synchronized with Dictionary DataGenFunctionDict in the constructor
        Basic,
        Reduction,
        Account,
        SystemAdmin,
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

        public Mock<AuditLogger> MockAuditLogger { get; set; }
        public AuditLogger AuditLoggerObject { get => MockAuditLogger.Object; }

        public Mock<IMessageQueue> MockMessageQueueService { get; set; }
        public IMessageQueue MessageQueueServicesObject { get => MockMessageQueueService.Object; }

        public Mock<IUploadHelper> MockUploadHelper { get; set; }
        public IUploadHelper UploadHelperObject { get => MockUploadHelper.Object; }

        public IConfiguration ConfigurationObject { get; set; }

        public Mock<IServiceProvider> MockServiceProvider { get; set; }
        public IServiceProvider ServiceProviderObject { get => MockServiceProvider.Object; }

        public IOptions<QlikviewConfig> QvConfig { get { return BuildQvConfig(); } }

        public DefaultAuthorizationService AuthorizationService { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public StandardQueries QueriesObj { get; set; }
        #endregion

        /// <summary>
        /// Associates each DataSelection enum value with the function that implements it
        /// </summary>
        private Dictionary<DataSelection, Action> DataGenFunctionDict;
        private string TestDataPath = Path.GetFullPath("../../../TestData");

        /// <summary>
        /// Constructor, initiates construction of functional but empty dependencies
        /// </summary>
        public TestInitialization()
        {
            GenerateDependencies();

            DataGenFunctionDict = new Dictionary<DataSelection, Action>
            {
                // Important: Keep this dictionary synchronized with enum DataSelection above
                { DataSelection.Basic, GenerateBasicTestData },
                { DataSelection.Reduction, GenerateReductionTestData },
                { DataSelection.Account, GenerateAccountTestData },
                { DataSelection.SystemAdmin, GenerateSystemAdminTestData },
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
                HttpContext = new DefaultHttpContext() { User=UserAsClaimsPrincipal },
                ActionDescriptor = new ControllerActionDescriptor { ActionName="Unit Test" }
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
            MockDbContext = MockMapDbContext.New();
            MockUserManager = TestResourcesLib.MockUserManager.New(MockDbContext);
            MockRoleManager = GenerateRoleManager(MockDbContext);
            MockMessageQueueService = GenerateMessageQueueService();
            MockUploadHelper = GenerateUploadHelper();
            LoggerFactory = new LoggerFactory();
            AuthorizationService = GenerateAuthorizationService(DbContextObject, UserManagerObject, LoggerFactory);
            MockAuditLogger = TestResourcesLib.MockAuditLogger.New();
            QueriesObj = new StandardQueries(DbContextObject, UserManagerObject, MockAuditLogger.Object);
            ConfigurationObject = GenerateConfiguration();
            MockServiceProvider = GenerateServiceProvider();
        }

        /// <summary>
        /// Prepare a mock IServiceProvider to fake security token validation
        /// We don't actually need to test this, since it's framework code, so returning true for ever validation should be okay.
        /// </summary>
        /// <returns></returns>
        private Mock<IServiceProvider> GenerateServiceProvider()
        {
            Mock<IServiceProvider> newServiceProvider = new Mock<IServiceProvider>();
            // IDataProtectionProvider dataProtectionProvider, IOptions<DataProtectionTokenProviderOptions> options
            Mock<IDataProtectionProvider> provider = new Mock<IDataProtectionProvider>();
            Mock<IOptions<DataProtectionTokenProviderOptions>> options = new Mock<IOptions<DataProtectionTokenProviderOptions>>() ;
            Mock<DataProtectorTokenProvider<ApplicationUser>> newTokenProvider = new Mock<DataProtectorTokenProvider<ApplicationUser>>(provider.Object, options.Object);

            // Validate tokens against TestResourcesLib.MockUserManager's static values
            newTokenProvider.Setup(m => m.ValidateAsync(It.IsAny<string>(), TestResourcesLib.MockUserManager.GoodToken, It.IsAny<UserManager<ApplicationUser>>(), It.IsAny<ApplicationUser>()))
                .Returns(Task.Run(() => true));
            newTokenProvider.Setup(m => m.ValidateAsync(It.IsAny<string>(), TestResourcesLib.MockUserManager.BadToken, It.IsAny<UserManager<ApplicationUser>>(), It.IsAny<ApplicationUser>()))
                .Returns(Task.Run(() => false));

            newServiceProvider.Setup(m => m.GetService(It.IsAny<Type>())).Returns(newTokenProvider.Object);

            return newServiceProvider;
        }

        private IOptions<QlikviewConfig> BuildQvConfig()
        {
            ConfigurationObject = GenerateConfiguration();

            return Options.Create(new QlikviewConfig
            {
                QvServerHost = ConfigurationObject["QvServerHost"],
                QvServerAdminUserAuthenticationDomain = ConfigurationObject["QvServerAdminUserAuthenticationDomain"],
                QvServerAdminUserName = ConfigurationObject["QvServerAdminUserName"],
                QvServerAdminUserPassword = ConfigurationObject["QvServerAdminUserPassword"],
                QvServerContentUriSubfolder = ConfigurationObject["QvServerContentUriSubfolder"],
                QdsQmsApiUrl = ConfigurationObject["QdsQmsApiUrl"],
                QvsQmsApiUrl = ConfigurationObject["QvsQmsApiUrl"]
            });
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

            ReturnMockRoleManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).Returns(async (string roleId) => await NewRoleStore.Object.FindByIdAsync(roleId, CancellationToken.None));
            ReturnMockRoleManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).Returns(async (string name) => await NewRoleStore.Object.FindByNameAsync(name, CancellationToken.None));

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

        private Mock<IMessageQueue> GenerateMessageQueueService()
        {
            Mock<IMessageQueue> ReturnObject = new Mock<IMessageQueue>();

            return ReturnObject;
        }

        private Mock<IUploadHelper> GenerateUploadHelper()
        {
            Mock<IUploadHelper> mock = new Mock<IUploadHelper>();

            ResumableInfo resumableInfo = null;
            mock.Setup(m => m.GetUploadStatus(It.IsAny<ResumableInfo>())).Returns<ResumableInfo>((x) =>
            {
                var chunkDirInfo = new DirectoryInfo(Path.Combine(TestDataPath, "Uploads", x.UID));
                var receivedChunks = new List<uint>();

                if (chunkDirInfo.Exists)
                {
                    receivedChunks.AddRange(chunkDirInfo.EnumerateFiles()
                        .Where(f => f.Exists && f.Length == x.ChunkSize)
                        .Select(f => Convert.ToUInt32(f.Name.Split('.')[0])));
                }

                return receivedChunks;
            });
            mock.Setup(m => m.OpenTempFile()).Returns(Stream.Null);
            mock.Setup(m => m.FinalizeUpload(It.IsAny<ResumableInfo>()));
            mock.Setup(m => m.GetOutputFilePath()).Returns(() =>
            {
                return Path.Combine(TestDataPath, $"{resumableInfo.UID}{resumableInfo.FileExt}");
            });

            return mock;
        }

        private IConfiguration GenerateConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder();
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            // Determine location to fetch the configuration
            switch (environmentName)
            {
                case "CI":
                    configurationBuilder.AddJsonFile(path: $"appsettings.{environmentName}.json", optional: false);
                    break;
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

            var ReturnConfiguration = configurationBuilder.Build();
            throw new ApplicationException("Built config: " + Newtonsoft.Json.JsonConvert.SerializeObject(ReturnConfiguration);
            return ReturnConfiguration;
        }

        private void GenerateBasicTestData()
        {
            #region Initialize Users
            DbContextObject.ApplicationUser.AddRange(new List<ApplicationUser>
                {
                    new ApplicationUser {
                        Id = TestUtil.MakeTestGuid(1),
                        UserName = "test1",
                        Email = "test1@example.com",
                        Employer = "example",
                        FirstName = "FN1",
                        LastName = "LN1",
                        NormalizedEmail = "test@example.com".ToUpper(),
                        PhoneNumber = "3171234567"
                    },
                    new ApplicationUser {
                        Id = TestUtil.MakeTestGuid(2),
                        UserName = "test2",
                        Email = "test2@example.com",
                        Employer = "example",
                        FirstName = "FN2",
                        LastName = "LN2",
                        NormalizedEmail = "test2@example.com".ToUpper(),
                        PhoneNumber = "3171234567",
                    },
                    new ApplicationUser {
                        Id = TestUtil.MakeTestGuid(3),
                        UserName = "ClientAdmin1",
                        Email = "clientadmin1@example2.com",
                        Employer = "example",
                        FirstName = "Client",
                        LastName = "Admin1",
                        NormalizedEmail = "clientadmin1@example2.com".ToUpper(),
                        PhoneNumber = "3171234567",
                    },
                    new ApplicationUser {
                        Id = TestUtil.MakeTestGuid(4),
                        UserName = "test3",
                        Email = "test3@example2.com",
                        Employer = "example",
                        FirstName = "FN3",
                        LastName = "LN3",
                        NormalizedEmail = "test3@example2.com".ToUpper(),
                        PhoneNumber = "3171234567",
                    },
                    new ApplicationUser {
                        Id = TestUtil.MakeTestGuid(5),
                        UserName = "user5",
                        Email = "user5@example.com",
                        Employer = "example",
                        FirstName = "FN5",
                        LastName = "LN5",
                        NormalizedEmail = "user5@example.com".ToUpper(),
                        PhoneNumber = "1234567890",
                    },
                    new ApplicationUser {
                        Id = TestUtil.MakeTestGuid(6),
                        UserName = "user6",
                        Email = "user6@example.com",
                        Employer = "example",
                        FirstName = "FN6",
                        LastName = "LN6",
                        NormalizedEmail = "user6@example.com".ToUpper(),
                        PhoneNumber = "1234567890",
                    },
            });
            #endregion

            #region Initialize ContentType
            DbContextObject.ContentType.AddRange(new List<ContentType>
                { 
                    new ContentType{ Id=TestUtil.MakeTestGuid(1), Name="Qlikview", CanReduce=true },
                });
            #endregion

            #region Initialize ProfitCenters
            DbContextObject.ProfitCenter.AddRange(new List<ProfitCenter>
                { 
                    new ProfitCenter { Id=TestUtil.MakeTestGuid(1), Name="Profit Center 1", ProfitCenterCode="pc1" },
                    new ProfitCenter { Id=TestUtil.MakeTestGuid(2), Name="Profit Center 2", ProfitCenterCode="pc2" },
                });
            #endregion

            #region Initialize UserRoleInProfitCenter
            DbContextObject.UserRoleInProfitCenter.AddRange(new List<UserRoleInProfitCenter>
            { 
                new UserRoleInProfitCenter { Id=TestUtil.MakeTestGuid(1), ProfitCenterId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3), RoleId=TestUtil.MakeTestGuid(1) }
            });
            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInProfitCenter, "RoleId", DbContextObject.ApplicationRole);
            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ProfitCenter>(DbContextObject.UserRoleInProfitCenter, "ProfitCenterId", DbContextObject.ProfitCenter);
            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInProfitCenter, "UserId", DbContextObject.ApplicationUser);
            #endregion

            #region Initialize Clients
            DbContextObject.Client.AddRange(new List<Client>
                { 
                    new Client { Id=TestUtil.MakeTestGuid(1), Name="Name1", ClientCode="ClientCode1", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new string[] { "example.com" }  },
                    new Client { Id=TestUtil.MakeTestGuid(2), Name="Name2", ClientCode="ClientCode2", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=TestUtil.MakeTestGuid(1),    AcceptedEmailDomainList=new string[] { "example.com" }  },
                    new Client { Id=TestUtil.MakeTestGuid(3), Name="Name3", ClientCode="ClientCode3", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new string[] { "example2.com" } },
                    new Client { Id=TestUtil.MakeTestGuid(4), Name="Name4", ClientCode="ClientCode4", ProfitCenterId=TestUtil.MakeTestGuid(2), ParentClientId=null, AcceptedEmailDomainList=new string[] { "example2.com" } },
                    new Client { Id=TestUtil.MakeTestGuid(5), Name="Name5", ClientCode="ClientCode5", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new string[] { "example2.com" } },
                    new Client { Id=TestUtil.MakeTestGuid(6), Name="Name6", ClientCode="ClientCode6", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=TestUtil.MakeTestGuid(1),    AcceptedEmailDomainList=new string[] { "example2.com" } },
                    new Client { Id=TestUtil.MakeTestGuid(7), Name="Name7", ClientCode="ClientCode7", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new string[] { "example.com" } },
                    new Client { Id=TestUtil.MakeTestGuid(8), Name="Name8", ClientCode="ClientCode8", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=TestUtil.MakeTestGuid(7),    AcceptedEmailDomainList=new string[] { "example.com" } },
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
            
                #region Initialize UserRoleInClient
                DbContextObject.UserRoleInClient.AddRange(new List<UserRoleInClient>
                    { 
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(2), UserId=TestUtil.MakeTestGuid(1) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(4), RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(5), RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(5), ClientId=TestUtil.MakeTestGuid(6), RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(6), ClientId=TestUtil.MakeTestGuid(5), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(2) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(7), ClientId=TestUtil.MakeTestGuid(7), RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(8), ClientId=TestUtil.MakeTestGuid(8), RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(9), ClientId=TestUtil.MakeTestGuid(8), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(5) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(10), ClientId=TestUtil.MakeTestGuid(8), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(6) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(11), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(2), UserId=TestUtil.MakeTestGuid(2) }, // this record is intentionally without a respective claim
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(12), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1) },
                    });
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(DbContextObject.UserRoleInClient, "ClientId", DbContextObject.Client);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInClient, "UserId", DbContextObject.ApplicationUser);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInClient, "RoleId", DbContextObject.ApplicationRole);
                #endregion

                #region Initialize UserClaims
                DbContextObject.UserClaims.AddRange(new List<IdentityUserClaim<Guid>>
                { 
                    new IdentityUserClaim<Guid>{ Id=1, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(3) },
                    new IdentityUserClaim<Guid>{ Id=2, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(4).ToString(), UserId=TestUtil.MakeTestGuid(3) },
                    new IdentityUserClaim<Guid>{ Id=3, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(5).ToString(), UserId=TestUtil.MakeTestGuid(3) },
                    new IdentityUserClaim<Guid>{ Id=4, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(1) },
                    new IdentityUserClaim<Guid>{ Id=5, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(5).ToString(), UserId=TestUtil.MakeTestGuid(2) },
                    new IdentityUserClaim<Guid>{ Id=6, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(6).ToString(), UserId=TestUtil.MakeTestGuid(3) },
                    new IdentityUserClaim<Guid>{ Id=7, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(7).ToString(), UserId=TestUtil.MakeTestGuid(3) },
                    new IdentityUserClaim<Guid>{ Id=8, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(8).ToString(), UserId=TestUtil.MakeTestGuid(3) },
                    new IdentityUserClaim<Guid>{ Id=9, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(8).ToString(), UserId=TestUtil.MakeTestGuid(5) },
                    new IdentityUserClaim<Guid>{ Id=10, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(8).ToString(), UserId=TestUtil.MakeTestGuid(6) },
                    new IdentityUserClaim<Guid>{ Id=11, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(5) },
                });
                #endregion
            #endregion 

            #region Initialize RootContentItem
            DbContextObject.RootContentItem.AddRange(new List<RootContentItem>
                { 
                    new RootContentItem{ Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 1", ContentTypeId=TestUtil.MakeTestGuid(1) },
                    new RootContentItem{ Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(2), ContentName="RootContent 2", ContentTypeId=TestUtil.MakeTestGuid(1) },
                    new RootContentItem{ Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(8), ContentName="RootContent 3", ContentTypeId=TestUtil.MakeTestGuid(1) },
                    new RootContentItem{ Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 4", ContentTypeId=TestUtil.MakeTestGuid(1) },
                    new RootContentItem{ Id=TestUtil.MakeTestGuid(5), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 5", ContentTypeId=TestUtil.MakeTestGuid(1) },
                });
            MockDbSet<RootContentItem>.AssignNavigationProperty<ContentType>(DbContextObject.RootContentItem, "ContentTypeId", DbContextObject.ContentType);
            MockDbSet<RootContentItem>.AssignNavigationProperty<Client>(DbContextObject.RootContentItem, "ClientId", DbContextObject.Client);
            #endregion

            #region Initialize HierarchyField
            DbContextObject.HierarchyField.AddRange(new List<HierarchyField>
                {
                    new HierarchyField { Id=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName1", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                });
            MockDbSet<HierarchyField>.AssignNavigationProperty<RootContentItem>(DbContextObject.HierarchyField, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize HierarchyFieldValue
            DbContextObject.HierarchyFieldValue.AddRange(new List<HierarchyFieldValue>
                { 
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(1), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 1" },
                });
            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty<HierarchyField>(DbContextObject.HierarchyFieldValue, "HierarchyFieldId", DbContextObject.HierarchyField);
            #endregion

            #region Initialize SelectionGroups
            DbContextObject.SelectionGroup.AddRange(new List<SelectionGroup>
                {
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(1), ContentInstanceUrl="Folder1/File1", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group1 For Content1" },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(2), ContentInstanceUrl="Folder1/File2", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group2 For Content1" },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(3), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group1 For Content2" },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(4), ContentInstanceUrl="Folder3/File1", RootContentItemId=TestUtil.MakeTestGuid(3), GroupName="Group1 For Content3" },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(5), ContentInstanceUrl="Folder3/File2", RootContentItemId=TestUtil.MakeTestGuid(3), GroupName="Group2 For Content3" },
                });
            MockDbSet<SelectionGroup>.AssignNavigationProperty<RootContentItem>(DbContextObject.SelectionGroup, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize UserInSelectionGroups
            DbContextObject.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
                { 
                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(1), SelectionGroupId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },
                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(2), SelectionGroupId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(3) },
                });
            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<SelectionGroup>(DbContextObject.UserInSelectionGroup, "SelectionGroupId", DbContextObject.SelectionGroup);
            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserInSelectionGroup, "UserId", DbContextObject.ApplicationUser);
            #endregion

            #region Initialize UserRoles
            DbContextObject.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
                { 
                // TODO Undo this
                    //new IdentityUserRole<Guid> { RoleId=((long) RoleEnum.Admin), UserId=TestUtil.MakeTestGuid(1) },
                    new IdentityUserRole<Guid> { RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },
                });
            #endregion

            #region Initialize UserRoleInRootContentItem
            DbContextObject.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
            { 
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(2), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(3), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(3), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(5), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(4), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(5), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(5), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(6), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(6), RoleId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
            });
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInRootContentItem, "RoleId", DbContextObject.ApplicationRole);
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInRootContentItem, "UserId", DbContextObject.ApplicationUser);
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<RootContentItem>(DbContextObject.UserRoleInRootContentItem, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion
        }

        private void GenerateReductionTestData()
        {
            #region Initialize Users
            DbContextObject.ApplicationUser.AddRange(new List<ApplicationUser>
                {
                    new ApplicationUser { Id=TestUtil.MakeTestGuid(1), UserName="user1", Email="user1@example.com" },
                    new ApplicationUser { Id=TestUtil.MakeTestGuid(2), UserName="user2", Email="user2@example.com" },
                    new ApplicationUser { Id=TestUtil.MakeTestGuid(3), UserName="user3", Email="user3@example.com" },
            });
            #endregion

            #region Initialize ContentType
            DbContextObject.ContentType.AddRange(new List<ContentType>
                {
                    new ContentType{ Id=TestUtil.MakeTestGuid(1), Name="Qlikview", CanReduce=true },
                });
            #endregion

            #region Initialize ProfitCenters
            DbContextObject.ProfitCenter.AddRange(new List<ProfitCenter>
                {
                    new ProfitCenter { Id=TestUtil.MakeTestGuid(1), Name="Profit Center 1", ProfitCenterCode="pc1" },
                });
            #endregion

            #region Initialize Clients
            DbContextObject.Client.AddRange(new List<Client>
                {
                    new Client { Id=TestUtil.MakeTestGuid(1), Name="Client 1", ClientCode="C1", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new string[] { "example.com" }  },
                    new Client { Id=TestUtil.MakeTestGuid(2), Name="Client 2", ClientCode="C2", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new string[] { "example.com" }  },
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

            #region Initialize UserRoleInClient
            DbContextObject.UserRoleInClient.AddRange(new List<UserRoleInClient>
            {
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(1) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(1) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(2) },
            });
            MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(DbContextObject.UserRoleInClient, "ClientId", DbContextObject.Client);
            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInClient, "UserId", DbContextObject.ApplicationUser);
            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInClient, "RoleId", DbContextObject.ApplicationRole);
            #endregion

            #region Initialize UserClaims
            DbContextObject.UserClaims.AddRange(new List<IdentityUserClaim<Guid>>
                {
                    new IdentityUserClaim<Guid>{ Id=1, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(1) },
                    new IdentityUserClaim<Guid>{ Id=2, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(2) },
                });
            #endregion
            #endregion

            #region Initialize RootContentItem
            DbContextObject.RootContentItem.AddRange(new List<RootContentItem>
            {
                new RootContentItem{ Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 1", ContentTypeId=TestUtil.MakeTestGuid(1) },
                new RootContentItem{ Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 2", ContentTypeId=TestUtil.MakeTestGuid(1) },
                new RootContentItem{ Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 3", ContentTypeId=TestUtil.MakeTestGuid(1) },
            });
            MockDbSet<RootContentItem>.AssignNavigationProperty<ContentType>(DbContextObject.RootContentItem, "ContentTypeId", DbContextObject.ContentType);
            MockDbSet<RootContentItem>.AssignNavigationProperty<Client>(DbContextObject.RootContentItem, "ClientId", DbContextObject.Client);
            #endregion

            #region Initialize HierarchyField
            DbContextObject.HierarchyField.AddRange(new List<HierarchyField>
                {
                    new HierarchyField { Id=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName1", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                    new HierarchyField { Id=TestUtil.MakeTestGuid(2), RootContentItemId=TestUtil.MakeTestGuid(2), FieldName="Field2", FieldDisplayName="DisplayName2", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                    new HierarchyField { Id=TestUtil.MakeTestGuid(3), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName3", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                });
            MockDbSet<HierarchyField>.AssignNavigationProperty<RootContentItem>(DbContextObject.HierarchyField, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize HierarchyFieldValue
            DbContextObject.HierarchyFieldValue.AddRange(new List<HierarchyFieldValue>
                {
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(1), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 1" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(2), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 2" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(3), HierarchyFieldId=TestUtil.MakeTestGuid(2),  Value="Value 1" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(4), HierarchyFieldId=TestUtil.MakeTestGuid(2),  Value="Value 2" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(5), HierarchyFieldId=TestUtil.MakeTestGuid(3),  Value="Value 1" },
                });
            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty<HierarchyField>(DbContextObject.HierarchyFieldValue, "HierarchyFieldId", DbContextObject.HierarchyField);
            #endregion

            #region Initialize SelectionGroups
            DbContextObject.SelectionGroup.AddRange(new List<SelectionGroup>
                {
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(1), ContentInstanceUrl="Folder1/File1", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group1 For Content1", SelectedHierarchyFieldValueList=new Guid[] { } },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(2), ContentInstanceUrl="Folder1/File2", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group2 For Content1", SelectedHierarchyFieldValueList=new Guid[] { } },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(3), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(2), GroupName="Group1 For Content2", SelectedHierarchyFieldValueList=new Guid[] { } },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(4), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(3), GroupName="Group1 For Content3", SelectedHierarchyFieldValueList=new Guid[] { } },
                });
            MockDbSet<SelectionGroup>.AssignNavigationProperty<RootContentItem>(DbContextObject.SelectionGroup, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize UserInSelectionGroups
            DbContextObject.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
                {
                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(1), SelectionGroupId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(2) },
                });
            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<SelectionGroup>(DbContextObject.UserInSelectionGroup, "SelectionGroupId", DbContextObject.SelectionGroup);
            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserInSelectionGroup, "UserId", DbContextObject.ApplicationUser);
            #endregion

            #region Initialize UserRoles
            DbContextObject.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
                {
                // TODO Undo this
                    //new IdentityUserRole<Guid> { RoleId=((long) RoleEnum.Admin), UserId=TestUtil.MakeTestGuid(1) },
                    new IdentityUserRole<Guid> { RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },
                });
            #endregion

            #region Initialize UserRoleInRootContentItem
            DbContextObject.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
            {
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(2), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(3), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(4), RoleId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(5), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(6), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(2), RootContentItemId=TestUtil.MakeTestGuid(1) },
            });
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInRootContentItem, "RoleId", DbContextObject.ApplicationRole);
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInRootContentItem, "UserId", DbContextObject.ApplicationUser);
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<RootContentItem>(DbContextObject.UserRoleInRootContentItem, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize ContentPublicationRequest
            DbContextObject.ContentPublicationRequest.AddRange(new List<ContentPublicationRequest>
            {
                new ContentPublicationRequest
                {
                    Id = TestUtil.MakeTestGuid(1),
                    ApplicationUserId =TestUtil.MakeTestGuid(1),
                    RootContentItemId = TestUtil.MakeTestGuid(1),
                    RequestStatus = PublicationStatus.Confirmed,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles>{ },
                    CreateDateTimeUtc = DateTime.FromFileTimeUtc(100),
                },
                new ContentPublicationRequest
                {
                    Id = TestUtil.MakeTestGuid(2),
                    ApplicationUserId = TestUtil.MakeTestGuid(1),
                    RootContentItemId = TestUtil.MakeTestGuid(2),
                    RequestStatus = PublicationStatus.Unknown,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles> { },
                    CreateDateTimeUtc = DateTime.UtcNow - new TimeSpan(0, 1, 0),
                },
                new ContentPublicationRequest
                {
                    Id = TestUtil.MakeTestGuid(3),
                    ApplicationUserId = TestUtil.MakeTestGuid(1),
                    RootContentItemId = TestUtil.MakeTestGuid(3),
                    RequestStatus = PublicationStatus.Unknown,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles> { },
                    CreateDateTimeUtc = DateTime.UtcNow - new TimeSpan(0, 1, 0),
                },
            });
            MockDbSet<ContentPublicationRequest>.AssignNavigationProperty<ApplicationUser>(DbContextObject.ContentPublicationRequest, "ApplicationUserId", DbContextObject.ApplicationUser);
            MockDbSet<ContentPublicationRequest>.AssignNavigationProperty<RootContentItem>(DbContextObject.ContentPublicationRequest, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize FileUpload
            DbContextObject.FileUpload.AddRange(new List<FileUpload>
            {
                new FileUpload { Id=TestUtil.MakeTestGuid(1) },
            });
            #endregion
        }

        private void GenerateAccountTestData()
        {
            #region Initialize Users
            DbContextObject.ApplicationUser.AddRange(new List<ApplicationUser>
            {
                new ApplicationUser { Id=TestUtil.MakeTestGuid(1), UserName="user1", Email="user1@example.com", NormalizedEmail="USER1@EXAMPLE.COM", NormalizedUserName="USER1" },
                new ApplicationUser { Id=TestUtil.MakeTestGuid(2), UserName="user2", Email="user2@example.com", NormalizedEmail="USER2@EXAMPLE.COM", NormalizedUserName="USER2", EmailConfirmed=true },
            });
            #endregion
        }

        private void GeneratePublishingTestData()
        {
            #region Initialize Users
            DbContextObject.ApplicationUser.AddRange(new List<ApplicationUser>
            {
                new ApplicationUser { Id=TestUtil.MakeTestGuid(1), UserName="user1", Email="user1@example.com" },
                new ApplicationUser { Id=TestUtil.MakeTestGuid(2), UserName="user2", Email="user2@example.com" },
            });
            #endregion

            #region Initialize ContentType
            DbContextObject.ContentType.AddRange(new List<ContentType>
                {
                    new ContentType{ Id=TestUtil.MakeTestGuid(1), Name="Qlikview", CanReduce=true },
                });
            #endregion

            #region Initialize ProfitCenters
            DbContextObject.ProfitCenter.AddRange(new List<ProfitCenter>
                {
                    new ProfitCenter { Id=TestUtil.MakeTestGuid(1), Name="Profit Center 1", ProfitCenterCode="pc1" },
                });
            #endregion

            #region Initialize Clients
            DbContextObject.Client.AddRange(new List<Client>
                {
                    new Client { Id=TestUtil.MakeTestGuid(1), Name="Client 1", ClientCode="C1", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new string[] { "example.com" }  },
                    new Client { Id=TestUtil.MakeTestGuid(2), Name="Client 2", ClientCode="C2", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new string[] { "example.com" }  },
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

            #region Initialize UserRoleInClient
            DbContextObject.UserRoleInClient.AddRange(new List<UserRoleInClient>
            {
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(1) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(1) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1) },
                //new UserRoleInClient { Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(2) },
            });
            MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(DbContextObject.UserRoleInClient, "ClientId", DbContextObject.Client);
            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInClient, "UserId", DbContextObject.ApplicationUser);
            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInClient, "RoleId", DbContextObject.ApplicationRole);
            #endregion

            #region Initialize UserClaims
            DbContextObject.UserClaims.AddRange(new List<IdentityUserClaim<Guid>>
                {
                    new IdentityUserClaim<Guid>{ Id=1, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(1) },
                    new IdentityUserClaim<Guid>{ Id=2, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(2) },
                });
            #endregion
            #endregion

            #region Initialize RootContentItem
            DbContextObject.RootContentItem.AddRange(new List<RootContentItem>
            {
                new RootContentItem{ Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 1", ContentTypeId=TestUtil.MakeTestGuid(1) },
                new RootContentItem{ Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 2", ContentTypeId=TestUtil.MakeTestGuid(1) },
                new RootContentItem{ Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 3", ContentTypeId=TestUtil.MakeTestGuid(1) },
            });
            MockDbSet<RootContentItem>.AssignNavigationProperty<ContentType>(DbContextObject.RootContentItem, "ContentTypeId", DbContextObject.ContentType);
            MockDbSet<RootContentItem>.AssignNavigationProperty<Client>(DbContextObject.RootContentItem, "ClientId", DbContextObject.Client);
            #endregion

            #region Initialize HierarchyField
            DbContextObject.HierarchyField.AddRange(new List<HierarchyField>
                {
                    new HierarchyField { Id=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName1", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                    new HierarchyField { Id=TestUtil.MakeTestGuid(2), RootContentItemId=TestUtil.MakeTestGuid(2), FieldName="Field2", FieldDisplayName="DisplayName2", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                    new HierarchyField { Id=TestUtil.MakeTestGuid(3), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName3", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                });
            MockDbSet<HierarchyField>.AssignNavigationProperty<RootContentItem>(DbContextObject.HierarchyField, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize HierarchyFieldValue
            DbContextObject.HierarchyFieldValue.AddRange(new List<HierarchyFieldValue>
                {
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(1), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 1" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(2), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 2" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(3), HierarchyFieldId=TestUtil.MakeTestGuid(2),  Value="Value 1" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(4), HierarchyFieldId=TestUtil.MakeTestGuid(2),  Value="Value 2" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(5), HierarchyFieldId=TestUtil.MakeTestGuid(3),  Value="Value 1" },
                });
            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty<HierarchyField>(DbContextObject.HierarchyFieldValue, "HierarchyFieldId", DbContextObject.HierarchyField);
            #endregion

            #region Initialize SelectionGroups
            DbContextObject.SelectionGroup.AddRange(new List<SelectionGroup>
                {
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(1), ContentInstanceUrl="Folder1/File1", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group1 For Content1", SelectedHierarchyFieldValueList=new Guid[] { } },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(2), ContentInstanceUrl="Folder1/File2", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group2 For Content1", SelectedHierarchyFieldValueList=new Guid[] { } },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(3), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(2), GroupName="Group1 For Content2", SelectedHierarchyFieldValueList=new Guid[] { } },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(4), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(3), GroupName="Group1 For Content3", SelectedHierarchyFieldValueList=new Guid[] { } },
                });
            MockDbSet<SelectionGroup>.AssignNavigationProperty<RootContentItem>(DbContextObject.SelectionGroup, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize UserInSelectionGroups
            DbContextObject.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
                {
                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(1), SelectionGroupId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(2) },
                });
            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<SelectionGroup>(DbContextObject.UserInSelectionGroup, "SelectionGroupId", DbContextObject.SelectionGroup);
            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserInSelectionGroup, "UserId", DbContextObject.ApplicationUser);
            #endregion

            #region Initialize UserRoles
            DbContextObject.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
                {
                // TODO Undo this
                    //new IdentityUserRole<Guid> { RoleId=((long) RoleEnum.Admin), UserId=TestUtil.MakeTestGuid(1) },
                    new IdentityUserRole<Guid> { RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },
                });
            #endregion

            #region Initialize UserRoleInRootContentItem
            DbContextObject.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
            {
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(2), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(3), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(4), RoleId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(5), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(6), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(2), RootContentItemId=TestUtil.MakeTestGuid(1) },
            });
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInRootContentItem, "RoleId", DbContextObject.ApplicationRole);
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInRootContentItem, "UserId", DbContextObject.ApplicationUser);
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<RootContentItem>(DbContextObject.UserRoleInRootContentItem, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize ContentPublicationRequest
            DbContextObject.ContentPublicationRequest.AddRange(new List<ContentPublicationRequest>
            {
                new ContentPublicationRequest
                {
                    Id = TestUtil.MakeTestGuid(1),
                    ApplicationUserId =TestUtil.MakeTestGuid(1),
                    RootContentItemId = TestUtil.MakeTestGuid(1),
                    RequestStatus = PublicationStatus.Confirmed,
                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles>{ },
                    CreateDateTimeUtc = DateTime.FromFileTimeUtc(100),
                },
            });
            #endregion

            #region Initialize FileUpload
            DbContextObject.FileUpload.AddRange(new List<FileUpload>
            {
                new FileUpload { Id=TestUtil.MakeTestGuid(1) },
            });
            #endregion
        }

        private void GenerateSystemAdminTestData()
        {
            #region Initialize Users
            DbContextObject.ApplicationUser.AddRange(new List<ApplicationUser>
            {
                    new ApplicationUser { Id =  TestUtil.MakeTestGuid(1), UserName = "sysAdmin1", Email = "sysAdmin1@site.domain", },
                    new ApplicationUser { Id =  TestUtil.MakeTestGuid(2), UserName = "sysAdmin2", Email = "sysAdmin2@site.domain", },
                    new ApplicationUser { Id = TestUtil.MakeTestGuid(11), UserName = "sysUser1",  Email = "sysUser1@site.domain",  },
                    new ApplicationUser { Id = TestUtil.MakeTestGuid(12), UserName = "sysUser2",  Email = "sysUser2@site.domain",  },
            });
            #endregion

            #region Initialize ContentType
            DbContextObject.ContentType.AddRange(new List<ContentType>
            { 
                new ContentType{ Id = TestUtil.MakeTestGuid(1), Name = "Qlikview", CanReduce = true },
            });
            #endregion

            #region Initialize ProfitCenters
            DbContextObject.ProfitCenter.AddRange(new List<ProfitCenter>
            { 
                new ProfitCenter { Id = TestUtil.MakeTestGuid(1), },
                new ProfitCenter { Id = TestUtil.MakeTestGuid(2), },
            });
            #endregion

            #region Initialize UserRoleInProfitCenter
            DbContextObject.UserRoleInProfitCenter.AddRange(new List<UserRoleInProfitCenter>
            { 
                new UserRoleInProfitCenter { Id = TestUtil.MakeTestGuid(1), ProfitCenterId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(1), RoleId = TestUtil.MakeTestGuid(1) },
                new UserRoleInProfitCenter { Id = TestUtil.MakeTestGuid(2), ProfitCenterId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(2), RoleId = TestUtil.MakeTestGuid(1) }
            });
            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty(DbContextObject.UserRoleInProfitCenter, "RoleId", DbContextObject.ApplicationRole);
            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty(DbContextObject.UserRoleInProfitCenter, "ProfitCenterId", DbContextObject.ProfitCenter);
            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty(DbContextObject.UserRoleInProfitCenter, "UserId", DbContextObject.ApplicationUser);
            #endregion

            #region Initialize Clients
            DbContextObject.Client.AddRange(new List<Client>
            { 
                new Client { Id = TestUtil.MakeTestGuid(1), ProfitCenterId = TestUtil.MakeTestGuid(1), ParentClientId = null, },
                new Client { Id = TestUtil.MakeTestGuid(2), ProfitCenterId = TestUtil.MakeTestGuid(1), ParentClientId = null, },
            });
            MockDbSet<Client>.AssignNavigationProperty(DbContextObject.Client, "ProfitCenterId", DbContextObject.ProfitCenter);
            #endregion

            #region Initialize User associations with Clients
            /*
             * There has to be a UserClaim for each user who is associated with a client
             * 
             * The number of user claims will not necessarily match the number of UserRoleForClient records, 
             *      since a user can have multiple roles with a client
             */
        
            #region Initialize UserRoleInClient
            DbContextObject.UserRoleInClient.AddRange(new List<UserRoleInClient>
            { 
                new UserRoleInClient { Id = TestUtil.MakeTestGuid(1), ClientId = TestUtil.MakeTestGuid(1), RoleId = TestUtil.MakeTestGuid(1), UserId =  TestUtil.MakeTestGuid(1) },
                new UserRoleInClient { Id = TestUtil.MakeTestGuid(2), ClientId = TestUtil.MakeTestGuid(1), RoleId = TestUtil.MakeTestGuid(5), UserId = TestUtil.MakeTestGuid(11) },
            });
            MockDbSet<UserRoleInClient>.AssignNavigationProperty(DbContextObject.UserRoleInClient, "ClientId", DbContextObject.Client);
            MockDbSet<UserRoleInClient>.AssignNavigationProperty(DbContextObject.UserRoleInClient, "UserId", DbContextObject.ApplicationUser);
            MockDbSet<UserRoleInClient>.AssignNavigationProperty(DbContextObject.UserRoleInClient, "RoleId", DbContextObject.ApplicationRole);
            #endregion

            #region Initialize UserClaims
            DbContextObject.UserClaims.AddRange(new List<IdentityUserClaim<Guid>>
            { 
                new IdentityUserClaim<Guid>{ Id = 1, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(1).ToString(), UserId = TestUtil.MakeTestGuid(1) },
                new IdentityUserClaim<Guid>{ Id = 2, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(1).ToString(), UserId = TestUtil.MakeTestGuid(11) },
            });
            #endregion
            #endregion 

            #region Initialize RootContentItem
            DbContextObject.RootContentItem.AddRange(new List<RootContentItem>
            { 
                new RootContentItem{ Id = TestUtil.MakeTestGuid(1), ClientId = TestUtil.MakeTestGuid(1), ContentTypeId = TestUtil.MakeTestGuid(1) },
                new RootContentItem{ Id = TestUtil.MakeTestGuid(2), ClientId = TestUtil.MakeTestGuid(1), ContentTypeId = TestUtil.MakeTestGuid(1) },
            });
            MockDbSet<RootContentItem>.AssignNavigationProperty(DbContextObject.RootContentItem, "ContentTypeId", DbContextObject.ContentType);
            MockDbSet<RootContentItem>.AssignNavigationProperty(DbContextObject.RootContentItem, "ClientId", DbContextObject.Client);
            #endregion

            #region Initialize SelectionGroups
            DbContextObject.SelectionGroup.AddRange(new List<SelectionGroup>
            {
                new SelectionGroup { Id = TestUtil.MakeTestGuid(1), ContentInstanceUrl = "Folder1/File1", RootContentItemId = TestUtil.MakeTestGuid(1), GroupName = "Group1 For Content1" },
                new SelectionGroup { Id = TestUtil.MakeTestGuid(2), ContentInstanceUrl = "Folder1/File2", RootContentItemId = TestUtil.MakeTestGuid(1), GroupName = "Group2 For Content1" },
            });
            MockDbSet<SelectionGroup>.AssignNavigationProperty(DbContextObject.SelectionGroup, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize UserInSelectionGroups
            DbContextObject.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
            { 
                new UserInSelectionGroup { Id = TestUtil.MakeTestGuid(1), SelectionGroupId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(11) },
                new UserInSelectionGroup { Id = TestUtil.MakeTestGuid(2), SelectionGroupId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(12) },
            });
            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty(DbContextObject.UserInSelectionGroup, "SelectionGroupId", DbContextObject.SelectionGroup);
            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty(DbContextObject.UserInSelectionGroup, "UserId", DbContextObject.ApplicationUser);
            #endregion

            #region Initialize UserRoles
            DbContextObject.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
            { 
                // TODO Undo this
                    //new IdentityUserRole<Guid> { RoleId=((long) RoleEnum.Admin), UserId=TestUtil.MakeTestGuid(1) },
                    new IdentityUserRole<Guid> { RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },
            });
            #endregion

            #region Initialize UserRoleInRootContentItem
            DbContextObject.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
            { 
            });
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(DbContextObject.UserRoleInRootContentItem, "RoleId", DbContextObject.ApplicationRole);
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(DbContextObject.UserRoleInRootContentItem, "UserId", DbContextObject.ApplicationUser);
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(DbContextObject.UserRoleInRootContentItem, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion
        }

    }
}
