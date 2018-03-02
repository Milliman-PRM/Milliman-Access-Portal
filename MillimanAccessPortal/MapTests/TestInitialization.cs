using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Moq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using QlikviewLib;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;
using AuditLogLib;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MillimanAccessPortal.Services;

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

        public Mock<AuditLogger> MockAuditLogger { get; set; }
        public AuditLogger AuditLoggerObject { get => MockAuditLogger.Object; }

        public Mock<IMessageQueue> MockMessageQueueService { get; set; }
        public IMessageQueue MessageQueueServicesObject { get => MockMessageQueueService.Object; }

        public IOptions<QlikviewConfig> QvConfig { get; set; }

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
            MockDbContext = GenerateDbContext();
            MockUserManager = MapTests.MockUserManager.New(MockDbContext);
            MockRoleManager = GenerateRoleManager(MockDbContext);
            MockMessageQueueService = GenerateMessageQueueService();
            LoggerFactory = new LoggerFactory();
            AuthorizationService = GenerateAuthorizationService(DbContextObject, UserManagerObject, LoggerFactory);
            QueriesObj = new StandardQueries(DbContextObject, UserManagerObject);
            QvConfig = BuildQvConfig();
            MockAuditLogger = GenerateAuditLogger();
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

        private Mock<AuditLogger> GenerateAuditLogger()
        {
            AuditLogLib.AuditLogger.Config = new AuditLogLib.AuditLoggerConfiguration { AuditLogConnectionString = "" };
            Mock<AuditLogger> ReturnObject = new Mock<AuditLogger>();
            ReturnObject.Setup(al => al.Log(It.IsAny<AuditEvent>())).Callback(() => { });

            return ReturnObject;
        }

        private void GenerateBasicTestData()
        {
            #region Initialize Users
            DbContextObject.ApplicationUser.AddRange(new List<ApplicationUser>
                {
                    new ApplicationUser {
                        Id = 1,
                        UserName = "test1",
                        Email = "test1@example.com",
                        Employer = "example",
                        FirstName = "FN1",
                        LastName = "LN1",
                        NormalizedEmail = "test@example.com".ToUpper(),
                        PhoneNumber = "3171234567"
                    },
                    new ApplicationUser {
                        Id = 2,
                        UserName = "test2",
                        Email = "test2@example.com",
                        Employer = "example",
                        FirstName = "FN2",
                        LastName = "LN2",
                        NormalizedEmail = "test2@example.com".ToUpper(),
                        PhoneNumber = "3171234567",
                    },
                    new ApplicationUser {
                        Id = 3,
                        UserName = "ClientAdmin1",
                        Email = "clientadmin1@example2.com",
                        Employer = "example",
                        FirstName = "Client",
                        LastName = "Admin1",
                        NormalizedEmail = "clientadmin1@example2.com".ToUpper(),
                        PhoneNumber = "3171234567",
                    },
                    new ApplicationUser {
                        Id = 4,
                        UserName = "test3",
                        Email = "test3@example2.com",
                        Employer = "example",
                        FirstName = "FN3",
                        LastName = "LN3",
                        NormalizedEmail = "test3@example2.com".ToUpper(),
                        PhoneNumber = "3171234567",
                    },
                    new ApplicationUser {
                        Id = 5,
                        UserName = "user5",
                        Email = "user5@example.com",
                        Employer = "example",
                        FirstName = "FN5",
                        LastName = "LN5",
                        NormalizedEmail = "user5@example.com".ToUpper(),
                        PhoneNumber = "1234567890",
                    },
                    new ApplicationUser {
                        Id = 6,
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
                    new ContentType{ Id=1, Name="Qlikview", CanReduce=true },
                });
            #endregion

            #region Initialize ProfitCenters
            DbContextObject.ProfitCenter.AddRange(new List<ProfitCenter>
                { 
                    new ProfitCenter { Id=1, Name="Profit Center 1", ProfitCenterCode="pc1" },
                    new ProfitCenter { Id=2, Name="Profit Center 2", ProfitCenterCode="pc2" },
                });
            #endregion

            #region Initialize UserRoleInProfitCenter
            DbContextObject.UserRoleInProfitCenter.AddRange(new List<UserRoleInProfitCenter>
            { 
                new UserRoleInProfitCenter { Id=1, ProfitCenterId=1, UserId=3, RoleId=1 }
            });
            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInProfitCenter, "RoleId", DbContextObject.ApplicationRole);
            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ProfitCenter>(DbContextObject.UserRoleInProfitCenter, "ProfitCenterId", DbContextObject.ProfitCenter);
            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInProfitCenter, "UserId", DbContextObject.ApplicationUser);
            #endregion

            #region Initialize Clients
            DbContextObject.Client.AddRange(new List<Client>
                { 
                    new Client { Id=1, Name="Name1", ClientCode="ClientCode1", ProfitCenterId=1, ParentClientId=null, AcceptedEmailDomainList=new string[] { "example.com" }  },
                    new Client { Id=2, Name="Name2", ClientCode="ClientCode2", ProfitCenterId=1, ParentClientId=1,    AcceptedEmailDomainList=new string[] { "example.com" }  },
                    new Client { Id=3, Name="Name3", ClientCode="ClientCode3", ProfitCenterId=1, ParentClientId=null, AcceptedEmailDomainList=new string[] { "example2.com" } },
                    new Client { Id=4, Name="Name4", ClientCode="ClientCode4", ProfitCenterId=2, ParentClientId=null, AcceptedEmailDomainList=new string[] { "example2.com" } },
                    new Client { Id=5, Name="Name5", ClientCode="ClientCode5", ProfitCenterId=1, ParentClientId=null, AcceptedEmailDomainList=new string[] { "example2.com" } },
                    new Client { Id=6, Name="Name6", ClientCode="ClientCode6", ProfitCenterId=1, ParentClientId=1,    AcceptedEmailDomainList=new string[] { "example2.com" } },
                    new Client { Id=7, Name="Name7", ClientCode="ClientCode7", ProfitCenterId=1, ParentClientId=null, AcceptedEmailDomainList=new string[] { "example.com" } },
                    new Client { Id=8, Name="Name8", ClientCode="ClientCode8", ProfitCenterId=1, ParentClientId=7,    AcceptedEmailDomainList=new string[] { "example.com" } },
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
                        new UserRoleInClient { Id=1, ClientId=1, RoleId=2, UserId=1 },
                        new UserRoleInClient { Id=2, ClientId=1, RoleId=1, UserId=3 },
                        new UserRoleInClient { Id=3, ClientId=4, RoleId=1, UserId=3 },
                        new UserRoleInClient { Id=4, ClientId=5, RoleId=1, UserId=3 },
                        new UserRoleInClient { Id=5, ClientId=6, RoleId=1, UserId=3 },
                        new UserRoleInClient { Id=6, ClientId=5, RoleId=5, UserId=2 },
                        new UserRoleInClient { Id=7, ClientId=7, RoleId=1, UserId=3 },
                        new UserRoleInClient { Id=8, ClientId=8, RoleId=1, UserId=3 },
                        new UserRoleInClient { Id=9, ClientId=8, RoleId=3, UserId=5 },
                        new UserRoleInClient { Id=10, ClientId=8, RoleId=3, UserId=6 },
                    });
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(DbContextObject.UserRoleInClient, "ClientId", DbContextObject.Client);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInClient, "UserId", DbContextObject.ApplicationUser);
                MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInClient, "RoleId", DbContextObject.ApplicationRole);
                #endregion

                #region Initialize UserClaims
                DbContextObject.UserClaims.AddRange(new List<IdentityUserClaim<long>>
                { 
                    new IdentityUserClaim<long>{ Id=1, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="1", UserId=3 },
                    new IdentityUserClaim<long>{ Id=2, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="4", UserId=3 },
                    new IdentityUserClaim<long>{ Id=3, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="5", UserId=3 },
                    new IdentityUserClaim<long>{ Id=4, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="1", UserId=1 },
                    new IdentityUserClaim<long>{ Id=5, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="5", UserId=2 },
                    new IdentityUserClaim<long>{ Id=6, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="6", UserId=3 },
                    new IdentityUserClaim<long>{ Id=7, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="7", UserId=3 },
                    new IdentityUserClaim<long>{ Id=8, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="8", UserId=3 },
                    new IdentityUserClaim<long>{ Id=9, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="8", UserId=5 },
                    new IdentityUserClaim<long>{ Id=10, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue="8", UserId=6 },
                });
                #endregion
            #endregion 

            #region Initialize RootContentItem
            DbContextObject.RootContentItem.AddRange(new List<RootContentItem>
                { 
                    new RootContentItem{ Id=1, ClientId=1, ContentName="RootContent 1", ContentTypeId=1 },
                    new RootContentItem{ Id=2, ClientId=2, ContentName="RootContent 2", ContentTypeId=1 },
                    new RootContentItem{ Id=3, ClientId=8, ContentName="RootContent 3", ContentTypeId=1 },
                });
            MockDbSet<RootContentItem>.AssignNavigationProperty<ContentType>(DbContextObject.RootContentItem, "ContentTypeId", DbContextObject.ContentType);
            MockDbSet<RootContentItem>.AssignNavigationProperty<Client>(DbContextObject.RootContentItem, "ClientId", DbContextObject.Client);
            #endregion

            #region Initialize HierarchyField
            DbContextObject.HierarchyField.AddRange(new List<HierarchyField>
                {
                    new HierarchyField { Id=1, RootContentItemId=1, FieldName="Field1", FieldDisplayName="DisplayName1", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                    new HierarchyField { Id=2, RootContentItemId=3, FieldName="Field1", FieldDisplayName="DisplayName1", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                });
            MockDbSet<HierarchyField>.AssignNavigationProperty<RootContentItem>(DbContextObject.HierarchyField, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize HierarchyFieldValue
            DbContextObject.HierarchyFieldValue.AddRange(new List<HierarchyFieldValue>
                { 
                    new HierarchyFieldValue { Id=1, HierarchyFieldId=1,  Value="Value 1" },
                    new HierarchyFieldValue { Id=2, HierarchyFieldId=2,  Value="Value 1" },
                    new HierarchyFieldValue { Id=3, HierarchyFieldId=2,  Value="Value 2" },
                });
            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty<HierarchyField>(DbContextObject.HierarchyFieldValue, "HierarchyFieldId", DbContextObject.HierarchyField);
            #endregion

            #region Initialize SelectionGroups
            DbContextObject.SelectionGroup.AddRange(new List<SelectionGroup>
                { 
                    new SelectionGroup { Id=1, ContentInstanceUrl="Folder1/File1", RootContentItemId=1, GroupName="Group1 For Content1", SelectedHierarchyFieldValueList=new long[] { } },
                    new SelectionGroup { Id=2, ContentInstanceUrl="Folder1/File2", RootContentItemId=1, GroupName="Group2 For Content1", SelectedHierarchyFieldValueList=new long[] { } },
                    new SelectionGroup { Id=3, ContentInstanceUrl="Folder2/File1", RootContentItemId=2, GroupName="Group1 For Content2", SelectedHierarchyFieldValueList=new long[] { } },
                    new SelectionGroup { Id=4, ContentInstanceUrl="Folder3/File1", RootContentItemId=3, GroupName="Group1 For Content3", SelectedHierarchyFieldValueList=new long[] { } },
                    new SelectionGroup { Id=5, ContentInstanceUrl="Folder3/File2", RootContentItemId=3, GroupName="Group2 For Content3", SelectedHierarchyFieldValueList=new long[] { } },
                });
            MockDbSet<SelectionGroup>.AssignNavigationProperty<RootContentItem>(DbContextObject.SelectionGroup, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize UserInSelectionGroups
            DbContextObject.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
                { 
                    new UserInSelectionGroup { Id=1, SelectionGroupId=1, UserId=1 },
                    new UserInSelectionGroup { Id=2, SelectionGroupId=4, UserId=3 },
                });
            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<SelectionGroup>(DbContextObject.UserInSelectionGroup, "SelectionGroupId", DbContextObject.SelectionGroup);
            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserInSelectionGroup, "UserId", DbContextObject.ApplicationUser);
            #endregion

            #region Initialize UserRoles
            DbContextObject.UserRoles.AddRange(new List<IdentityUserRole<long>>
                { 
                    new IdentityUserRole<long> { RoleId=((long) RoleEnum.Admin), UserId=1 },
                });
            #endregion

            #region Initialize UserRoleInRootContentItem
            DbContextObject.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
            { 
                new UserRoleInRootContentItem { Id=1, RoleId=5, UserId=1, RootContentItemId=1 },
                new UserRoleInRootContentItem { Id=2, RoleId=5, UserId=3, RootContentItemId=3 },
                new UserRoleInRootContentItem { Id=3, RoleId=5, UserId=5, RootContentItemId=3 },
                new UserRoleInRootContentItem { Id=4, RoleId=3, UserId=5, RootContentItemId=3 },
                new UserRoleInRootContentItem { Id=5, RoleId=5, UserId=6, RootContentItemId=3 },
            });
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationRole>(DbContextObject.UserRoleInRootContentItem, "RoleId", DbContextObject.ApplicationRole);
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationUser>(DbContextObject.UserRoleInRootContentItem, "UserId", DbContextObject.ApplicationUser);
            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<RootContentItem>(DbContextObject.UserRoleInRootContentItem, "RootContentItemId", DbContextObject.RootContentItem);
            #endregion

            #region Initialize ContentPublicationRequest
            DbContextObject.ContentPublicationRequest.AddRange(new List<ContentPublicationRequest>
            {
                new ContentPublicationRequest { Id=1, ApplicationUserId=5, MasterFilePath="C:\\Dir\\file.ext", RootContentItemId=3 },
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
