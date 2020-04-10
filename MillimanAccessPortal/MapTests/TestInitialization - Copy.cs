/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Initialization of resources used by MAP controllers, especially injected services that MAP initializes through ASP architecture
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.Utilities;
using Moq;
using Newtonsoft.Json;
using PowerBiLib;
using QlikviewLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using TestResourcesLib;

//namespace MapTests
//{
//    internal class TestInitialization
//    {
//        public IServiceProvider ServiceProvider { get; private set; } = default;
//        public IServiceScope Scope { get; private set; } = default;
//        public IServiceScopeFactory ServiceScopeFactory { get; private set; } = default;

//        #region Scoped registered services
//        public ApplicationDbContext DbContext { get; private set; } = default;
//        public UserManager<ApplicationUser> UserManager { get; private set; } = default;
//        public RoleManager<ApplicationRole> RoleManager { get; private set; } = default;
//        public SignInManager<ApplicationUser> SignInManager { get; private set; } = default;
//        public IAuditLogger AuditLogger { get; private set; } = default;
//        public IAuthorizationService AuthorizationService { get; private set; } = default;
//        public IConfiguration Configuration { get; private set; } = default;
//        public IServiceProvider ScopedServiceProvider { get; private set; } = default;
//        public IAuthenticationService AuthenticationService { get; private set; } = default;
//        public IOptions<PowerBiConfig> PowerBiConfig { get; private set; } = default;
//        public IOptions<QlikviewConfig> QvConfig { get; private set; } = default;
//        public IAuthenticationSchemeProvider AuthenticationSchemeProvider { get; private set; } = default;
//        public StandardQueries StandardQueries { get; set; } = default;
//        public ContentAccessAdminQueries ContentAccessAdminQueries { get; set; } = default;
//        public ContentPublishingAdminQueries ContentPublishingAdminQueries { get; set; } = default;
//        public FileDropQueries FileDropQueries { get; set; } = default;
//        public ClientQueries ClientQueries { get; set; } = default;
//        public ContentItemQueries ContentItemQueries { get; set; } = default;
//        public HierarchyQueries HierarchyQueries { get; set; } = default;
//        public SelectionGroupQueries SelectionGroupQueries { get; set; } = default;
//        public PublicationQueries PublicationQueries { get; set; } = default;
//        public UserQueries UserQueries { get; set; } = default;
//        public FileSystemTasks FileSystemTasks { get; set; } = default;
//        public IUploadHelper UploadHelper { get; set; } = default;
//        #endregion

//        #region Transient registered services
//        public IMessageQueue MessageQueueServicesObject { get; private set; } = default;
//        #endregion

//        #region Singleton registered services
//        public IGoLiveTaskQueue GoLiveTaskQueue { get; set; } = default;
//        public IPublicationPostProcessingTaskQueue PublicationPostProcessingTaskQueue { get; set; } = default;
//        public IUploadTaskQueue UploadTaskQueue { get; set; } = default;
//        public IFileProvider FileProvider { get; set; } = default;
//        #endregion

//        public TestInitialization()
//        {
//            Configuration = GenerateConfiguration();

//            ConfigureServices();

//            ServiceScopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
//            Scope = ServiceScopeFactory.CreateScope();

//            InitializeInjectedServices();
//        }

//        ~TestInitialization()
//        {
//            Scope.Dispose();
//        }

//        public void ConfigureServices()
//        {
//            string tokenProviderName = "MAPResetToken";

//            var services = new ServiceCollection();

//            services.AddDbContext<ApplicationDbContext>(builder =>
//            {
//                //builder.UseNpgsql("xyz");
//                builder.UseNpgsql("Server=localhost;Database=UnitTests;User Id=postgres;Password=postgres;");
//                var opt = builder.Options;
//            });

//            services.AddDataProtection();

//            services.AddIdentityCore<ApplicationUser>()
//                .AddRoles<ApplicationRole>()
//                .AddSignInManager()
//                .AddEntityFrameworkStores<ApplicationDbContext>()
//                .AddDefaultTokenProviders()
//                .AddTokenProvider<PasswordResetSecurityTokenProvider<ApplicationUser>>(tokenProviderName)
//                .AddTop100000PasswordValidator<ApplicationUser>()
//                .AddRecentPasswordInDaysValidator<ApplicationUser>(30)
//                .AddPasswordValidator<PasswordIsNotEmailValidator<ApplicationUser>>()
//                .AddCommonWordsValidator<ApplicationUser>(new List<string>())
//                ;

//            services.AddAuthentication(IdentityConstants.ApplicationScheme)
//                    .AddIdentityCookies();

//            services.Configure<IdentityOptions>(options =>
//            {
//                // Password settings
//                options.Password.RequireDigit = true;
//                options.Password.RequiredLength = 10;
//                options.Password.RequiredUniqueChars = 6;
//                options.Password.RequireNonAlphanumeric = true;
//                options.Password.RequireUppercase = true;
//                options.Password.RequireLowercase = false;

//                // Lockout settings
//                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
//                options.Lockout.MaxFailedAccessAttempts = 5;
//                options.Lockout.AllowedForNewUsers = true;

//                // User settings
//                options.User.RequireUniqueEmail = true;

//                // Enable custom token provider for password resets
//                options.Tokens.PasswordResetTokenProvider = tokenProviderName;
//            });

//            // Configure custom security token provider
//            services.Configure<PasswordResetSecurityTokenProviderOptions>(options =>
//            {
//                options.TokenLifespan = TimeSpan.FromHours(3);
//            });

//            // Configure the default token provider used for account activation
//            services.Configure<DataProtectionTokenProviderOptions>(options =>
//            {
//                options.TokenLifespan = TimeSpan.FromDays(7);
//            });

//            services.AddScoped<IConfiguration>(p => GenerateConfiguration());

//            //services.AddScoped<ApplicationDbContext, MockableMapDbContext>();
//            services.AddScoped<StandardQueries>();
//            services.AddScoped<ContentAccessAdminQueries>();
//            services.AddScoped<ContentPublishingAdminQueries>();
//            services.AddScoped<FileDropQueries>();
//            services.AddScoped<FileSystemTasks>();
//            services.AddScoped<IAuditLogger> (p => MockAuditLogger.New().Object);
//            services.AddScoped<IUploadHelper, UploadHelper>();
//            services.AddScoped<ClientQueries>();
//            services.AddScoped<ContentItemQueries>();
//            services.AddScoped<HierarchyQueries>();
//            services.AddScoped<SelectionGroupQueries>();
//            services.AddScoped<PublicationQueries>();
//            services.AddScoped<PublicationQueries>();
//            services.AddScoped<UserQueries>();

//            string fileUploadPath = Path.GetTempPath();
//            // The environment variable check enables migrations to be deployed to Staging or Production via the MAP deployment server
//            // This variable should never be set on a real production or staging system
//            if (Configuration != null && !string.IsNullOrWhiteSpace(Configuration.GetValue<string>("Storage:FileUploadPath")) && Environment.GetEnvironmentVariable("MIGRATIONS_RUNNING") == null)
//            {
//                fileUploadPath = Configuration.GetValue<string>("Storage:FileUploadPath");
//            }
//            services.AddSingleton<IFileProvider>(new PhysicalFileProvider(fileUploadPath));
//            services.AddSingleton<IGoLiveTaskQueue, GoLiveTaskQueue>();
//            services.AddSingleton<IPublicationPostProcessingTaskQueue, PublicationPostProcessingTaskQueue>();
//            services.AddSingleton<IUploadTaskQueue, UploadTaskQueue>();

//            services.AddTransient<IMessageQueue, MessageQueueServices>();

//            ServiceProvider = services.BuildServiceProvider();
//        }

//        public void InitializeInjectedServices()
//        {
//            #region Scoped registered services
//            DbContext = Scope.ServiceProvider.GetService<ApplicationDbContext>();
//            UserManager = Scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
//            RoleManager = Scope.ServiceProvider.GetService<RoleManager<ApplicationRole>>();
//            SignInManager = Scope.ServiceProvider.GetService<SignInManager<ApplicationUser>>();
//            AuditLogger = Scope.ServiceProvider.GetService<IAuditLogger>();
//            AuthorizationService = Scope.ServiceProvider.GetService<IAuthorizationService>();
//            Configuration = Scope.ServiceProvider.GetService<IConfiguration>();
//            ScopedServiceProvider = Scope.ServiceProvider;
//            AuthenticationService = Scope.ServiceProvider.GetService<AuthenticationService>();
//            AuthenticationSchemeProvider = ScopedServiceProvider.GetService<IAuthenticationSchemeProvider>();
//            StandardQueries = ScopedServiceProvider.GetService<StandardQueries>();
//            ContentAccessAdminQueries = ScopedServiceProvider.GetService<ContentAccessAdminQueries>();
//            ContentPublishingAdminQueries = ScopedServiceProvider.GetService<ContentPublishingAdminQueries>();
//            FileDropQueries = ScopedServiceProvider.GetService<FileDropQueries>();
//            FileSystemTasks = ScopedServiceProvider.GetService<FileSystemTasks>();
//            UploadHelper = ScopedServiceProvider.GetService<IUploadHelper>();
//            ClientQueries = ScopedServiceProvider.GetService<ClientQueries>();
//            ContentItemQueries = ScopedServiceProvider.GetService<ContentItemQueries>();
//            HierarchyQueries = ScopedServiceProvider.GetService<HierarchyQueries>();
//            SelectionGroupQueries = ScopedServiceProvider.GetService<SelectionGroupQueries>();
//            PublicationQueries = ScopedServiceProvider.GetService<PublicationQueries>();
//            UserQueries = ScopedServiceProvider.GetService<UserQueries>();
//            FileProvider = ScopedServiceProvider.GetService<IFileProvider>();
//            #endregion

//            #region Transient registered services
//            //MailSender.ConfigureMailSender(new SmtpConfig());
//            //MessageQueueServicesObject = ServiceProvider.GetService<IMessageQueue>();
//            MessageQueueServicesObject = new Mock<IMessageQueue>().Object;
//            #endregion

//            #region Singleton registered services
//            GoLiveTaskQueue = ServiceProvider.GetService<IGoLiveTaskQueue>();
//            PublicationPostProcessingTaskQueue = ServiceProvider.GetService<IPublicationPostProcessingTaskQueue>();
//            UploadTaskQueue = ServiceProvider.GetService<IUploadTaskQueue>();
//            #endregion
//        }

//        /// <summary>
//        /// Initializes a ControllerContext based on a user name. 
//        /// </summary>
//        /// <param name="userName">The user to be impersonated in the ControllerContext</param>
//        /// <returns></returns>
//        internal ControllerContext GenerateControllerContext(string userName = null, UriBuilder requestUriBuilder = null, Dictionary<string, StringValues> requestHeaders = null)
//        {
//            ClaimsPrincipal TestUserClaimsPrincipal = new ClaimsPrincipal();
//            if (!string.IsNullOrWhiteSpace(userName))
//            {
//                ApplicationUser userRecord = DbContext.ApplicationUser.Single(u => u.UserName == userName);
//                Claim[] newIdentity = new[] 
//                { 
//                    new Claim(ClaimTypes.Name, userName), 
//                    new Claim(ClaimTypes.NameIdentifier, userRecord.Id.ToString()) 
//                };
//                TestUserClaimsPrincipal.AddIdentity(new ClaimsIdentity(newIdentity));
//            }

//            return GenerateControllerContext(TestUserClaimsPrincipal, requestUriBuilder, requestHeaders);
//        }

//        /// <summary>
//        /// Initializes a ControllerContext as needed to construct a functioning controller. 
//        /// </summary>
//        /// <param name="UserAsClaimsPrincipal">The user to be impersonated in the ControllerContext</param>
//        /// <returns></returns>
//        internal static ControllerContext GenerateControllerContext(ClaimsPrincipal UserAsClaimsPrincipal, UriBuilder requestUriBuilder = null, Dictionary<string, StringValues> requestHeaders = null)
//        {
//            ControllerContext returnVal = new ControllerContext
//            {
//                HttpContext = new DefaultHttpContext() { User = UserAsClaimsPrincipal, },
//                ActionDescriptor = new ControllerActionDescriptor { ActionName = "Unit Test" }
//            };

//            if (requestUriBuilder != null)
//            {
//                returnVal.HttpContext.Request.Scheme = requestUriBuilder.Scheme;
//                returnVal.HttpContext.Request.Host = requestUriBuilder.Port > 0
//                    ? new HostString(requestUriBuilder.Host, requestUriBuilder.Port)
//                    : new HostString(requestUriBuilder.Host);
//                returnVal.HttpContext.Request.Path = requestUriBuilder.Path;

//                if (!string.IsNullOrWhiteSpace(requestUriBuilder.Query))
//                {
//                    var listOfQueries = requestUriBuilder.Query.Substring(1).Split('&', StringSplitOptions.RemoveEmptyEntries).ToList();
//                    Dictionary<string, StringValues> dict = new Dictionary<string, StringValues>();
//                    foreach (string query in listOfQueries)
//                    {
//                        var keyAndValue = query.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
//                        dict.Add(keyAndValue[0], keyAndValue[1]);
//                    }
//                    returnVal.HttpContext.Request.Query = new QueryCollection(dict);
//                }
//            }

//            if (requestHeaders != null)
//            {
//                foreach (var header in requestHeaders)
//                {
//                    returnVal.HttpContext.Request.Headers.Add(header);
//                }
//            }

//            return returnVal;
//        }

//        public void GenerateTestData(IEnumerable<DataSelection> DataSelections)
//        {
//            var DataGenFunctionDict = new Dictionary<DataSelection, Action>
//            {
//                // Important: Keep this dictionary synchronized with enum DataSelection above
//                { DataSelection.Basic, GenerateBasicTestData },
//                { DataSelection.Reduction, GenerateReductionTestData },
//                { DataSelection.Account, GenerateAccountTestData },
//                { DataSelection.SystemAdmin, GenerateSystemAdminTestData },
//                { DataSelection.FileDrop, GenerateFileDropTestData },
//            };

//            foreach (DataSelection Selection in DataSelections.Distinct())
//            {
//                DataGenFunctionDict[Selection]();
//            }

//            ConnectServicesToData();
//        }

//        private void GenerateBasicTestData()
//        {
//            #region Initialize Users
//            DbContext.ApplicationUser.AddRange(new List<ApplicationUser>
//                {
//                    new ApplicationUser {
//                        Id = TestUtil.MakeTestGuid(1),
//                        UserName = "test1",
//                        Email = "test1@example.com",
//                        Employer = "example",
//                        FirstName = "FN1",
//                        LastName = "LN1",
//                        NormalizedEmail = "test@example.com".ToUpper(),
//                        PhoneNumber = "3171234567"
//                    },
//                    new ApplicationUser {
//                        Id = TestUtil.MakeTestGuid(2),
//                        UserName = "test2",
//                        Email = "test2@example.com",
//                        Employer = "example",
//                        FirstName = "FN2",
//                        LastName = "LN2",
//                        NormalizedEmail = "test2@example.com".ToUpper(),
//                        PhoneNumber = "3171234567",
//                    },
//                    new ApplicationUser {
//                        Id = TestUtil.MakeTestGuid(3),
//                        UserName = "ClientAdmin1",
//                        Email = "clientadmin1@example2.com",
//                        Employer = "example",
//                        FirstName = "Client",
//                        LastName = "Admin1",
//                        NormalizedEmail = "clientadmin1@example2.com".ToUpper(),
//                        PhoneNumber = "3171234567",
//                    },
//                    new ApplicationUser {
//                        Id = TestUtil.MakeTestGuid(4),
//                        UserName = "test3",
//                        Email = "test3@example2.com",
//                        Employer = "example",
//                        FirstName = "FN3",
//                        LastName = "LN3",
//                        NormalizedEmail = "test3@example2.com".ToUpper(),
//                        PhoneNumber = "3171234567",
//                    },
//                    new ApplicationUser {
//                        Id = TestUtil.MakeTestGuid(5),
//                        UserName = "user5",
//                        Email = "user5@example.com",
//                        Employer = "example",
//                        FirstName = "FN5",
//                        LastName = "LN5",
//                        NormalizedEmail = "user5@example.com".ToUpper(),
//                        PhoneNumber = "1234567890",
//                    },
//                    new ApplicationUser {
//                        Id = TestUtil.MakeTestGuid(6),
//                        UserName = "user6",
//                        Email = "user6@example.com",
//                        Employer = "example",
//                        FirstName = "FN6",
//                        LastName = "LN6",
//                        NormalizedEmail = "user6@example.com".ToUpper(),
//                        PhoneNumber = "1234567890",
//                    },
//            });
//            #endregion

//            #region Initialize ContentType
//            DbContext.ContentType.AddRange(new List<ContentType>
//                {
//                    new ContentType{ Id=TestUtil.MakeTestGuid(1), TypeEnum=ContentTypeEnum.Qlikview, CanReduce=true },
//                });
//            #endregion

//            #region Initialize ProfitCenters
//            DbContext.ProfitCenter.AddRange(new List<ProfitCenter>
//                {
//                    new ProfitCenter { Id=TestUtil.MakeTestGuid(1), Name="Profit Center 1", ProfitCenterCode="pc1" },
//                    new ProfitCenter { Id=TestUtil.MakeTestGuid(2), Name="Profit Center 2", ProfitCenterCode="pc2" },
//                });
//            #endregion

//            #region Initialize UserRoleInProfitCenter
//            DbContext.UserRoleInProfitCenter.AddRange(new List<UserRoleInProfitCenter>
//            {
//                new UserRoleInProfitCenter { Id=TestUtil.MakeTestGuid(1), ProfitCenterId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3), RoleId=TestUtil.MakeTestGuid(1) }
//            });
//            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ApplicationRole>(DbContext.UserRoleInProfitCenter, "RoleId", DbContext.ApplicationRole);
//            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ProfitCenter>(DbContext.UserRoleInProfitCenter, "ProfitCenterId", DbContext.ProfitCenter);
//            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty<ApplicationUser>(DbContext.UserRoleInProfitCenter, "UserId", DbContext.ApplicationUser);
//            #endregion

//            #region Initialize Clients
//            DbContext.Client.AddRange(new List<Client>
//                {
//                    new Client { Id=TestUtil.MakeTestGuid(1), Name="Name1", ClientCode="ClientCode1", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" }  },
//                    new Client { Id=TestUtil.MakeTestGuid(2), Name="Name2", ClientCode="ClientCode2", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=TestUtil.MakeTestGuid(1),    AcceptedEmailDomainList=new List<string> { "example.com" }  },
//                    new Client { Id=TestUtil.MakeTestGuid(3), Name="Name3", ClientCode="ClientCode3", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example2.com" } },
//                    new Client { Id=TestUtil.MakeTestGuid(4), Name="Name4", ClientCode="ClientCode4", ProfitCenterId=TestUtil.MakeTestGuid(2), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example2.com" } },
//                    new Client { Id=TestUtil.MakeTestGuid(5), Name="Name5", ClientCode="ClientCode5", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example2.com" } },
//                    new Client { Id=TestUtil.MakeTestGuid(6), Name="Name6", ClientCode="ClientCode6", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=TestUtil.MakeTestGuid(1),    AcceptedEmailDomainList=new List<string> { "example2.com" } },
//                    new Client { Id=TestUtil.MakeTestGuid(7), Name="Name7", ClientCode="ClientCode7", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" } },
//                    new Client { Id=TestUtil.MakeTestGuid(8), Name="Name8", ClientCode="ClientCode8", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=TestUtil.MakeTestGuid(7),    AcceptedEmailDomainList=new List<string> { "example.com" } },
//                });
//            MockDbSet<Client>.AssignNavigationProperty<ProfitCenter>(DbContext.Client, "ProfitCenterId", DbContext.ProfitCenter);
//            #endregion

//            #region Initialize User associations with Clients
//            /*
//             * There has to be a UserClaim for each user who is associated with a client
//             * 
//             * The number of user claims will not necessarily match the number of UserRoleForClient records, 
//             *      since a user can have multiple roles with a client
//             */

//            #region Initialize UserRoleInClient
//            DbContext.UserRoleInClient.AddRange(new List<UserRoleInClient>
//                    {
//                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(2), UserId=TestUtil.MakeTestGuid(1) },
//                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3) },
//                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(4), RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3) },
//                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(5), RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3) },
//                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(5), ClientId=TestUtil.MakeTestGuid(6), RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3) },
//                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(6), ClientId=TestUtil.MakeTestGuid(5), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(2) },
//                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(7), ClientId=TestUtil.MakeTestGuid(7), RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3) },
//                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(8), ClientId=TestUtil.MakeTestGuid(8), RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3) },
//                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(9), ClientId=TestUtil.MakeTestGuid(8), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(5) },
//                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(10), ClientId=TestUtil.MakeTestGuid(8), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(6) },
//                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(11), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(2), UserId=TestUtil.MakeTestGuid(2) }, // this record is intentionally without a respective claim
//                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(12), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1) },
//                    });
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(DbContext.UserRoleInClient, "ClientId", DbContext.Client);
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(DbContext.UserRoleInClient, "UserId", DbContext.ApplicationUser);
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(DbContext.UserRoleInClient, "RoleId", DbContext.ApplicationRole);
//            #endregion

//            #region Initialize UserClaims
//            DbContext.UserClaims.AddRange(new List<IdentityUserClaim<Guid>>
//                {
//                    new IdentityUserClaim<Guid>{ Id=1, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(3) },
//                    new IdentityUserClaim<Guid>{ Id=2, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(4).ToString(), UserId=TestUtil.MakeTestGuid(3) },
//                    new IdentityUserClaim<Guid>{ Id=3, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(5).ToString(), UserId=TestUtil.MakeTestGuid(3) },
//                    new IdentityUserClaim<Guid>{ Id=4, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(1) },
//                    new IdentityUserClaim<Guid>{ Id=5, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(5).ToString(), UserId=TestUtil.MakeTestGuid(2) },
//                    new IdentityUserClaim<Guid>{ Id=6, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(6).ToString(), UserId=TestUtil.MakeTestGuid(3) },
//                    new IdentityUserClaim<Guid>{ Id=7, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(7).ToString(), UserId=TestUtil.MakeTestGuid(3) },
//                    new IdentityUserClaim<Guid>{ Id=8, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(8).ToString(), UserId=TestUtil.MakeTestGuid(3) },
//                    new IdentityUserClaim<Guid>{ Id=9, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(8).ToString(), UserId=TestUtil.MakeTestGuid(5) },
//                    new IdentityUserClaim<Guid>{ Id=10, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(8).ToString(), UserId=TestUtil.MakeTestGuid(6) },
//                    new IdentityUserClaim<Guid>{ Id=11, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(5) },
//                });
//            #endregion
//            #endregion

//            #region Initialize RootContentItem
//            DbContext.RootContentItem.AddRange(new List<RootContentItem>
//                {
//                    new RootContentItem{ Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 1", ContentTypeId=TestUtil.MakeTestGuid(1),
//                        ContentFilesList = new List<ContentRelatedFile>{
//                            new ContentRelatedFile {
//                                FileOriginalName = "filename",
//                                FilePurpose = "mastercontent",
//                            },
//                        },
//                    },
//                    new RootContentItem{ Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(2), ContentName="RootContent 2", ContentTypeId=TestUtil.MakeTestGuid(1) },
//                    new RootContentItem{ Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(8), ContentName="RootContent 3", ContentTypeId=TestUtil.MakeTestGuid(1) },
//                    new RootContentItem{ Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 4", ContentTypeId=TestUtil.MakeTestGuid(1) },
//                    new RootContentItem{ Id=TestUtil.MakeTestGuid(5), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 5", ContentTypeId=TestUtil.MakeTestGuid(1) },
//                });
//            MockDbSet<RootContentItem>.AssignNavigationProperty<ContentType>(DbContext.RootContentItem, "ContentTypeId", DbContext.ContentType);
//            MockDbSet<RootContentItem>.AssignNavigationProperty<Client>(DbContext.RootContentItem, "ClientId", DbContext.Client);
//            #endregion

//            #region Initialize HierarchyField
//            DbContext.HierarchyField.AddRange(new List<HierarchyField>
//                {
//                    new HierarchyField { Id=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName1", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
//                });
//            MockDbSet<HierarchyField>.AssignNavigationProperty<RootContentItem>(DbContext.HierarchyField, "RootContentItemId", DbContext.RootContentItem);
//            #endregion

//            #region Initialize HierarchyFieldValue
//            DbContext.HierarchyFieldValue.AddRange(new List<HierarchyFieldValue>
//                {
//                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(1), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 1" },
//                });
//            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty<HierarchyField>(DbContext.HierarchyFieldValue, "HierarchyFieldId", DbContext.HierarchyField);
//            #endregion

//            #region Initialize SelectionGroups
//            DbContext.SelectionGroup.AddRange(new List<SelectionGroup>
//                {
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(1), ContentInstanceUrl="Folder1/File1", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group1 For Content1" },
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(2), ContentInstanceUrl="Folder1/File2", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group2 For Content1" },
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(3), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group1 For Content2" },
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(4), ContentInstanceUrl="Folder3/File1", RootContentItemId=TestUtil.MakeTestGuid(3), GroupName="Group1 For Content3" },
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(5), ContentInstanceUrl="Folder3/File2", RootContentItemId=TestUtil.MakeTestGuid(3), GroupName="Group2 For Content3" },
//                });
//            MockDbSet<SelectionGroup>.AssignNavigationProperty<RootContentItem>(DbContext.SelectionGroup, "RootContentItemId", DbContext.RootContentItem);
//            #endregion

//            #region Initialize UserInSelectionGroups
//            DbContext.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
//                {
//                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(1), SelectionGroupId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },
//                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(2), SelectionGroupId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(3) },
//                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(3), SelectionGroupId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },  // duplicate
//                });
//            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<SelectionGroup>(DbContext.UserInSelectionGroup, "SelectionGroupId", DbContext.SelectionGroup);
//            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<ApplicationUser>(DbContext.UserInSelectionGroup, "UserId", DbContext.ApplicationUser);
//            #endregion

//            #region Initialize UserRoles
//            DbContext.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
//                { 
//                    //new IdentityUserRole<Guid> { RoleId=((long) RoleEnum.Admin), UserId=TestUtil.MakeTestGuid(1) },
//                    new IdentityUserRole<Guid> { RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },
//                });
//            #endregion

//            #region Initialize UserRoleInRootContentItem
//            DbContext.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
//            {
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(2), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(3), RootContentItemId=TestUtil.MakeTestGuid(3) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(3), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(5), RootContentItemId=TestUtil.MakeTestGuid(3) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(4), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(5), RootContentItemId=TestUtil.MakeTestGuid(3) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(5), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(6), RootContentItemId=TestUtil.MakeTestGuid(3) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(6), RoleId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
//            });
//            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationRole>(DbContext.UserRoleInRootContentItem, "RoleId", DbContext.ApplicationRole);
//            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationUser>(DbContext.UserRoleInRootContentItem, "UserId", DbContext.ApplicationUser);
//            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<RootContentItem>(DbContext.UserRoleInRootContentItem, "RootContentItemId", DbContext.RootContentItem);
//            #endregion
//        }

//        private void GenerateReductionTestData()
//        {
//            #region Initialize Users
//            DbContext.ApplicationUser.AddRange(new List<ApplicationUser>
//                {
//                    new ApplicationUser { Id=TestUtil.MakeTestGuid(1), UserName="user1", Email="user1@example.com" },
//                    new ApplicationUser { Id=TestUtil.MakeTestGuid(2), UserName="user2", Email="user2@example.com" },
//                    new ApplicationUser { Id=TestUtil.MakeTestGuid(3), UserName="user3", Email="user3@example.com" },
//            });
//            #endregion

//            #region Initialize ContentType
//            DbContext.ContentType.AddRange(new List<ContentType>
//                {
//                    new ContentType{ Id=TestUtil.MakeTestGuid((int)ContentTypeEnum.Qlikview), TypeEnum=ContentTypeEnum.Qlikview, CanReduce=true },
//                    new ContentType{ Id=TestUtil.MakeTestGuid((int)ContentTypeEnum.PowerBi), TypeEnum=ContentTypeEnum.PowerBi, CanReduce=true },
//                });
//            #endregion

//            #region Initialize ProfitCenters
//            DbContext.ProfitCenter.AddRange(new List<ProfitCenter>
//                {
//                    new ProfitCenter { Id=TestUtil.MakeTestGuid(1), Name="Profit Center 1", ProfitCenterCode="pc1" },
//                });
//            #endregion

//            #region Initialize Clients
//            DbContext.Client.AddRange(new List<Client>
//                {
//                    new Client { Id=TestUtil.MakeTestGuid(1), Name="Client 1", ClientCode="C1", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" }  },
//                    new Client { Id=TestUtil.MakeTestGuid(2), Name="Client 2", ClientCode="C2", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" }  },
//                });
//            MockDbSet<Client>.AssignNavigationProperty<ProfitCenter>(DbContext.Client, "ProfitCenterId", DbContext.ProfitCenter);
//            #endregion

//            #region Initialize User associations with Clients
//            /*
//             * There has to be a UserClaim for each user who is associated with a client
//             * 
//             * The number of user claims will not necessarily match the number of UserRoleForClient records, 
//             *      since a user can have multiple roles with a client
//             */

//            #region Initialize UserRoleInClient
//            DbContext.UserRoleInClient.AddRange(new List<UserRoleInClient>
//            {
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(1) },
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(1) },
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1) },
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(2) },
//            });
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(DbContext.UserRoleInClient, "ClientId", DbContext.Client);
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(DbContext.UserRoleInClient, "UserId", DbContext.ApplicationUser);
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(DbContext.UserRoleInClient, "RoleId", DbContext.ApplicationRole);
//            #endregion

//            #region Initialize UserClaims
//            DbContext.UserClaims.AddRange(new List<IdentityUserClaim<Guid>>
//                {
//                    new IdentityUserClaim<Guid>{ Id=1, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(1) },
//                    new IdentityUserClaim<Guid>{ Id=2, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(2) },
//                });
//            #endregion
//            #endregion

//            #region Initialize RootContentItem
//            DbContext.RootContentItem.AddRange(new List<RootContentItem>
//            {
//                new RootContentItem{ Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 1", ContentTypeId=TestUtil.MakeTestGuid((int)ContentTypeEnum.Qlikview), DoesReduce=true },
//                new RootContentItem{ Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 2", ContentTypeId=TestUtil.MakeTestGuid((int)ContentTypeEnum.Qlikview) },
//                new RootContentItem{ Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 3", ContentTypeId=TestUtil.MakeTestGuid((int)ContentTypeEnum.Qlikview) },
//                new RootContentItem{ Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 4", ContentTypeId=TestUtil.MakeTestGuid((int)ContentTypeEnum.PowerBi), TypeSpecificDetail = JsonConvert.SerializeObject(new PowerBiContentItemProperties()) },
//            });
//            MockDbSet<RootContentItem>.AssignNavigationProperty<ContentType>(DbContext.RootContentItem, "ContentTypeId", DbContext.ContentType);
//            MockDbSet<RootContentItem>.AssignNavigationProperty<Client>(DbContext.RootContentItem, "ClientId", DbContext.Client);
//            #endregion

//            #region Initialize HierarchyField
//            DbContext.HierarchyField.AddRange(new List<HierarchyField>
//                {
//                    new HierarchyField { Id=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName1", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
//                    new HierarchyField { Id=TestUtil.MakeTestGuid(2), RootContentItemId=TestUtil.MakeTestGuid(2), FieldName="Field2", FieldDisplayName="DisplayName2", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
//                    new HierarchyField { Id=TestUtil.MakeTestGuid(3), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName3", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
//                });
//            MockDbSet<HierarchyField>.AssignNavigationProperty<RootContentItem>(DbContext.HierarchyField, "RootContentItemId", DbContext.RootContentItem);
//            #endregion

//            #region Initialize HierarchyFieldValue
//            DbContext.HierarchyFieldValue.AddRange(new List<HierarchyFieldValue>
//                {
//                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(1), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 1" },
//                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(2), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 2" },
//                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(3), HierarchyFieldId=TestUtil.MakeTestGuid(2),  Value="Value 1" },
//                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(4), HierarchyFieldId=TestUtil.MakeTestGuid(2),  Value="Value 2" },
//                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(5), HierarchyFieldId=TestUtil.MakeTestGuid(3),  Value="Value 1" },
//                });
//            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty<HierarchyField>(DbContext.HierarchyFieldValue, "HierarchyFieldId", DbContext.HierarchyField);
//            #endregion

//            #region Initialize SelectionGroups
//            DbContext.SelectionGroup.AddRange(new List<SelectionGroup>
//                {
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(1), ContentInstanceUrl="Folder1/File1", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group1 For Content1", SelectedHierarchyFieldValueList=new List<Guid>() },
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(2), ContentInstanceUrl="Folder1/File2", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group2 For Content1", SelectedHierarchyFieldValueList=new List<Guid>() },
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(3), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(2), GroupName="Group1 For Content2", SelectedHierarchyFieldValueList=new List<Guid>() },
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(4), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(3), GroupName="Group1 For Content3", SelectedHierarchyFieldValueList=new List<Guid>() },
//                });
//            MockDbSet<SelectionGroup>.AssignNavigationProperty<RootContentItem>(DbContext.SelectionGroup, "RootContentItemId", DbContext.RootContentItem);
//            #endregion

//            #region Initialize UserInSelectionGroups
//            DbContext.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
//                {
//                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(1), SelectionGroupId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(2) },
//                });
//            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<SelectionGroup>(DbContext.UserInSelectionGroup, "SelectionGroupId", DbContext.SelectionGroup);
//            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<ApplicationUser>(DbContext.UserInSelectionGroup, "UserId", DbContext.ApplicationUser);
//            #endregion

//            #region Initialize UserRoles
//            DbContext.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
//                {
//                    //new IdentityUserRole<Guid> { RoleId=((long) RoleEnum.Admin), UserId=TestUtil.MakeTestGuid(1) },
//                    new IdentityUserRole<Guid> { RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },
//                });
//            #endregion

//            #region Initialize UserRoleInRootContentItem
//            DbContext.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
//            {
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(2), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(3), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(4), RoleId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(5), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(6), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(2), RootContentItemId=TestUtil.MakeTestGuid(1) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(7), RoleId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(4) },
//            });
//            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationRole>(DbContext.UserRoleInRootContentItem, "RoleId", DbContext.ApplicationRole);
//            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationUser>(DbContext.UserRoleInRootContentItem, "UserId", DbContext.ApplicationUser);
//            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<RootContentItem>(DbContext.UserRoleInRootContentItem, "RootContentItemId", DbContext.RootContentItem);
//            #endregion

//            #region Initialize ContentPublicationRequest
//            DbContext.ContentPublicationRequest.AddRange(new List<ContentPublicationRequest>
//            {
//                new ContentPublicationRequest
//                {
//                    Id = TestUtil.MakeTestGuid(1),
//                    ApplicationUserId =TestUtil.MakeTestGuid(1),
//                    RootContentItemId = TestUtil.MakeTestGuid(1),
//                    RequestStatus = PublicationStatus.Confirmed,
//                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles>{ },
//                    CreateDateTimeUtc = DateTime.FromFileTimeUtc(100),
//                },
//                new ContentPublicationRequest
//                {
//                    Id = TestUtil.MakeTestGuid(2),
//                    ApplicationUserId = TestUtil.MakeTestGuid(1),
//                    RootContentItemId = TestUtil.MakeTestGuid(2),
//                    RequestStatus = PublicationStatus.Unknown,
//                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles> { },
//                    CreateDateTimeUtc = DateTime.UtcNow - new TimeSpan(0, 1, 0),
//                },
//                new ContentPublicationRequest
//                {
//                    Id = TestUtil.MakeTestGuid(3),
//                    ApplicationUserId = TestUtil.MakeTestGuid(1),
//                    RootContentItemId = TestUtil.MakeTestGuid(3),
//                    RequestStatus = PublicationStatus.Unknown,
//                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles> { },
//                    CreateDateTimeUtc = DateTime.UtcNow - new TimeSpan(0, 1, 0),
//                },
//            });
//            MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(DbContext.ContentPublicationRequest, "ApplicationUserId", DbContext.ApplicationUser);
//            MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(DbContext.ContentPublicationRequest, "RootContentItemId", DbContext.RootContentItem);
//            #endregion

//            #region Initialize FileUpload
//            DbContext.FileUpload.AddRange(new List<FileUpload>
//            {
//                new FileUpload { Id=TestUtil.MakeTestGuid(1) },
//            });
//            #endregion
//        }

//        private void GenerateAccountTestData()
//        {
//            #region authentication schemes
//            DbContext.AuthenticationScheme.AddRange(new List<MapDbContextLib.Context.AuthenticationScheme>
//            {
//                new MapDbContextLib.Context.AuthenticationScheme  // AuthenticationType.Default
//                {
//                    Id = TestUtil.MakeTestGuid(1),
//                    Name = IdentityConstants.ApplicationScheme,
//                    DisplayName = "The default scheme",
//                    Type = AuthenticationType.Default,
//                    SchemePropertiesObj = null
//                },
//                new MapDbContextLib.Context.AuthenticationScheme  // "prmtest", AuthenticationType.WsFederation
//                {
//                    Id =TestUtil.MakeTestGuid(2),
//                    Name = "prmtest",
//                    DisplayName = "PRMTest.local Domain",
//                    Type = AuthenticationType.WsFederation,
//                    SchemePropertiesObj = new WsFederationSchemeProperties
//                    {
//                        MetadataAddress = "https://adfs.prmtest.local/FederationMetadata/2007-06/FederationMetadata.xml",
//                        Wtrealm = "https://localhost:44336"
//                    }
//                },
//                new MapDbContextLib.Context.AuthenticationScheme  // "domainmatch", AuthenticationType.WsFederation
//                {
//                    Id =TestUtil.MakeTestGuid(3),
//                    Name = "domainmatch",
//                    DisplayName = "DomainMatch.local Domain",
//                    Type = AuthenticationType.WsFederation,
//                    SchemePropertiesObj = new WsFederationSchemeProperties
//                    {
//                        MetadataAddress = "https://adfs.domainmatch.local/FederationMetadata/2007-06/FederationMetadata.xml",
//                        Wtrealm = "https://localhost:44336"
//                    },
//                    DomainList = { "DomainMatch.local" },
//                },
//            });
//            #endregion

//            #region Initialize Users
//            DbContext.ApplicationUser.AddRange(new List<ApplicationUser>
//            {
//                new ApplicationUser { Id=TestUtil.MakeTestGuid(1), UserName="user1", Email="user1@example.com", NormalizedEmail="USER1@EXAMPLE.COM", NormalizedUserName="USER1" },
//                new ApplicationUser { Id=TestUtil.MakeTestGuid(2), UserName="user2", Email="user2@example.com", NormalizedEmail="USER2@EXAMPLE.COM", NormalizedUserName="USER2", EmailConfirmed=true },
//                new ApplicationUser { Id=TestUtil.MakeTestGuid(3), UserName="user3-confirmed-defaultscheme", Email="user3@example.com", NormalizedEmail="USER3@EXAMPLE.COM", NormalizedUserName="USER3-CONFIRMED-DEFAULTSCHEME", EmailConfirmed=true, AuthenticationSchemeId = TestUtil.MakeTestGuid(1) },
//                new ApplicationUser { Id=TestUtil.MakeTestGuid(4), UserName="user4-confirmed-wsscheme", Email="user4@example.com", NormalizedEmail="USER4@EXAMPLE.COM", NormalizedUserName="USER4-CONFIRMED-WSSCHEME", EmailConfirmed=true, AuthenticationSchemeId = TestUtil.MakeTestGuid(2) },
//                new ApplicationUser { Id=TestUtil.MakeTestGuid(5), UserName="user5-notconfirmed-wsscheme", Email="user5@example.com", NormalizedEmail="USER5@EXAMPLE.COM", NormalizedUserName="USER5-NOTCONFIRMED-WSSCHEME", EmailConfirmed=false, AuthenticationSchemeId = TestUtil.MakeTestGuid(2) },
//                new ApplicationUser { Id=TestUtil.MakeTestGuid(6), UserName="user6-confirmed@domainmatch.local", Email="user6@example.com", NormalizedEmail="USER6@EXAMPLE.COM", NormalizedUserName="USER6-CONFIRMED@DOMAINMATCH.LOCAL", EmailConfirmed=false },
//                new ApplicationUser { Id=TestUtil.MakeTestGuid(7), UserName="user7-confirmed@domainnomatch.local", Email="user7@example.com", NormalizedEmail="USER7@EXAMPLE.COM", NormalizedUserName="USER7-CONFIRMED@DOMAINNOMATCH.LOCAL", EmailConfirmed=false },
//            });
//            MockDbSet<ApplicationUser>.AssignNavigationProperty(DbContext.ApplicationUser, "AuthenticationSchemeId", DbContext.AuthenticationScheme);
//            #endregion
//        }

//        private void GeneratePublishingTestData()
//        {
//            #region Initialize Users
//            DbContext.ApplicationUser.AddRange(new List<ApplicationUser>
//            {
//                new ApplicationUser { Id=TestUtil.MakeTestGuid(1), UserName="user1", Email="user1@example.com" },
//                new ApplicationUser { Id=TestUtil.MakeTestGuid(2), UserName="user2", Email="user2@example.com" },
//            });
//            #endregion

//            #region Initialize ContentType
//            DbContext.ContentType.AddRange(new List<ContentType>
//                {
//                    new ContentType{ Id=TestUtil.MakeTestGuid(1), TypeEnum=ContentTypeEnum.Qlikview, CanReduce=true },
//                });
//            #endregion

//            #region Initialize ProfitCenters
//            DbContext.ProfitCenter.AddRange(new List<ProfitCenter>
//                {
//                    new ProfitCenter { Id=TestUtil.MakeTestGuid(1), Name="Profit Center 1", ProfitCenterCode="pc1" },
//                });
//            #endregion

//            #region Initialize Clients
//            DbContext.Client.AddRange(new List<Client>
//                {
//                    new Client { Id=TestUtil.MakeTestGuid(1), Name="Client 1", ClientCode="C1", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" }  },
//                    new Client { Id=TestUtil.MakeTestGuid(2), Name="Client 2", ClientCode="C2", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" }  },
//                });
//            MockDbSet<Client>.AssignNavigationProperty<ProfitCenter>(DbContext.Client, "ProfitCenterId", DbContext.ProfitCenter);
//            #endregion

//            #region Initialize User associations with Clients
//            /*
//             * There has to be a UserClaim for each user who is associated with a client
//             * 
//             * The number of user claims will not necessarily match the number of UserRoleForClient records, 
//             *      since a user can have multiple roles with a client
//             */

//            #region Initialize UserRoleInClient
//            DbContext.UserRoleInClient.AddRange(new List<UserRoleInClient>
//            {
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(1) },
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(1) },
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1) },
//                //new UserRoleInClient { Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(2) },
//            });
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty<Client>(DbContext.UserRoleInClient, "ClientId", DbContext.Client);
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationUser>(DbContext.UserRoleInClient, "UserId", DbContext.ApplicationUser);
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty<ApplicationRole>(DbContext.UserRoleInClient, "RoleId", DbContext.ApplicationRole);
//            #endregion

//            #region Initialize UserClaims
//            DbContext.UserClaims.AddRange(new List<IdentityUserClaim<Guid>>
//                {
//                    new IdentityUserClaim<Guid>{ Id=1, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(1) },
//                    new IdentityUserClaim<Guid>{ Id=2, ClaimType=ClaimNames.ClientMembership.ToString(), ClaimValue=TestUtil.MakeTestGuid(1).ToString(), UserId=TestUtil.MakeTestGuid(2) },
//                });
//            #endregion
//            #endregion

//            #region Initialize RootContentItem
//            DbContext.RootContentItem.AddRange(new List<RootContentItem>
//            {
//                new RootContentItem{ Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 1", ContentTypeId=TestUtil.MakeTestGuid(1) },
//                new RootContentItem{ Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 2", ContentTypeId=TestUtil.MakeTestGuid(1) },
//                new RootContentItem{ Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 3", ContentTypeId=TestUtil.MakeTestGuid(1) },
//            });
//            MockDbSet<RootContentItem>.AssignNavigationProperty<ContentType>(DbContext.RootContentItem, "ContentTypeId", DbContext.ContentType);
//            MockDbSet<RootContentItem>.AssignNavigationProperty<Client>(DbContext.RootContentItem, "ClientId", DbContext.Client);
//            #endregion

//            #region Initialize HierarchyField
//            DbContext.HierarchyField.AddRange(new List<HierarchyField>
//                {
//                    new HierarchyField { Id=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName1", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
//                    new HierarchyField { Id=TestUtil.MakeTestGuid(2), RootContentItemId=TestUtil.MakeTestGuid(2), FieldName="Field2", FieldDisplayName="DisplayName2", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
//                    new HierarchyField { Id=TestUtil.MakeTestGuid(3), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName3", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
//                });
//            MockDbSet<HierarchyField>.AssignNavigationProperty<RootContentItem>(DbContext.HierarchyField, "RootContentItemId", DbContext.RootContentItem);
//            #endregion

//            #region Initialize HierarchyFieldValue
//            DbContext.HierarchyFieldValue.AddRange(new List<HierarchyFieldValue>
//                {
//                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(1), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 1" },
//                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(2), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 2" },
//                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(3), HierarchyFieldId=TestUtil.MakeTestGuid(2),  Value="Value 1" },
//                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(4), HierarchyFieldId=TestUtil.MakeTestGuid(2),  Value="Value 2" },
//                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(5), HierarchyFieldId=TestUtil.MakeTestGuid(3),  Value="Value 1" },
//                });
//            MockDbSet<HierarchyFieldValue>.AssignNavigationProperty<HierarchyField>(DbContext.HierarchyFieldValue, "HierarchyFieldId", DbContext.HierarchyField);
//            #endregion

//            #region Initialize SelectionGroups
//            DbContext.SelectionGroup.AddRange(new List<SelectionGroup>
//                {
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(1), ContentInstanceUrl="Folder1/File1", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group1 For Content1", SelectedHierarchyFieldValueList=new List<Guid>() },
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(2), ContentInstanceUrl="Folder1/File2", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group2 For Content1", SelectedHierarchyFieldValueList=new List<Guid>() },
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(3), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(2), GroupName="Group1 For Content2", SelectedHierarchyFieldValueList=new List<Guid>() },
//                    new SelectionGroup { Id=TestUtil.MakeTestGuid(4), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(3), GroupName="Group1 For Content3", SelectedHierarchyFieldValueList=new List<Guid>() },
//                });
//            MockDbSet<SelectionGroup>.AssignNavigationProperty<RootContentItem>(DbContext.SelectionGroup, "RootContentItemId", DbContext.RootContentItem);
//            #endregion

//            #region Initialize UserInSelectionGroups
//            DbContext.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
//                {
//                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(1), SelectionGroupId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(2) },
//                });
//            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<SelectionGroup>(DbContext.UserInSelectionGroup, "SelectionGroupId", DbContext.SelectionGroup);
//            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty<ApplicationUser>(DbContext.UserInSelectionGroup, "UserId", DbContext.ApplicationUser);
//            #endregion

//            #region Initialize UserRoles
//            DbContext.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
//                {
//                    //new IdentityUserRole<Guid> { RoleId=((long) RoleEnum.Admin), UserId=TestUtil.MakeTestGuid(1) },
//                    new IdentityUserRole<Guid> { RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },
//                });
//            #endregion

//            #region Initialize UserRoleInRootContentItem
//            DbContext.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
//            {
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(2), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(3), RoleId=TestUtil.MakeTestGuid(3), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(4), RoleId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(5), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
//                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(6), RoleId=TestUtil.MakeTestGuid(5), UserId=TestUtil.MakeTestGuid(2), RootContentItemId=TestUtil.MakeTestGuid(1) },
//            });
//            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationRole>(DbContext.UserRoleInRootContentItem, "RoleId", DbContext.ApplicationRole);
//            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<ApplicationUser>(DbContext.UserRoleInRootContentItem, "UserId", DbContext.ApplicationUser);
//            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty<RootContentItem>(DbContext.UserRoleInRootContentItem, "RootContentItemId", DbContext.RootContentItem);
//            #endregion

//            #region Initialize ContentPublicationRequest
//            DbContext.ContentPublicationRequest.AddRange(new List<ContentPublicationRequest>
//            {
//                new ContentPublicationRequest
//                {
//                    Id = TestUtil.MakeTestGuid(1),
//                    ApplicationUserId =TestUtil.MakeTestGuid(1),
//                    RootContentItemId = TestUtil.MakeTestGuid(1),
//                    RequestStatus = PublicationStatus.Confirmed,
//                    ReductionRelatedFilesObj = new List<ReductionRelatedFiles>{ },
//                    CreateDateTimeUtc = DateTime.FromFileTimeUtc(100),
//                },
//            });
//            #endregion

//            #region Initialize FileUpload
//            DbContext.FileUpload.AddRange(new List<FileUpload>
//            {
//                new FileUpload { Id=TestUtil.MakeTestGuid(1) },
//            });
//            #endregion
//        }

//        private void GenerateSystemAdminTestData()
//        {
//            #region authentication schemes
//            DbContext.AuthenticationScheme.AddRange(new List<MapDbContextLib.Context.AuthenticationScheme>
//            {
//                new MapDbContextLib.Context.AuthenticationScheme  // AuthenticationType.Default
//                {
//                    Id = TestUtil.MakeTestGuid(1),
//                    Name = IdentityConstants.ApplicationScheme,
//                    DisplayName = "The default scheme",
//                    Type = AuthenticationType.Default,
//                    SchemePropertiesObj = null
//                },
//                new MapDbContextLib.Context.AuthenticationScheme  // "prmtest", AuthenticationType.WsFederation
//                {
//                    Id =TestUtil.MakeTestGuid(2),
//                    Name = "prmtest",
//                    DisplayName = "PRMTest.local Domain",
//                    Type = AuthenticationType.WsFederation,
//                    SchemePropertiesObj = new WsFederationSchemeProperties
//                    {
//                        MetadataAddress = "https://adfs.prmtest.local/FederationMetadata/2007-06/FederationMetadata.xml",
//                        Wtrealm = "https://localhost:44336"
//                    }
//                },
//                new MapDbContextLib.Context.AuthenticationScheme  // "domainmatch", AuthenticationType.WsFederation
//                {
//                    Id =TestUtil.MakeTestGuid(3),
//                    Name = "domainmatch",
//                    DisplayName = "DomainMatch.local Domain",
//                    Type = AuthenticationType.WsFederation,
//                    SchemePropertiesObj = new WsFederationSchemeProperties
//                    {
//                        MetadataAddress = "https://adfs.domainmatch.local/FederationMetadata/2007-06/FederationMetadata.xml",
//                        Wtrealm = "https://localhost:44336"
//                    },
//                    DomainList = { "DomainMatch.local" },
//                },
//            });
//            #endregion

//            #region Initialize Users
//            DbContext.ApplicationUser.AddRange(new List<ApplicationUser>
//            {
//                    new ApplicationUser { Id =  TestUtil.MakeTestGuid(1), UserName = "sysAdmin1", Email = "sysAdmin1@site.domain", },
//                    new ApplicationUser { Id =  TestUtil.MakeTestGuid(2), UserName = "sysAdmin2", Email = "sysAdmin2@site.domain", },
//                    new ApplicationUser { Id = TestUtil.MakeTestGuid(11), UserName = "sysUser1",  Email = "sysUser1@site.domain",  },
//                    new ApplicationUser { Id = TestUtil.MakeTestGuid(12), UserName = "sysUser2",  Email = "sysUser2@site.domain",  },
//            });
//            MockDbSet<ApplicationUser>.AssignNavigationProperty(DbContext.ApplicationUser, "AuthenticationSchemeId", DbContext.AuthenticationScheme);
//            #endregion

//            #region Initialize ContentType
//            DbContext.ContentType.AddRange(new List<ContentType>
//            {
//                new ContentType{ Id = TestUtil.MakeTestGuid(1), TypeEnum = ContentTypeEnum.Qlikview, CanReduce = true },
//            });
//            #endregion

//            #region Initialize ProfitCenters
//            DbContext.ProfitCenter.AddRange(new List<ProfitCenter>
//            {
//                new ProfitCenter { Id = TestUtil.MakeTestGuid(1), },
//                new ProfitCenter { Id = TestUtil.MakeTestGuid(2), },
//            });
//            #endregion

//            #region Initialize UserRoleInProfitCenter
//            DbContext.UserRoleInProfitCenter.AddRange(new List<UserRoleInProfitCenter>
//            {
//                new UserRoleInProfitCenter { Id = TestUtil.MakeTestGuid(1), ProfitCenterId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(1), RoleId = TestUtil.MakeTestGuid(1) },
//                new UserRoleInProfitCenter { Id = TestUtil.MakeTestGuid(2), ProfitCenterId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(2), RoleId = TestUtil.MakeTestGuid(1) }
//            });
//            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty(DbContext.UserRoleInProfitCenter, "RoleId", DbContext.ApplicationRole);
//            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty(DbContext.UserRoleInProfitCenter, "ProfitCenterId", DbContext.ProfitCenter);
//            MockDbSet<UserRoleInProfitCenter>.AssignNavigationProperty(DbContext.UserRoleInProfitCenter, "UserId", DbContext.ApplicationUser);
//            #endregion

//            #region Initialize Clients
//            DbContext.Client.AddRange(new List<Client>
//            {
//                new Client { Id = TestUtil.MakeTestGuid(1), ProfitCenterId = TestUtil.MakeTestGuid(1), ParentClientId = null, AcceptedEmailDomainList = new List<string>{"abc.com", "def.com"} },
//                new Client { Id = TestUtil.MakeTestGuid(2), ProfitCenterId = TestUtil.MakeTestGuid(1), ParentClientId = null, },
//            });
//            MockDbSet<Client>.AssignNavigationProperty(DbContext.Client, "ProfitCenterId", DbContext.ProfitCenter);
//            #endregion

//            #region Initialize User associations with Clients
//            /*
//             * There has to be a UserClaim for each user who is associated with a client
//             * 
//             * The number of user claims will not necessarily match the number of UserRoleForClient records, 
//             *      since a user can have multiple roles with a client
//             */

//            #region Initialize UserRoleInClient
//            DbContext.UserRoleInClient.AddRange(new List<UserRoleInClient>
//            {
//                new UserRoleInClient { Id = TestUtil.MakeTestGuid(1), ClientId = TestUtil.MakeTestGuid(1), RoleId = TestUtil.MakeTestGuid(1), UserId =  TestUtil.MakeTestGuid(1) },
//                new UserRoleInClient { Id = TestUtil.MakeTestGuid(2), ClientId = TestUtil.MakeTestGuid(1), RoleId = TestUtil.MakeTestGuid(5), UserId = TestUtil.MakeTestGuid(11) },
//            });
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty(DbContext.UserRoleInClient, "ClientId", DbContext.Client);
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty(DbContext.UserRoleInClient, "UserId", DbContext.ApplicationUser);
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty(DbContext.UserRoleInClient, "RoleId", DbContext.ApplicationRole);
//            #endregion

//            #region Initialize UserClaims
//            DbContext.UserClaims.AddRange(new List<IdentityUserClaim<Guid>>
//            {
//                new IdentityUserClaim<Guid>{ Id = 1, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(1).ToString(), UserId = TestUtil.MakeTestGuid(1) },
//                new IdentityUserClaim<Guid>{ Id = 2, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(1).ToString(), UserId = TestUtil.MakeTestGuid(11) },
//            });
//            #endregion
//            #endregion 

//            #region Initialize RootContentItem
//            DbContext.RootContentItem.AddRange(new List<RootContentItem>
//            {
//                new RootContentItem{ Id = TestUtil.MakeTestGuid(1), ClientId = TestUtil.MakeTestGuid(1), ContentTypeId = TestUtil.MakeTestGuid(1) },
//                new RootContentItem{ Id = TestUtil.MakeTestGuid(2), ClientId = TestUtil.MakeTestGuid(1), ContentTypeId = TestUtil.MakeTestGuid(1) },
//            });
//            MockDbSet<RootContentItem>.AssignNavigationProperty(DbContext.RootContentItem, "ContentTypeId", DbContext.ContentType);
//            MockDbSet<RootContentItem>.AssignNavigationProperty(DbContext.RootContentItem, "ClientId", DbContext.Client);
//            #endregion

//            #region Initialize SelectionGroups
//            DbContext.SelectionGroup.AddRange(new List<SelectionGroup>
//            {
//                new SelectionGroup { Id = TestUtil.MakeTestGuid(1), ContentInstanceUrl = "Folder1/File1", RootContentItemId = TestUtil.MakeTestGuid(1), GroupName = "Group1 For Content1" },
//                new SelectionGroup { Id = TestUtil.MakeTestGuid(2), ContentInstanceUrl = "Folder1/File2", RootContentItemId = TestUtil.MakeTestGuid(1), GroupName = "Group2 For Content1" },
//            });
//            MockDbSet<SelectionGroup>.AssignNavigationProperty(DbContext.SelectionGroup, "RootContentItemId", DbContext.RootContentItem);
//            #endregion

//            #region Initialize UserInSelectionGroups
//            DbContext.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
//            {
//                new UserInSelectionGroup { Id = TestUtil.MakeTestGuid(1), SelectionGroupId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(11) },
//                new UserInSelectionGroup { Id = TestUtil.MakeTestGuid(2), SelectionGroupId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(12) },
//            });
//            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty(DbContext.UserInSelectionGroup, "SelectionGroupId", DbContext.SelectionGroup);
//            MockDbSet<UserInSelectionGroup>.AssignNavigationProperty(DbContext.UserInSelectionGroup, "UserId", DbContext.ApplicationUser);
//            #endregion

//            #region Initialize UserRoles
//            DbContext.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
//            { 
//                    //new IdentityUserRole<Guid> { RoleId=((long) RoleEnum.Admin), UserId=TestUtil.MakeTestGuid(1) },
//                    new IdentityUserRole<Guid> { RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },
//            });
//            #endregion

//            #region Initialize UserRoleInRootContentItem
//            DbContext.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
//            {
//            });
//            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(DbContext.UserRoleInRootContentItem, "RoleId", DbContext.ApplicationRole);
//            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(DbContext.UserRoleInRootContentItem, "UserId", DbContext.ApplicationUser);
//            MockDbSet<UserRoleInRootContentItem>.AssignNavigationProperty(DbContext.UserRoleInRootContentItem, "RootContentItemId", DbContext.RootContentItem);
//            #endregion

//            #region Initialize ContentPublicationRequest
//            DbContext.ContentPublicationRequest.AddRange(new List<ContentPublicationRequest>
//            {
//                new ContentPublicationRequest { Id = TestUtil.MakeTestGuid(1), RootContentItemId = TestUtil.MakeTestGuid(1), RequestStatus = PublicationStatus.Processing }
//            });
//            MockDbSet<ContentPublicationRequest>.AssignNavigationProperty(DbContext.ContentPublicationRequest, "RootContentItemId", DbContext.RootContentItem);
//            #endregion

//            #region Initialize ContentReductionTask
//            DbContext.ContentReductionTask.AddRange(new List<ContentReductionTask>
//            {
//                new ContentReductionTask { Id = TestUtil.MakeTestGuid(1), SelectionGroupId = TestUtil.MakeTestGuid(1), ReductionStatus = ReductionStatusEnum.Reducing, SelectionCriteriaObj = new ContentReductionHierarchy<ReductionFieldValueSelection>() }
//            });
//            MockDbSet<ContentReductionTask>.AssignNavigationProperty(DbContext.ContentReductionTask, "SelectionGroupId", DbContext.SelectionGroup);
//            #endregion
//        }

//        private void GenerateFileDropTestData()
//        {
//            #region Initialize Users
//            DbContext.ApplicationUser.AddRange(new List<ApplicationUser>
//            {
//                    new ApplicationUser { Id = TestUtil.MakeTestGuid(1), UserName = "user1", Email = "user1@site.domain", },
//                    new ApplicationUser { Id = TestUtil.MakeTestGuid(2), UserName = "user2", Email = "user2@site.domain", },
//                    new ApplicationUser { Id = TestUtil.MakeTestGuid(3), UserName = "user3", Email = "user3@site.domain",  },
//                    new ApplicationUser { Id = TestUtil.MakeTestGuid(4), UserName = "user4", Email = "user4@site.domain",  },
//                    new ApplicationUser { Id = TestUtil.MakeTestGuid(5), UserName = "user5", Email = "user5@site.domain",  },
//                    new ApplicationUser { Id = TestUtil.MakeTestGuid(6), UserName = "user6", Email = "user6@site.domain",  },
//                    new ApplicationUser { Id = TestUtil.MakeTestGuid(7), UserName = "user7", Email = "user7@site.domain",  },
//                    new ApplicationUser { Id = TestUtil.MakeTestGuid(8), UserName = "user8", Email = "user8@site.domain",  },
//            });
//            MockDbSet<ApplicationUser>.AssignNavigationProperty(DbContext.ApplicationUser, "AuthenticationSchemeId", DbContext.AuthenticationScheme);
//            #endregion

//            #region Initialize ProfitCenters
//            DbContext.ProfitCenter.AddRange(new List<ProfitCenter>
//            {
//                new ProfitCenter { Id = TestUtil.MakeTestGuid(1), Name = "Test Profit Center"},
//            });
//            #endregion

//            #region Initialize Clients
//            DbContext.Client.AddRange(new List<Client>
//            {
//                new Client { Id = TestUtil.MakeTestGuid(1), Name = "Client 1, Parent of client 2", ProfitCenterId = TestUtil.MakeTestGuid(1), ParentClientId = null, AcceptedEmailDomainList = new List<string>{"abc.com", "def.com"} },
//                new Client { Id = TestUtil.MakeTestGuid(2), Name = "Client 2", ProfitCenterId = TestUtil.MakeTestGuid(1), ParentClientId = TestUtil.MakeTestGuid(1), AcceptedEmailDomainList = new List<string>{"abc.com", "def.com"} },
//                new Client { Id = TestUtil.MakeTestGuid(3), Name = "Client 3, no parent or child", ProfitCenterId = TestUtil.MakeTestGuid(1), ParentClientId = null, AcceptedEmailDomainList = new List<string>{"abc.com", "def.com"} },
//            });
//            MockDbSet<Client>.AssignNavigationProperty(DbContext.Client, "ProfitCenterId", DbContext.ProfitCenter);
//            #endregion

//            #region Initialize User associations with Clients
//            #region Initialize UserRoleInClient
//            DbContext.UserRoleInClient.AddRange(new List<UserRoleInClient>
//            {
//                // user1 admin only on parent only
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(6), UserId=TestUtil.MakeTestGuid(1) },
//                // user2 user only on parent only
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(7), UserId=TestUtil.MakeTestGuid(2) },
//                // user3 admin only on child only
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(2), RoleId=TestUtil.MakeTestGuid(6), UserId=TestUtil.MakeTestGuid(3) },
//                // user4 user only on child only
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(2), RoleId=TestUtil.MakeTestGuid(7), UserId=TestUtil.MakeTestGuid(4) },
//                // user5 admin only on parent and child
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(5), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(6), UserId=TestUtil.MakeTestGuid(5) },
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(6), ClientId=TestUtil.MakeTestGuid(2), RoleId=TestUtil.MakeTestGuid(6), UserId=TestUtil.MakeTestGuid(5) },
//                // user6 user only on parent and child
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(7), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(7), UserId=TestUtil.MakeTestGuid(6) },
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(8), ClientId=TestUtil.MakeTestGuid(2), RoleId=TestUtil.MakeTestGuid(7), UserId=TestUtil.MakeTestGuid(6) },
//                // user7 user and admin on both parent and child
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(9), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(6), UserId=TestUtil.MakeTestGuid(7) },
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(10), ClientId=TestUtil.MakeTestGuid(2), RoleId=TestUtil.MakeTestGuid(6), UserId=TestUtil.MakeTestGuid(7) },
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(11), ClientId=TestUtil.MakeTestGuid(1), RoleId=TestUtil.MakeTestGuid(7), UserId=TestUtil.MakeTestGuid(7) },
//                new UserRoleInClient { Id=TestUtil.MakeTestGuid(12), ClientId=TestUtil.MakeTestGuid(2), RoleId=TestUtil.MakeTestGuid(7), UserId=TestUtil.MakeTestGuid(7) },
//            });
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty(DbContext.UserRoleInClient, "ClientId", DbContext.Client);
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty(DbContext.UserRoleInClient, "UserId", DbContext.ApplicationUser);
//            MockDbSet<UserRoleInClient>.AssignNavigationProperty(DbContext.UserRoleInClient, "RoleId", DbContext.ApplicationRole);
//            #endregion

//            #region Initialize UserClaims
//            DbContext.UserClaims.AddRange(new List<IdentityUserClaim<Guid>>
//            {
//                // all users members of all clients
//                // Client 1
//                new IdentityUserClaim<Guid>{ Id = 1, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(1).ToString(), UserId = TestUtil.MakeTestGuid(1) },
//                new IdentityUserClaim<Guid>{ Id = 2, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(1).ToString(), UserId = TestUtil.MakeTestGuid(2) },
//                new IdentityUserClaim<Guid>{ Id = 3, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(1).ToString(), UserId = TestUtil.MakeTestGuid(3) },
//                new IdentityUserClaim<Guid>{ Id = 3, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(1).ToString(), UserId = TestUtil.MakeTestGuid(4) },
//                // Client 2
//                new IdentityUserClaim<Guid>{ Id = 1, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(2).ToString(), UserId = TestUtil.MakeTestGuid(1) },
//                new IdentityUserClaim<Guid>{ Id = 2, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(2).ToString(), UserId = TestUtil.MakeTestGuid(2) },
//                new IdentityUserClaim<Guid>{ Id = 3, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(2).ToString(), UserId = TestUtil.MakeTestGuid(3) },
//                new IdentityUserClaim<Guid>{ Id = 3, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(2).ToString(), UserId = TestUtil.MakeTestGuid(4) },
//                // Client 3
//                new IdentityUserClaim<Guid>{ Id = 1, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(3).ToString(), UserId = TestUtil.MakeTestGuid(1) },
//                new IdentityUserClaim<Guid>{ Id = 2, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(3).ToString(), UserId = TestUtil.MakeTestGuid(2) },
//                new IdentityUserClaim<Guid>{ Id = 3, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(3).ToString(), UserId = TestUtil.MakeTestGuid(3) },
//                new IdentityUserClaim<Guid>{ Id = 3, ClaimType = ClaimNames.ClientMembership.ToString(), ClaimValue = TestUtil.MakeTestGuid(3).ToString(), UserId = TestUtil.MakeTestGuid(4) },
//});
//            #endregion
//            #endregion 

//            #region Initialize UserRoles
//            DbContext.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
//            { 
//                    //new IdentityUserRole<Guid> { RoleId=((long) RoleEnum.Admin), UserId=TestUtil.MakeTestGuid(1) },
//                    new IdentityUserRole<Guid> { RoleId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },
//            });
//            #endregion

//            #region Initialize FileDrops
//            DbContext.FileDrop.AddRange(new List<FileDrop>
//            {
//                new FileDrop { Id = TestUtil.MakeTestGuid(1), Name = "FileDrop 1", ClientId = TestUtil.MakeTestGuid(1), RootPath = TestUtil.MakeTestGuid(1).ToString(), SftpAccounts = new List<SftpAccount>() },
//                new FileDrop { Id = TestUtil.MakeTestGuid(2), Name = "FileDrop 2", ClientId = TestUtil.MakeTestGuid(2), RootPath = TestUtil.MakeTestGuid(2).ToString(), SftpAccounts = new List<SftpAccount>() },
//                new FileDrop { Id = TestUtil.MakeTestGuid(3), Name = "FileDrop 3", ClientId = TestUtil.MakeTestGuid(3), RootPath = TestUtil.MakeTestGuid(3).ToString(), SftpAccounts = new List<SftpAccount>() },
//            });
//            foreach (FileDrop d in DbContext.FileDrop)
//            {
//                string fileDropRootFolder = Path.Combine(Configuration.GetValue<string>("Storage:FileDropRoot"), d.RootPath);
//                Directory.CreateDirectory(fileDropRootFolder);
//            }

//            MockDbSet<FileDrop>.AssignNavigationProperty<Client>(DbContext.FileDrop, nameof(FileDrop.ClientId), DbContext.Client);
//            #endregion

//            #region Initialize FileDropPermissionGroups
//            DbContext.FileDropUserPermissionGroup.AddRange(new List<FileDropUserPermissionGroup>
//            {
//                new FileDropUserPermissionGroup { Id = TestUtil.MakeTestGuid(1), Name = "user 2 in FileDrop 1", FileDropId = TestUtil.MakeTestGuid(1) },
//                new FileDropUserPermissionGroup { Id = TestUtil.MakeTestGuid(2), Name = "user 4 in FileDrop 2", FileDropId = TestUtil.MakeTestGuid(2) },
//                new FileDropUserPermissionGroup { Id = TestUtil.MakeTestGuid(3), Name = "user 6 in FileDrop 1", FileDropId = TestUtil.MakeTestGuid(1) },
//                new FileDropUserPermissionGroup { Id = TestUtil.MakeTestGuid(4), Name = "user 6 in FileDrop 2", FileDropId = TestUtil.MakeTestGuid(2) },
//                new FileDropUserPermissionGroup { Id = TestUtil.MakeTestGuid(5), Name = "user 7 in FileDrop 1", FileDropId = TestUtil.MakeTestGuid(1) },
//                new FileDropUserPermissionGroup { Id = TestUtil.MakeTestGuid(6), Name = "user 7 in FileDrop 2", FileDropId = TestUtil.MakeTestGuid(2) },
//            });
//            MockDbSet<FileDropUserPermissionGroup>.AssignNavigationProperty<FileDrop>(DbContext.FileDropUserPermissionGroup, nameof(FileDropUserPermissionGroup.FileDropId), DbContext.FileDrop);
//            #endregion

//            #region Initialize SftpAccount
//            DbContext.SftpAccount.AddRange(new List<SftpAccount>
//                {
//                    new SftpAccount(fileDropId: TestUtil.MakeTestGuid(1)) { Id = TestUtil.MakeTestGuid(1), ApplicationUserId = TestUtil.MakeTestGuid(2), FileDropUserPermissionGroupId = TestUtil.MakeTestGuid(1) },
//                    new SftpAccount(fileDropId: TestUtil.MakeTestGuid(2)) { Id = TestUtil.MakeTestGuid(2), ApplicationUserId = TestUtil.MakeTestGuid(4), FileDropUserPermissionGroupId = TestUtil.MakeTestGuid(2) },
//                    new SftpAccount(fileDropId: TestUtil.MakeTestGuid(1)) { Id = TestUtil.MakeTestGuid(3), ApplicationUserId = TestUtil.MakeTestGuid(6), FileDropUserPermissionGroupId = TestUtil.MakeTestGuid(3) },
//                    new SftpAccount(fileDropId: TestUtil.MakeTestGuid(2)) { Id = TestUtil.MakeTestGuid(4), ApplicationUserId = TestUtil.MakeTestGuid(6), FileDropUserPermissionGroupId = TestUtil.MakeTestGuid(4) },
//                    new SftpAccount(fileDropId: TestUtil.MakeTestGuid(1)) { Id = TestUtil.MakeTestGuid(5), ApplicationUserId = TestUtil.MakeTestGuid(7), FileDropUserPermissionGroupId = TestUtil.MakeTestGuid(5) },
//                    new SftpAccount(fileDropId: TestUtil.MakeTestGuid(2)) { Id = TestUtil.MakeTestGuid(6), ApplicationUserId = TestUtil.MakeTestGuid(7), FileDropUserPermissionGroupId = TestUtil.MakeTestGuid(6) },
//                });
//            MockDbSet<SftpAccount>.AssignNavigationProperty<ApplicationUser>(DbContext.SftpAccount, nameof(SftpAccount.ApplicationUserId), DbContext.ApplicationUser);
//            MockDbSet<SftpAccount>.AssignNavigationProperty<FileDrop>(DbContext.SftpAccount, nameof(SftpAccount.FileDropId), DbContext.FileDrop);
//            #endregion

//            //MockDbSet<SftpAccount>.AssignReverseNavigationProperty<ApplicationUser>(DbContext.SftpAccount, nameof(SftpAccount.ApplicationUserId), DbContext.ApplicationUser, nameof(ApplicationUser.SftpAccounts));
//        }

//        private void ConnectServicesToData()
//        {
//            // Build initialization data for WsFederation options provider
//            IOptionsMonitorCache<WsFederationOptions> wsfedOptionSvc = (IOptionsMonitorCache<WsFederationOptions>)ScopedServiceProvider.GetService(typeof(IOptionsMonitorCache<WsFederationOptions>));
//            IOptionsMonitorCache<CookieAuthenticationOptions> cookieOptionSvc = (IOptionsMonitorCache<CookieAuthenticationOptions>)ScopedServiceProvider.GetService(typeof(IOptionsMonitorCache<CookieAuthenticationOptions>));

//            var initData = new List<KeyValuePair<string, WsFederationOptions>>();
//            foreach (var scheme in DbContext.AuthenticationScheme)
//            {
//                Type handlerType = null;
//                switch (scheme.Type)
//                {
//                    case AuthenticationType.WsFederation:
//                        WsFederationSchemeProperties props = (WsFederationSchemeProperties)scheme.SchemePropertiesObj;
//                        WsFederationOptions wsOptions = new WsFederationOptions
//                        {
//                            MetadataAddress = props.MetadataAddress,
//                            Wtrealm = props.Wtrealm,
//                        };
//                        wsOptions.CallbackPath += $"-{scheme.Name}";
//                        wsfedOptionSvc.TryAdd(scheme.Name, wsOptions);
//                        handlerType = typeof(WsFederationHandler);
//                        break;

//                    /*
//                    case AuthenticationType.Default:
//                        var cookieOptions = new CookieAuthenticationOptions
//                        {
//                            LoginPath = "/Account/LogIn",
//                            LogoutPath = "/Account/LogOut",
//                            ExpireTimeSpan = TimeSpan.FromMinutes(30),
//                            SlidingExpiration = true,
//                        };
//                        handlerType = typeof(CookieAuthenticationHandler);
//                        cookieOptionSvc.TryAdd(scheme.Name, cookieOptions);
//                        break;
//                    */
//                }

//                if (handlerType != null)
//                {
//                    AuthenticationSchemeProvider.AddScheme(new Microsoft.AspNetCore.Authentication.AuthenticationScheme(scheme.Name, scheme.DisplayName, handlerType));
//                }
//            }
//        }

//        private IConfiguration GenerateConfiguration()
//        {
//            var configurationBuilder = new ConfigurationBuilder();
//            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

//            configurationBuilder.AddJsonFile("appsettings.json", true);
//            configurationBuilder.AddJsonFile($"appsettings.{environmentName}.json", true);
//            configurationBuilder.AddUserSecrets<TestInitialization>();

//            return configurationBuilder.Build();
//        }

//        public void Test()
//        {
//            int roleCount = default;
//            UserManager<ApplicationUser> userManager = default;

//            using (var db = Scope.ServiceProvider.GetService<ApplicationDbContext>())
//            {
//                roleCount = db.ApplicationRole.Count();
//            }

//            using (IServiceScope scope = ServiceScopeFactory.CreateScope())
//            {
//                userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
//            }

//            Debug.Assert(roleCount != default);
//            Debug.Assert(userManager != default);
//        }
//    }
//}
