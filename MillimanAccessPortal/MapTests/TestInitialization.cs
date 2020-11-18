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
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.Utilities;
using Moq;
using Newtonsoft.Json;
using PowerBiLib;
using QlikviewLib;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using TestResourcesLib;

namespace MapTests
{
    internal enum DataSelection
    {
        // Important: Keep this enum synchronized with Dictionary DataGenFunctionDict in the constructor
        Basic,
        Reduction,
        Account,
        SystemAdmin,
        FileDrop,
    }

    internal class TestInitialization : IDisposable
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;

        public IServiceProvider ServiceProvider { get; private set; } = default;
        public IServiceScope Scope { get; private set; } = default;
        public IServiceScopeFactory ServiceScopeFactory { get; private set; } = default;

        #region Scoped registered services
        public ApplicationDbContext DbContext { get; private set; } = default;
        public UserManager<ApplicationUser> UserManager { get; private set; } = default;
        public RoleManager<ApplicationRole> RoleManager { get; private set; } = default;
        public SignInManager<ApplicationUser> SignInManager { get; private set; } = default;
        public IAuditLogger AuditLogger { get; private set; } = default;
        public IAuthorizationService AuthorizationService { get; private set; } = default;
        public IConfiguration Configuration { get; private set; } = default;
        public IServiceProvider ScopedServiceProvider { get; private set; } = default;
        public IAuthenticationService AuthenticationService { get; private set; } = default;
        public IOptions<PowerBiConfig> PowerBiConfig { get; private set; } = default;
        public IOptions<QlikviewConfig> QvConfig { get; private set; } = default;
        public IAuthenticationSchemeProvider AuthenticationSchemeProvider { get; private set; } = default;
        public StandardQueries StandardQueries { get; set; } = default;
        public ContentAccessAdminQueries ContentAccessAdminQueries { get; set; } = default;
        public ClientAccessReviewQueries ClientAccessReviewQueries { get; set; } = default;
        public ContentPublishingAdminQueries ContentPublishingAdminQueries { get; set; } = default;
        public FileDropQueries FileDropQueries { get; set; } = default;
        public ClientQueries ClientQueries { get; set; } = default;
        public ContentItemQueries ContentItemQueries { get; set; } = default;
        public HierarchyQueries HierarchyQueries { get; set; } = default;
        public SelectionGroupQueries SelectionGroupQueries { get; set; } = default;
        public PublicationQueries PublicationQueries { get; set; } = default;
        public UserQueries UserQueries { get; set; } = default;
        public AuthorizedContentQueries AuthorizedContentQueries { get; set; } = default;
        public FileSystemTasks FileSystemTasks { get; set; } = default;
        public IUploadHelper UploadHelper { get; set; } = default;
        public IUrlHelper UrlHelper { get; set; } = default;
        public ClientAdminQueries ClientAdminQueries { get; set; } = default;
        #endregion

        #region Transient registered services
        public IMessageQueue MessageQueueServicesObject { get; private set; } = default;
        #endregion

        #region Singleton registered services
        public IGoLiveTaskQueue GoLiveTaskQueue { get; set; } = default;
        public IPublicationPostProcessingTaskQueue PublicationPostProcessingTaskQueue { get; set; } = default;
        public IUploadTaskQueue UploadTaskQueue { get; set; } = default;
        public IFileProvider FileProvider { get; set; } = default;
        #endregion

        private TestInitialization() { }

        public static async Task<TestInitialization> Create(DatabaseLifetimeFixture dbLifeTimeFixture, DataSelection dataSelection)
        {
            TestInitialization returnVal = new TestInitialization();

            returnVal._dbLifeTimeFixture = dbLifeTimeFixture;

            returnVal.Configuration = dbLifeTimeFixture.Config;

            returnVal.ConfigureInjectedServices();

            returnVal.ServiceScopeFactory = returnVal.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            returnVal.Scope = returnVal.ServiceScopeFactory.CreateScope();

            returnVal.InitializeInjectedServices();

            //returnVal.DbContext.Database.EnsureCreated();

            returnVal.ClearAllData();
            await returnVal.GenerateTestData(dataSelection);

            return returnVal;
        }

        public void Dispose()
        {
            //ClearAllData();

            Scope.Dispose();
        }

        private void ConfigureInjectedServices()
        {
            string tokenProviderName = "MAPResetToken";

            var services = new ServiceCollection();

            services.AddDbContext<ApplicationDbContext>(builder =>
            {
                builder.UseNpgsql(_dbLifeTimeFixture.ConnectionString);
            });

            services.AddDataProtection();

            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<ApplicationRole>()
                .AddSignInManager()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<PasswordResetSecurityTokenProvider<ApplicationUser>>(tokenProviderName)
                .AddTop100000PasswordValidator<ApplicationUser>()
                .AddRecentPasswordInDaysValidator<ApplicationUser>(30)
                .AddPasswordValidator<PasswordIsNotEmailValidator<ApplicationUser>>()
                .AddCommonWordsValidator<ApplicationUser>(new List<string>())
                ;

            services.AddAuthentication(IdentityConstants.ApplicationScheme)
                    .AddIdentityCookies();

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 10;
                options.Password.RequiredUniqueChars = 6;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = false;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;

                // Enable custom token provider for password resets
                options.Tokens.PasswordResetTokenProvider = tokenProviderName;
            });

            services.AddScoped<IAuthorizationHandler, MapAuthorizationHandler>();

            services.AddControllersWithViews(options =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                                    .RequireAuthenticatedUser()
                                    .Build();
                    options.Filters.Add(new AuthorizeFilter(policy));
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
                })
                .AddControllersAsServices();

            // Configure custom security token provider
            services.Configure<PasswordResetSecurityTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromHours(3);
            });

            // Configure the default token provider used for account activation
            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromDays(7);
            });

            services.Configure<QlikviewConfig>(Configuration);
            services.Configure<PowerBiConfig>(Configuration);
            //services.Configure<AuditLoggerConfiguration>(Configuration);
            //services.Configure<SmtpConfig>(Configuration);

            services.AddScoped<IConfiguration>(p => _dbLifeTimeFixture.Config);

            //services.AddScoped<ApplicationDbContext, MockableMapDbContext>();
            services.AddScoped<StandardQueries>();
            services.AddScoped<ContentAccessAdminQueries>();
            services.AddScoped<ClientAccessReviewQueries>();
            services.AddScoped<ContentPublishingAdminQueries>();
            services.AddScoped<FileDropQueries>();
            services.AddScoped<FileSystemTasks>();
            services.AddScoped<IAuditLogger>(p => MockAuditLogger.New().Object);
            services.AddScoped<IUploadHelper, UploadHelper>();
            services.AddScoped<ClientQueries>();
            services.AddScoped<ContentItemQueries>();
            services.AddScoped<HierarchyQueries>();
            services.AddScoped<SelectionGroupQueries>();
            services.AddScoped<PublicationQueries>();
            services.AddScoped<AuthorizedContentQueries>();
            services.AddScoped<UserQueries>();
            services.AddScoped<ClientAdminQueries>();

            string fileUploadPath = Path.GetTempPath();
            // The environment variable check enables migrations to be deployed to Staging or Production via the MAP deployment server
            // This variable should never be set on a real production or staging system
            if (Configuration != null && !string.IsNullOrWhiteSpace(Configuration.GetValue<string>("Storage:FileUploadPath")) && Environment.GetEnvironmentVariable("MIGRATIONS_RUNNING") == null)
            {
                fileUploadPath = Configuration.GetValue<string>("Storage:FileUploadPath");
            }
            services.AddSingleton<IFileProvider>(new PhysicalFileProvider(fileUploadPath));
            services.AddSingleton<IGoLiveTaskQueue, GoLiveTaskQueue>();
            services.AddSingleton<IPublicationPostProcessingTaskQueue, PublicationPostProcessingTaskQueue>();
            services.AddSingleton<IUploadTaskQueue, UploadTaskQueue>();

            services.AddTransient<IMessageQueue, MessageQueueServices>();

            ServiceProvider = services.BuildServiceProvider();
        }

        private void InitializeInjectedServices()
        {
            #region Scoped registered services
            DbContext = Scope.ServiceProvider.GetService<ApplicationDbContext>();
            UserManager = Scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
            RoleManager = Scope.ServiceProvider.GetService<RoleManager<ApplicationRole>>();
            SignInManager = Scope.ServiceProvider.GetService<SignInManager<ApplicationUser>>();
            AuditLogger = Scope.ServiceProvider.GetService<IAuditLogger>();
            AuthorizationService = Scope.ServiceProvider.GetService<IAuthorizationService>();
            Configuration = Scope.ServiceProvider.GetService<IConfiguration>();
            ScopedServiceProvider = Scope.ServiceProvider;
            AuthenticationService = Scope.ServiceProvider.GetService<IAuthenticationService>();
            AuthenticationSchemeProvider = ScopedServiceProvider.GetService<IAuthenticationSchemeProvider>();
            StandardQueries = ScopedServiceProvider.GetService<StandardQueries>();
            ContentAccessAdminQueries = ScopedServiceProvider.GetService<ContentAccessAdminQueries>();
            ClientAccessReviewQueries = ScopedServiceProvider.GetService<ClientAccessReviewQueries>();
            ContentPublishingAdminQueries = ScopedServiceProvider.GetService<ContentPublishingAdminQueries>();
            FileDropQueries = ScopedServiceProvider.GetService<FileDropQueries>();
            FileSystemTasks = ScopedServiceProvider.GetService<FileSystemTasks>();
            UploadHelper = ScopedServiceProvider.GetService<IUploadHelper>();
            ClientQueries = ScopedServiceProvider.GetService<ClientQueries>();
            ClientAdminQueries = ScopedServiceProvider.GetService<ClientAdminQueries>();
            ContentItemQueries = ScopedServiceProvider.GetService<ContentItemQueries>();
            HierarchyQueries = ScopedServiceProvider.GetService<HierarchyQueries>();
            SelectionGroupQueries = ScopedServiceProvider.GetService<SelectionGroupQueries>();
            PublicationQueries = ScopedServiceProvider.GetService<PublicationQueries>();
            UserQueries = ScopedServiceProvider.GetService<UserQueries>();
            AuthorizedContentQueries = ScopedServiceProvider.GetService<AuthorizedContentQueries>();
            FileProvider = ScopedServiceProvider.GetService<IFileProvider>();
            QvConfig = ScopedServiceProvider.GetService<IOptions<QlikviewConfig>>();
            PowerBiConfig = ScopedServiceProvider.GetService<IOptions<PowerBiConfig>>();
            #endregion

            #region Transient registered services
            //MailSender.ConfigureMailSender(new SmtpConfig());
            //MessageQueueServicesObject = ServiceProvider.GetService<IMessageQueue>();
            MessageQueueServicesObject = new Mock<IMessageQueue>().Object;
            #endregion

            #region Singleton registered services
            GoLiveTaskQueue = ServiceProvider.GetService<IGoLiveTaskQueue>();
            PublicationPostProcessingTaskQueue = ServiceProvider.GetService<IPublicationPostProcessingTaskQueue>();
            UploadTaskQueue = ServiceProvider.GetService<IUploadTaskQueue>();
            #endregion
        }

        /// <summary>
        /// Initializes a ControllerContext based on a user name. 
        /// </summary>
        /// <param name="userName">The user to be impersonated in the ControllerContext</param>
        /// <returns></returns>
        public ControllerContext GenerateControllerContext(string userName = null, UriBuilder requestUriBuilder = null, Dictionary<string, StringValues> requestHeaders = null)
        {
            ClaimsPrincipal TestUserClaimsPrincipal = new ClaimsPrincipal();
            if (!string.IsNullOrWhiteSpace(userName))
            {
                ApplicationUser userRecord = DbContext.ApplicationUser.Single(u => u.UserName == userName);
                Claim[] newIdentity = new[] 
                { 
                    new Claim(ClaimTypes.Name, userName), 
                    new Claim(ClaimTypes.NameIdentifier, userRecord.Id.ToString()) 
                };
                TestUserClaimsPrincipal.AddIdentity(new ClaimsIdentity(newIdentity));
            }

            ControllerContext returnVal =  GenerateControllerContext(TestUserClaimsPrincipal, requestUriBuilder, requestHeaders);

            return returnVal;
        }

        /// <summary>
        /// Initializes a ControllerContext as needed to construct a functioning controller. 
        /// </summary>
        /// <param name="UserAsClaimsPrincipal">The user to be impersonated in the ControllerContext</param>
        /// <returns></returns>
        public ControllerContext GenerateControllerContext(ClaimsPrincipal UserAsClaimsPrincipal, UriBuilder requestUriBuilder = null, Dictionary<string, StringValues> requestHeaders = null)
        {
            ControllerContext returnVal = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() 
                { 
                    User = UserAsClaimsPrincipal,
                    RequestServices = ScopedServiceProvider,
                },
                ActionDescriptor = new ControllerActionDescriptor { ActionName = "Unit Test" }
            };

            SignInManager.Context = returnVal.HttpContext;

            if (requestUriBuilder != null)
            {
                returnVal.HttpContext.Request.Scheme = requestUriBuilder.Scheme;
                returnVal.HttpContext.Request.Host = requestUriBuilder.Port > 0
                    ? new HostString(requestUriBuilder.Host, requestUriBuilder.Port)
                    : new HostString(requestUriBuilder.Host);
                returnVal.HttpContext.Request.Path = requestUriBuilder.Path;

                if (!string.IsNullOrWhiteSpace(requestUriBuilder.Query))
                {
                    var listOfQueries = requestUriBuilder.Query.Substring(1).Split('&', StringSplitOptions.RemoveEmptyEntries).ToList();
                    Dictionary<string, StringValues> dict = new Dictionary<string, StringValues>();
                    foreach (string query in listOfQueries)
                    {
                        var keyAndValue = query.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                        dict.Add(keyAndValue[0], keyAndValue[1]);
                    }
                    returnVal.HttpContext.Request.Query = new QueryCollection(dict);
                }
            }

            if (requestHeaders != null)
            {
                foreach (var header in requestHeaders)
                {
                    returnVal.HttpContext.Request.Headers.Add(header);
                }
            }

            return returnVal;
        }

        private async Task GenerateTestData(DataSelection dataSelection)
        {
            /// The following does 4 things using the same code as MAP production application
            /// 1. Seed the db with all roles using the enumeration in class ApplicationRole
            /// 2. Seed the db with all content Types using the enumeration in class ContentType
            /// 3. Seed the db with the default authentication scheme
            /// 4. Seed the db with name/value configuration as in 
            await ApplicationDbContext.InitializeAllAsync(ScopedServiceProvider);
            await DbContext.ApplicationRole.LoadAsync();
            await DbContext.ContentType.LoadAsync();
            await DbContext.AuthenticationScheme.LoadAsync();
            await DbContext.NameValueConfiguration.LoadAsync();

            switch (dataSelection)
            {
                case DataSelection.Account:
                    await GenerateAccountTestData();
                    break;
                case DataSelection.Basic:
                    await GenerateBasicTestData();
                    break;
                case DataSelection.Reduction:
                    await GenerateReductionTestData();
                    break;
                case DataSelection.SystemAdmin:
                    await GenerateSystemAdminTestData();
                    break;
                case DataSelection.FileDrop:
                    await GenerateFileDropTestData();
                    break;
            }

            ConnectServicesToData();
        }

        private async Task GenerateBasicTestData()
        {
            #region Initialize Users
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(1), UserName = "test1", Email = "test1@example.com", Employer = "example", FirstName = "FN1", LastName = "LN1", PhoneNumber = "3171234567" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(2), UserName = "test2", Email = "test2@example.com", Employer = "example", FirstName = "FN2", LastName = "LN2", PhoneNumber = "3171234567" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(3), UserName = "ClientAdmin1", Email = "clientadmin1@example2.com", Employer = "example", FirstName = "Client", LastName = "Admin1", PhoneNumber = "3171234567" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(4), UserName = "test3", Email = "test3@example2.com", Employer = "example", FirstName = "FN3", LastName = "LN3", PhoneNumber = "3171234567" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(5), UserName = "user5", Email = "user5@example.com", Employer = "example", FirstName = "FN5", LastName = "LN5", PhoneNumber = "1234567890" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(6), UserName = "user6", Email = "user6@example.com", Employer = "example", FirstName = "FN6", LastName = "LN6", PhoneNumber = "1234567890" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(7), UserName = "AdminOfChildClient", Email = "AdminOfChildClient@example.com", Employer = "example", FirstName = "FN7", LastName = "LN7", PhoneNumber = "1234567890" });
            DbContext.ApplicationUser.Load();
            #endregion

            #region Initialize ProfitCenters
            DbContext.ProfitCenter.AddRange(new List<ProfitCenter>
                {
                    new ProfitCenter { Id=TestUtil.MakeTestGuid(1), Name="Profit Center 1", ProfitCenterCode="pc1" },
                    new ProfitCenter { Id=TestUtil.MakeTestGuid(2), Name="Profit Center 2", ProfitCenterCode="pc2" },
                });
            #endregion

            #region Initialize UserRoleInProfitCenter
            DbContext.UserRoleInProfitCenter.AddRange(new List<UserRoleInProfitCenter>
            {
                new UserRoleInProfitCenter { Id=TestUtil.MakeTestGuid(1), ProfitCenterId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(3), RoleId = DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id }
            });
            #endregion

            #region Initialize Clients
            DbContext.Client.AddRange(new List<Client>
                {
                    new Client { Id=TestUtil.MakeTestGuid(1), Name="Name1", ClientCode="ClientCode1", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" }  },
                    new Client { Id=TestUtil.MakeTestGuid(2), Name="Name2", ClientCode="ClientCode2", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=TestUtil.MakeTestGuid(1),    AcceptedEmailDomainList=new List<string> { "example.com" }  },
                    new Client { Id=TestUtil.MakeTestGuid(3), Name="Name3", ClientCode="ClientCode3", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example2.com" } },
                    new Client { Id=TestUtil.MakeTestGuid(4), Name="Name4", ClientCode="ClientCode4", ProfitCenterId=TestUtil.MakeTestGuid(2), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example2.com" } },
                    new Client { Id=TestUtil.MakeTestGuid(5), Name="Name5", ClientCode="ClientCode5", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example2.com" } },
                    new Client { Id=TestUtil.MakeTestGuid(6), Name="Name6", ClientCode="ClientCode6", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=TestUtil.MakeTestGuid(1),    AcceptedEmailDomainList=new List<string> { "example2.com" } },
                    new Client { Id=TestUtil.MakeTestGuid(7), Name="Name7", ClientCode="ClientCode7", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" } },
                    new Client { Id=TestUtil.MakeTestGuid(8), Name="Name8", ClientCode="ClientCode8", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=TestUtil.MakeTestGuid(7),    AcceptedEmailDomainList=new List<string> { "example.com" } },
                    new Client { Id=TestUtil.MakeTestGuid(9), Name="Name9", ClientCode="ClientCode9", ProfitCenterId=TestUtil.MakeTestGuid(1),
                     ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" } },
                });
            #endregion

            #region Initialize User associations with Clients
            /*
             * There has to be a UserClaim for each user who is associated with a client
             * 
             * The number of user claims will not necessarily match the number of UserRoleForClient records, 
             *      since a user can have multiple roles with a client
             */

            #region Initialize UserRoleInClient
            DbContext.UserRoleInClient.AddRange(new List<UserRoleInClient>
                    {
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.UserCreator).Id, UserId=TestUtil.MakeTestGuid(1) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(3) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(4), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(3) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(5), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(3) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(5), ClientId=TestUtil.MakeTestGuid(6), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(3) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(6), ClientId=TestUtil.MakeTestGuid(5), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(2) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(7), ClientId=TestUtil.MakeTestGuid(7), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(3) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(8), ClientId=TestUtil.MakeTestGuid(8), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(3) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(9), ClientId=TestUtil.MakeTestGuid(8), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentAccessAdmin).Id, UserId=TestUtil.MakeTestGuid(5) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(10), ClientId=TestUtil.MakeTestGuid(8), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentAccessAdmin).Id, UserId=TestUtil.MakeTestGuid(6) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(11), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.UserCreator).Id, UserId=TestUtil.MakeTestGuid(2) }, // this record is intentionally without a respective claim
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(12), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(1) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(13), ClientId=TestUtil.MakeTestGuid(2), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(7) },
                        new UserRoleInClient { Id=TestUtil.MakeTestGuid(14), ClientId=TestUtil.MakeTestGuid(7), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(7) },
                    });
            #endregion

            #region Initialize UserClaims
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("ClientAdmin1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("ClientAdmin1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(4).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("ClientAdmin1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(5).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("test1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("test2"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(5).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("ClientAdmin1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(6).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("ClientAdmin1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(7).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("ClientAdmin1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(8).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user5"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(8).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user6"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(8).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user5"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("AdminOfChildClient"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(7).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("ClientAdmin1"), new Claim(ClaimNames.ClientMembership.ToString(),
             TestUtil.MakeTestGuid(9).ToString()));
            DbContext.UserClaims.Load();
            #endregion
            #endregion

            #region Initialize RootContentItem
            DbContext.RootContentItem.AddRange(new List<RootContentItem>
                {
                    new RootContentItem{ Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 1", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id,
                        ContentFilesList = new List<ContentRelatedFile>{
                            new ContentRelatedFile {
                                FileOriginalName = "filename",
                                FilePurpose = "mastercontent",
                            },
                        },
                    },
                    new RootContentItem{ Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(2), ContentName="RootContent 2", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id },
                    new RootContentItem{ Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(8), ContentName="RootContent 3", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id },
                    new RootContentItem{ Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 4", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id },
                    new RootContentItem{ Id=TestUtil.MakeTestGuid(5), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 5", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id },
                    new RootContentItem{ Id=TestUtil.MakeTestGuid(6), ClientId=TestUtil.MakeTestGuid(7), ContentName="RootContent 6", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id },
                });
            #endregion

            #region Initialize HierarchyField
            DbContext.HierarchyField.AddRange(new List<HierarchyField>
                {
                    new HierarchyField { Id=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName1", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                });
            #endregion

            #region Initialize HierarchyFieldValue
            DbContext.HierarchyFieldValue.AddRange(new List<HierarchyFieldValue>
                {
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(1), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 1" },
                });
            #endregion

            #region Initialize SelectionGroups
            DbContext.SelectionGroup.AddRange(new List<SelectionGroup>
                {
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(1), ContentInstanceUrl="Folder1/File1", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group1 For Content1" },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(2), ContentInstanceUrl="Folder1/File2", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group2 For Content1" },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(3), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group1 For Content2" },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(4), ContentInstanceUrl="Folder3/File1", RootContentItemId=TestUtil.MakeTestGuid(3), GroupName="Group1 For Content3" },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(5), ContentInstanceUrl="Folder3/File2", RootContentItemId=TestUtil.MakeTestGuid(3), GroupName="Group2 For Content3" },
                });
            #endregion

            #region Initialize UserInSelectionGroups
            DbContext.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
                {
                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(1), SelectionGroupId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },
                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(2), SelectionGroupId=TestUtil.MakeTestGuid(4), UserId=TestUtil.MakeTestGuid(3) },
                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(3), SelectionGroupId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(1) },  // duplicate
                });
            #endregion

            #region Initialize UserRoles
            /*
            DbContext.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
                { 
                    new IdentityUserRole<Guid> { RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(1) },
                });
            */
            #endregion

            #region Initialize UserRoleInRootContentItem
            DbContext.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
            {
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(2), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(3), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(3), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(5), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(4), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentAccessAdmin).Id, UserId=TestUtil.MakeTestGuid(5), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(5), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(6), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(6), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentPublisher).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
            });
            #endregion

            #region Initialize FileDrop
            DbContext.FileDrop.AddRange(new List<FileDrop>
            {
                new FileDrop { Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(7), Name="Client 7 File Drop 1", ShortHash="abcd", RootPath = "" },
            });
            #endregion

            DbContext.SaveChanges();
        }

        private async Task GenerateReductionTestData()
        {
            #region Initialize Users
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(1), UserName = "user1", Email = "user1@example.com" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(2), UserName = "user2", Email = "user2@example.com" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(3), UserName = "user3", Email = "user3@example.com" });
            DbContext.ApplicationUser.Load();
            #endregion

            #region Initialize ContentType
            /*
            DbContext.ContentType.AddRange(new List<ContentType>
                {
                    new ContentType{ Id=TestUtil.MakeTestGuid((int)ContentTypeEnum.Qlikview), TypeEnum=ContentTypeEnum.Qlikview, CanReduce=true },
                    new ContentType{ Id=TestUtil.MakeTestGuid((int)ContentTypeEnum.PowerBi), TypeEnum=ContentTypeEnum.PowerBi, CanReduce=true },
                });
            */
            #endregion

            #region Initialize ProfitCenters
            DbContext.ProfitCenter.AddRange(new List<ProfitCenter>
                {
                    new ProfitCenter { Id=TestUtil.MakeTestGuid(1), Name="Profit Center 1", ProfitCenterCode="pc1" },
                });
            #endregion

            #region Initialize Clients
            DbContext.Client.AddRange(new List<Client>
                {
                    new Client { Id=TestUtil.MakeTestGuid(1), Name="Client 1", ClientCode="C1", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" }  },
                    new Client { Id=TestUtil.MakeTestGuid(2), Name="Client 2", ClientCode="C2", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" }  },
                });
            #endregion

            #region Initialize User associations with Clients
            /*
             * There has to be a UserClaim for each user who is associated with a client
             * 
             * The number of user claims will not necessarily match the number of UserRoleForClient records, 
             *      since a user can have multiple roles with a client
             */

            #region Initialize UserRoleInClient
            DbContext.UserRoleInClient.AddRange(new List<UserRoleInClient>
            {
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentAccessAdmin).Id, UserId=TestUtil.MakeTestGuid(1) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentPublisher).Id, UserId=TestUtil.MakeTestGuid(1) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(1) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(2) },
            });
            #endregion

            #region Initialize UserClaims
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user2"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));
            DbContext.ApplicationUser.Load();
            #endregion
            #endregion

            #region Initialize RootContentItem
            DbContext.RootContentItem.AddRange(new List<RootContentItem>
            {
                new RootContentItem{ Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 1", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id, DoesReduce=true },
                new RootContentItem{ Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 2", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id },
                new RootContentItem{ Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 3", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id },
                new RootContentItem{ Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 4", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.PowerBi).Id, TypeSpecificDetail = JsonConvert.SerializeObject(new PowerBiContentItemProperties()) },
            });
            #endregion

            #region Initialize HierarchyField
            DbContext.HierarchyField.AddRange(new List<HierarchyField>
                {
                    new HierarchyField { Id=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName1", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                    new HierarchyField { Id=TestUtil.MakeTestGuid(2), RootContentItemId=TestUtil.MakeTestGuid(2), FieldName="Field2", FieldDisplayName="DisplayName2", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                    new HierarchyField { Id=TestUtil.MakeTestGuid(3), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName3", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                });
            #endregion

            #region Initialize HierarchyFieldValue
            DbContext.HierarchyFieldValue.AddRange(new List<HierarchyFieldValue>
                {
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(1), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 1" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(2), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 2" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(3), HierarchyFieldId=TestUtil.MakeTestGuid(2),  Value="Value 1" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(4), HierarchyFieldId=TestUtil.MakeTestGuid(2),  Value="Value 2" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(5), HierarchyFieldId=TestUtil.MakeTestGuid(3),  Value="Value 1" },
                });
            #endregion

            #region Initialize SelectionGroups
            DbContext.SelectionGroup.AddRange(new List<SelectionGroup>
                {
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(1), ContentInstanceUrl="Folder1/File1", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group1 For Content1", SelectedHierarchyFieldValueList=new List<Guid>() },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(2), ContentInstanceUrl="Folder1/File2", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group2 For Content1", SelectedHierarchyFieldValueList=new List<Guid>() },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(3), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(2), GroupName="Group1 For Content2", SelectedHierarchyFieldValueList=new List<Guid>() },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(4), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(3), GroupName="Group1 For Content3", SelectedHierarchyFieldValueList=new List<Guid>() },
                });
            #endregion

            #region Initialize UserInSelectionGroups
            DbContext.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
                {
                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(1), SelectionGroupId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(2) },
                });
            #endregion

            #region Initialize UserRoles
            /*
            DbContext.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
                {
                    new IdentityUserRole<Guid> { RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(1) },
                });
            */
            #endregion

            #region Initialize UserRoleInRootContentItem
            DbContext.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
            {
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentAccessAdmin).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(2), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(3), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentAccessAdmin).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(4), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentPublisher).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(5), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(6), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(2), RootContentItemId=TestUtil.MakeTestGuid(1) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(7), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentPublisher).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(4) },
            });
            #endregion

            #region Initialize ContentPublicationRequest
            DbContext.ContentPublicationRequest.AddRange(new List<ContentPublicationRequest>
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
            #endregion

            #region Initialize FileUpload
            DbContext.FileUpload.AddRange(new List<FileUpload>
            {
                new FileUpload { Id=TestUtil.MakeTestGuid(1), ClientFileIdentifier="", Status = FileUploadStatus.InProgress, InitiatedDateTimeUtc = DateTime.UtcNow },
            });
            #endregion

            DbContext.SaveChanges();
        }

        private async Task GenerateAccountTestData()
        {
            #region authentication schemes
            DbContext.AuthenticationScheme.AddRange(new List<MapDbContextLib.Context.AuthenticationScheme>
            {
                /*
                new MapDbContextLib.Context.AuthenticationScheme  // AuthenticationType.Default
                {
                    Id = TestUtil.MakeTestGuid(1),
                    Name = IdentityConstants.ApplicationScheme,
                    DisplayName = "The default scheme",
                    Type = AuthenticationType.Default,
                    SchemePropertiesObj = null
                },
                */
                new MapDbContextLib.Context.AuthenticationScheme  // "prmtest", AuthenticationType.WsFederation
                {
                    Id =TestUtil.MakeTestGuid(2),
                    Name = "prmtest",
                    DisplayName = "PRMTest.local Domain",
                    Type = AuthenticationType.WsFederation,
                    SchemePropertiesObj = new WsFederationSchemeProperties
                    {
                        MetadataAddress = "https://adfs.prmtest.local/FederationMetadata/2007-06/FederationMetadata.xml",
                        Wtrealm = "https://localhost:44336"
                    }
                },
                new MapDbContextLib.Context.AuthenticationScheme  // "domainmatch", AuthenticationType.WsFederation
                {
                    Id =TestUtil.MakeTestGuid(3),
                    Name = "domainmatch",
                    DisplayName = "DomainMatch.local Domain",
                    Type = AuthenticationType.WsFederation,
                    SchemePropertiesObj = new WsFederationSchemeProperties
                    {
                        MetadataAddress = "https://adfs.domainmatch.local/FederationMetadata/2007-06/FederationMetadata.xml",
                        Wtrealm = "https://localhost:44336"
                    },
                    DomainList = { "DomainMatch.local" },
                },
            });
            #endregion

            #region Initialize Users
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(1), UserName = "user1", Email = "user1@example.com", TwoFactorEnabled = true });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(2), UserName = "user2", Email = "user2@example.com", EmailConfirmed = true });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(3), UserName = "user3-confirmed-defaultscheme", Email = "user3@example.com", EmailConfirmed = true, AuthenticationSchemeId = DbContext.AuthenticationScheme.Single(s=>s.Type==AuthenticationType.Default).Id });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(4), UserName = "user4-confirmed-wsscheme", Email = "user4@example.com", EmailConfirmed = true, AuthenticationSchemeId = DbContext.AuthenticationScheme.Single(s => s.Name == "prmtest").Id });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(5), UserName = "user5-notconfirmed-wsscheme", Email = "user5@example.com", EmailConfirmed = false, AuthenticationSchemeId = DbContext.AuthenticationScheme.Single(s => s.Name == "domainmatch").Id });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(6), UserName = "user6-confirmed@domainmatch.local", Email = "user6@example.com", EmailConfirmed = false });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(7), UserName = "user7-confirmed@domainnomatch.local", Email = "user7@example.com", EmailConfirmed = false });
            DbContext.ApplicationUser.Load();
            #endregion

            DbContext.SaveChanges();
        }

        private async Task GeneratePublishingTestData()
        {
            #region Initialize Users
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(1), UserName = "user1", Email = "user1@example.com" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(2), UserName = "user2", Email = "user2@example.com" });
            DbContext.ApplicationUser.Load();
            #endregion

            #region Initialize ContentType
            /*
            DbContext.ContentType.AddRange(new List<ContentType>
                {
                    new ContentType{ Id=TestUtil.MakeTestGuid(1), TypeEnum=ContentTypeEnum.Qlikview, CanReduce=true },
                });
            */
            #endregion

            #region Initialize ProfitCenters
            DbContext.ProfitCenter.AddRange(new List<ProfitCenter>
                {
                    new ProfitCenter { Id=TestUtil.MakeTestGuid(1), Name="Profit Center 1", ProfitCenterCode="pc1" },
                });
            #endregion

            #region Initialize Clients
            DbContext.Client.AddRange(new List<Client>
                {
                    new Client { Id=TestUtil.MakeTestGuid(1), Name="Client 1", ClientCode="C1", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" }  },
                    new Client { Id=TestUtil.MakeTestGuid(2), Name="Client 2", ClientCode="C2", ProfitCenterId=TestUtil.MakeTestGuid(1), ParentClientId=null, AcceptedEmailDomainList=new List<string> { "example.com" }  },
                });
            #endregion

            #region Initialize User associations with Clients
            /*
             * There has to be a UserClaim for each user who is associated with a client
             * 
             * The number of user claims will not necessarily match the number of UserRoleForClient records, 
             *      since a user can have multiple roles with a client
             */

            #region Initialize UserRoleInClient
            DbContext.UserRoleInClient.AddRange(new List<UserRoleInClient>
            {
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentAccessAdmin).Id, UserId=TestUtil.MakeTestGuid(1) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentPublisher).Id, UserId=TestUtil.MakeTestGuid(1) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(1) },
                //new UserRoleInClient { Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(2) },
            });
            #endregion

            #region Initialize UserClaims
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user2"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));
            DbContext.ApplicationUser.Load();
            #endregion
            #endregion

            #region Initialize RootContentItem
            DbContext.RootContentItem.AddRange(new List<RootContentItem>
            {
                new RootContentItem{ Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 1", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id },
                new RootContentItem{ Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 2", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id },
                new RootContentItem{ Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(1), ContentName="RootContent 3", ContentTypeId=DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id },
            });
            #endregion

            #region Initialize HierarchyField
            DbContext.HierarchyField.AddRange(new List<HierarchyField>
                {
                    new HierarchyField { Id=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName1", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                    new HierarchyField { Id=TestUtil.MakeTestGuid(2), RootContentItemId=TestUtil.MakeTestGuid(2), FieldName="Field2", FieldDisplayName="DisplayName2", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                    new HierarchyField { Id=TestUtil.MakeTestGuid(3), RootContentItemId=TestUtil.MakeTestGuid(1), FieldName="Field1", FieldDisplayName="DisplayName3", StructureType=FieldStructureType.Flat, FieldDelimiter="|" },
                });
            #endregion

            #region Initialize HierarchyFieldValue
            DbContext.HierarchyFieldValue.AddRange(new List<HierarchyFieldValue>
                {
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(1), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 1" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(2), HierarchyFieldId=TestUtil.MakeTestGuid(1),  Value="Value 2" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(3), HierarchyFieldId=TestUtil.MakeTestGuid(2),  Value="Value 1" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(4), HierarchyFieldId=TestUtil.MakeTestGuid(2),  Value="Value 2" },
                    new HierarchyFieldValue { Id=TestUtil.MakeTestGuid(5), HierarchyFieldId=TestUtil.MakeTestGuid(3),  Value="Value 1" },
                });
            #endregion

            #region Initialize SelectionGroups
            DbContext.SelectionGroup.AddRange(new List<SelectionGroup>
                {
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(1), ContentInstanceUrl="Folder1/File1", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group1 For Content1", SelectedHierarchyFieldValueList=new List<Guid>() },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(2), ContentInstanceUrl="Folder1/File2", RootContentItemId=TestUtil.MakeTestGuid(1), GroupName="Group2 For Content1", SelectedHierarchyFieldValueList=new List<Guid>() },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(3), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(2), GroupName="Group1 For Content2", SelectedHierarchyFieldValueList=new List<Guid>() },
                    new SelectionGroup { Id=TestUtil.MakeTestGuid(4), ContentInstanceUrl="Folder2/File1", RootContentItemId=TestUtil.MakeTestGuid(3), GroupName="Group1 For Content3", SelectedHierarchyFieldValueList=new List<Guid>() },
                });
            #endregion

            #region Initialize UserInSelectionGroups
            DbContext.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
                {
                    new UserInSelectionGroup { Id=TestUtil.MakeTestGuid(1), SelectionGroupId=TestUtil.MakeTestGuid(1), UserId=TestUtil.MakeTestGuid(2) },
                });
            #endregion

            #region Initialize UserRoles
            /*
            DbContext.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
                {
                    new IdentityUserRole<Guid> { RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(1) },
                });
            */
            #endregion

            #region Initialize UserRoleInRootContentItem
            DbContext.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
            {
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentAccessAdmin).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(2), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(1) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(3), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentAccessAdmin).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(4), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentPublisher).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(5), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(1), RootContentItemId=TestUtil.MakeTestGuid(3) },
                new UserRoleInRootContentItem { Id=TestUtil.MakeTestGuid(6), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId=TestUtil.MakeTestGuid(2), RootContentItemId=TestUtil.MakeTestGuid(1) },
            });
            #endregion

            #region Initialize ContentPublicationRequest
            DbContext.ContentPublicationRequest.AddRange(new List<ContentPublicationRequest>
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
            DbContext.FileUpload.AddRange(new List<FileUpload>
            {
                new FileUpload { Id=TestUtil.MakeTestGuid(1) },
            });
            #endregion

            DbContext.SaveChanges();
        }

        private async Task GenerateSystemAdminTestData()
        {
            #region authentication schemes
            DbContext.AuthenticationScheme.AddRange(new List<MapDbContextLib.Context.AuthenticationScheme>
            {
                /*
                new MapDbContextLib.Context.AuthenticationScheme  // AuthenticationType.Default
                {
                    Id = TestUtil.MakeTestGuid(1),
                    Name = IdentityConstants.ApplicationScheme,
                    DisplayName = "The default scheme",
                    Type = AuthenticationType.Default,
                    SchemePropertiesObj = null
                },
                */
                new MapDbContextLib.Context.AuthenticationScheme  // "prmtest", AuthenticationType.WsFederation
                {
                    Id =TestUtil.MakeTestGuid(2),
                    Name = "prmtest",
                    DisplayName = "PRMTest.local Domain",
                    Type = AuthenticationType.WsFederation,
                    SchemePropertiesObj = new WsFederationSchemeProperties
                    {
                        MetadataAddress = "https://adfs.prmtest.local/FederationMetadata/2007-06/FederationMetadata.xml",
                        Wtrealm = "https://localhost:44336"
                    }
                },
                new MapDbContextLib.Context.AuthenticationScheme  // "domainmatch", AuthenticationType.WsFederation
                {
                    Id =TestUtil.MakeTestGuid(3),
                    Name = "domainmatch",
                    DisplayName = "DomainMatch.local Domain",
                    Type = AuthenticationType.WsFederation,
                    SchemePropertiesObj = new WsFederationSchemeProperties
                    {
                        MetadataAddress = "https://adfs.domainmatch.local/FederationMetadata/2007-06/FederationMetadata.xml",
                        Wtrealm = "https://localhost:44336"
                    },
                    DomainList = { "DomainMatch.local" },
                },
            });
            #endregion

            #region Initialize Users
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(1), UserName = "sysAdmin1", Email = "sysAdmin1@site.domain" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(2), UserName = "sysAdmin2", Email = "sysAdmin2@site.domain" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(11), UserName = "sysUser1", Email = "sysUser1@site.domain" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(12), UserName = "sysUser2", Email = "sysUser2@site.domain" });
            DbContext.ApplicationUser.Load();
            #endregion

            #region Initialize ContentType
            /*
            DbContext.ContentType.AddRange(new List<ContentType>
            {
                new ContentType{ Id = TestUtil.MakeTestGuid(1), TypeEnum = ContentTypeEnum.Qlikview, CanReduce = true },
            });
            */
            #endregion

            #region Initialize ProfitCenters
            DbContext.ProfitCenter.AddRange(new List<ProfitCenter>
            {
                new ProfitCenter { Id = TestUtil.MakeTestGuid(1), Name = "Profit Center 1" },
                new ProfitCenter { Id = TestUtil.MakeTestGuid(2), Name = "Profit Center 2" },
            });
            #endregion

            #region Initialize UserRoleInProfitCenter
            DbContext.UserRoleInProfitCenter.AddRange(new List<UserRoleInProfitCenter>
            {
                new UserRoleInProfitCenter { Id = TestUtil.MakeTestGuid(1), ProfitCenterId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(1), RoleId = DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id },
                new UserRoleInProfitCenter { Id = TestUtil.MakeTestGuid(2), ProfitCenterId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(2), RoleId = DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id },
            });
            #endregion

            #region Initialize Clients
            DbContext.Client.AddRange(new List<Client>
            {
                new Client { Id = TestUtil.MakeTestGuid(1), Name="Client 1", ProfitCenterId = TestUtil.MakeTestGuid(1), ParentClientId = null, AcceptedEmailDomainList = new List<string>{"abc.com", "def.com"} },
                new Client { Id = TestUtil.MakeTestGuid(2), Name="Client 2", ProfitCenterId = TestUtil.MakeTestGuid(1), ParentClientId = null, },
            });
            #endregion

            #region Initialize User associations with Clients
            /*
             * There has to be a UserClaim for each user who is associated with a client
             * 
             * The number of user claims will not necessarily match the number of UserRoleForClient records, 
             *      since a user can have multiple roles with a client
             */

            #region Initialize UserRoleInClient
            DbContext.UserRoleInClient.AddRange(new List<UserRoleInClient>
            {
                new UserRoleInClient { Id = TestUtil.MakeTestGuid(1), ClientId = TestUtil.MakeTestGuid(1), RoleId = DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId =  TestUtil.MakeTestGuid(1) },
                new UserRoleInClient { Id = TestUtil.MakeTestGuid(2), ClientId = TestUtil.MakeTestGuid(1), RoleId = DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.ContentUser).Id, UserId = TestUtil.MakeTestGuid(11) },
            });
            #endregion

            #region Initialize UserClaims
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("sysAdmin1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("sysUser1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));
            DbContext.UserClaims.Load();
            #endregion
            #endregion 

            #region Initialize RootContentItem
            DbContext.RootContentItem.AddRange(new List<RootContentItem>
            {
                new RootContentItem{ Id = TestUtil.MakeTestGuid(1), ContentName = "Root Content 1", ClientId = TestUtil.MakeTestGuid(1), ContentTypeId = DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id },
                new RootContentItem{ Id = TestUtil.MakeTestGuid(2), ContentName = "Root Content 2", ClientId = TestUtil.MakeTestGuid(1), ContentTypeId = DbContext.ContentType.Single(t=>t.TypeEnum==ContentTypeEnum.Qlikview).Id },
            });
            #endregion

            #region Initialize SelectionGroups
            DbContext.SelectionGroup.AddRange(new List<SelectionGroup>
            {
                new SelectionGroup { Id = TestUtil.MakeTestGuid(1), ContentInstanceUrl = "Folder1/File1", RootContentItemId = TestUtil.MakeTestGuid(1), GroupName = "Group1 For Content1" },
                new SelectionGroup { Id = TestUtil.MakeTestGuid(2), ContentInstanceUrl = "Folder1/File2", RootContentItemId = TestUtil.MakeTestGuid(1), GroupName = "Group2 For Content1" },
            });
            #endregion

            #region Initialize UserInSelectionGroups
            DbContext.UserInSelectionGroup.AddRange(new List<UserInSelectionGroup>
            {
                new UserInSelectionGroup { Id = TestUtil.MakeTestGuid(1), SelectionGroupId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(11) },
                new UserInSelectionGroup { Id = TestUtil.MakeTestGuid(2), SelectionGroupId = TestUtil.MakeTestGuid(1), UserId = TestUtil.MakeTestGuid(12) },
            });
            #endregion

            #region Initialize UserRoles
            await UserManager.AddToRoleAsync(DbContext.ApplicationUser.Find(TestUtil.MakeTestGuid(1)), "Admin");
            DbContext.UserRoles.Load();
            /*
            DbContext.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
            { 
                new IdentityUserRole<Guid> { RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(1) },
            });
            */
            #endregion

            #region Initialize UserRoleInRootContentItem
            DbContext.UserRoleInRootContentItem.AddRange(new List<UserRoleInRootContentItem>
            {
            });
            #endregion

            #region Initialize ContentPublicationRequest
            DbContext.ContentPublicationRequest.AddRange(new List<ContentPublicationRequest>
            {
                new ContentPublicationRequest { Id = TestUtil.MakeTestGuid(1), RootContentItemId = TestUtil.MakeTestGuid(1), RequestStatus = PublicationStatus.Processing, ApplicationUser = DbContext.ApplicationUser.Single(u => u.UserName == "sysAdmin1") }
            });
            #endregion

            #region Initialize ContentReductionTask
            DbContext.ContentReductionTask.AddRange(new List<ContentReductionTask>
            {
                new ContentReductionTask { Id = TestUtil.MakeTestGuid(1), SelectionGroupId = TestUtil.MakeTestGuid(1), ReductionStatus = ReductionStatusEnum.Reducing, SelectionCriteriaObj = new ContentReductionHierarchy<ReductionFieldValueSelection>(), MasterFilePath = "", ApplicationUser = DbContext.ApplicationUser.Single(u => u.UserName == "sysAdmin1") }
            });
            #endregion

            DbContext.SaveChanges();
        }

        private async Task GenerateFileDropTestData()
        {
            #region Initialize Users
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(1), UserName = "user1", Email = "user1@site.domain" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(2), UserName = "user2", Email = "user2@site.domain" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(3), UserName = "user3", Email = "user3@site.domain" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(4), UserName = "user4", Email = "user4@site.domain" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(5), UserName = "user5", Email = "user5@site.domain" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(6), UserName = "user6", Email = "user6@site.domain" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(7), UserName = "user7", Email = "user7@site.domain" });
            await UserManager.CreateAsync(new ApplicationUser { Id = TestUtil.MakeTestGuid(8), UserName = "user8", Email = "user8@site.domain" });
            DbContext.ApplicationUser.Load();
            #endregion

            #region Initialize ProfitCenters
            DbContext.ProfitCenter.AddRange(new List<ProfitCenter>
            {
                new ProfitCenter { Id = TestUtil.MakeTestGuid(1), Name = "Test Profit Center"},
            });
            #endregion

            #region Initialize Clients
            DbContext.Client.AddRange(new List<Client>
            {
                new Client { Id = TestUtil.MakeTestGuid(1), Name = "Client 1, Parent of client 2", ProfitCenterId = TestUtil.MakeTestGuid(1), ParentClientId = null, AcceptedEmailDomainList = new List<string>{"abc.com", "def.com"} },
                new Client { Id = TestUtil.MakeTestGuid(2), Name = "Client 2", ProfitCenterId = TestUtil.MakeTestGuid(1), ParentClientId = TestUtil.MakeTestGuid(1), AcceptedEmailDomainList = new List<string>{"abc.com", "def.com"} },
                new Client { Id = TestUtil.MakeTestGuid(3), Name = "Client 3, no parent or child", ProfitCenterId = TestUtil.MakeTestGuid(1), ParentClientId = null, AcceptedEmailDomainList = new List<string>{"abc.com", "def.com"} },
            });
            #endregion

            #region Initialize User associations with Clients
            #region Initialize UserRoleInClient
            DbContext.UserRoleInClient.AddRange(new List<UserRoleInClient>
            {
                // user1 admin only on parent only
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(1), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.FileDropAdmin).Id, UserId=TestUtil.MakeTestGuid(1) },
                // user2 user only on parent only
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(2), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.FileDropUser).Id, UserId=TestUtil.MakeTestGuid(2) },
                // user3 admin only on child only
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(3), ClientId=TestUtil.MakeTestGuid(2), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.FileDropAdmin).Id, UserId=TestUtil.MakeTestGuid(3) },
                // user4 user only on child only
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(4), ClientId=TestUtil.MakeTestGuid(2), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.FileDropUser).Id, UserId=TestUtil.MakeTestGuid(4) },
                // user5 admin only on parent and child
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(5), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.FileDropAdmin).Id, UserId=TestUtil.MakeTestGuid(5) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(6), ClientId=TestUtil.MakeTestGuid(2), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.FileDropAdmin).Id, UserId=TestUtil.MakeTestGuid(5) },
                // user6 user only on parent and child
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(7), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.FileDropUser).Id, UserId=TestUtil.MakeTestGuid(6) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(8), ClientId=TestUtil.MakeTestGuid(2), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.FileDropUser).Id, UserId=TestUtil.MakeTestGuid(6) },
                // user7 user and admin on both parent and child
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(9), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.FileDropAdmin).Id, UserId=TestUtil.MakeTestGuid(7) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(10), ClientId=TestUtil.MakeTestGuid(2), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.FileDropAdmin).Id, UserId=TestUtil.MakeTestGuid(7) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(11), ClientId=TestUtil.MakeTestGuid(1), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.FileDropUser).Id, UserId=TestUtil.MakeTestGuid(7) },
                new UserRoleInClient { Id=TestUtil.MakeTestGuid(12), ClientId=TestUtil.MakeTestGuid(2), RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.FileDropUser).Id, UserId=TestUtil.MakeTestGuid(7) },
            });
            #endregion

            #region Initialize UserClaims
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user2"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user3"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user4"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(1).ToString()));

            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(2).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user2"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(2).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user3"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(2).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user4"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(2).ToString()));

            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user1"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(3).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user2"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(3).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user3"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(3).ToString()));
            await UserManager.AddClaimAsync(await UserManager.FindByNameAsync("user4"), new Claim(ClaimNames.ClientMembership.ToString(), TestUtil.MakeTestGuid(3).ToString()));
            DbContext.ApplicationUser.Load();
            #endregion
            #endregion 

            #region Initialize UserRoles
            /*
            DbContext.UserRoles.AddRange(new List<IdentityUserRole<Guid>>
            { 
                new IdentityUserRole<Guid> { RoleId=DbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == RoleEnum.Admin).Id, UserId=TestUtil.MakeTestGuid(1) },
            });
            */
            #endregion

            #region Initialize FileDrops
            DbContext.FileDrop.AddRange(new List<FileDrop>
            {
                new FileDrop { Id = TestUtil.MakeTestGuid(1), Name = "FileDrop 1", ClientId = TestUtil.MakeTestGuid(1), RootPath = TestUtil.MakeTestGuid(1).ToString(), ShortHash = "aaa1", SftpAccounts = new List<SftpAccount>() },
                new FileDrop { Id = TestUtil.MakeTestGuid(2), Name = "FileDrop 2", ClientId = TestUtil.MakeTestGuid(2), RootPath = TestUtil.MakeTestGuid(2).ToString(), ShortHash = "aaa2", SftpAccounts = new List<SftpAccount>() },
                new FileDrop { Id = TestUtil.MakeTestGuid(3), Name = "FileDrop 3", ClientId = TestUtil.MakeTestGuid(3), RootPath = TestUtil.MakeTestGuid(3).ToString(), ShortHash = "aaa3", SftpAccounts = new List<SftpAccount>() },
            });
            foreach (FileDrop d in DbContext.FileDrop)
            {
                string fileDropRootFolder = Path.Combine(Configuration.GetValue<string>("Storage:FileDropRoot"), d.RootPath);
                Directory.CreateDirectory(fileDropRootFolder);
            }

            #endregion

            #region Initialize FileDropPermissionGroups
            DbContext.FileDropUserPermissionGroup.AddRange(new List<FileDropUserPermissionGroup>
            {
                new FileDropUserPermissionGroup { Id = TestUtil.MakeTestGuid(1), Name = "user 2 in FileDrop 1", FileDropId = TestUtil.MakeTestGuid(1) },
                new FileDropUserPermissionGroup { Id = TestUtil.MakeTestGuid(2), Name = "user 4 in FileDrop 2", FileDropId = TestUtil.MakeTestGuid(2) },
                new FileDropUserPermissionGroup { Id = TestUtil.MakeTestGuid(3), Name = "user 6 in FileDrop 1", FileDropId = TestUtil.MakeTestGuid(1) },
                new FileDropUserPermissionGroup { Id = TestUtil.MakeTestGuid(4), Name = "user 6 in FileDrop 2", FileDropId = TestUtil.MakeTestGuid(2) },
                new FileDropUserPermissionGroup { Id = TestUtil.MakeTestGuid(5), Name = "user 7 in FileDrop 1", FileDropId = TestUtil.MakeTestGuid(1) },
                new FileDropUserPermissionGroup { Id = TestUtil.MakeTestGuid(6), Name = "user 7 in FileDrop 2", FileDropId = TestUtil.MakeTestGuid(2) },
            });
            #endregion

            #region Initialize SftpAccount
            DbContext.SftpAccount.AddRange(new List<SftpAccount>
                {
                    new SftpAccount(fileDropId: TestUtil.MakeTestGuid(1)) { Id = TestUtil.MakeTestGuid(1), ApplicationUserId = TestUtil.MakeTestGuid(2), FileDropUserPermissionGroupId = TestUtil.MakeTestGuid(1), UserName = "SFTP user 1-aaa1" },
                    new SftpAccount(fileDropId: TestUtil.MakeTestGuid(2)) { Id = TestUtil.MakeTestGuid(2), ApplicationUserId = TestUtil.MakeTestGuid(4), FileDropUserPermissionGroupId = TestUtil.MakeTestGuid(2), UserName = "SFTP user 2-aaa2" },
                    new SftpAccount(fileDropId: TestUtil.MakeTestGuid(1)) { Id = TestUtil.MakeTestGuid(3), ApplicationUserId = TestUtil.MakeTestGuid(6), FileDropUserPermissionGroupId = TestUtil.MakeTestGuid(3), UserName = "SFTP user 3-aaa1" },
                    new SftpAccount(fileDropId: TestUtil.MakeTestGuid(2)) { Id = TestUtil.MakeTestGuid(4), ApplicationUserId = TestUtil.MakeTestGuid(6), FileDropUserPermissionGroupId = TestUtil.MakeTestGuid(4), UserName = "SFTP user 4-aaa2" },
                    new SftpAccount(fileDropId: TestUtil.MakeTestGuid(1)) { Id = TestUtil.MakeTestGuid(5), ApplicationUserId = TestUtil.MakeTestGuid(7), FileDropUserPermissionGroupId = TestUtil.MakeTestGuid(5), UserName = "SFTP user 5-aaa1" },
                    new SftpAccount(fileDropId: TestUtil.MakeTestGuid(2)) { Id = TestUtil.MakeTestGuid(6), ApplicationUserId = TestUtil.MakeTestGuid(7), FileDropUserPermissionGroupId = TestUtil.MakeTestGuid(6), UserName = "SFTP user 6-aaa2" },
                });
            #endregion

            DbContext.SaveChanges();
        }

        private void ClearAllData()
        {
            DbContext.RoleClaims.RemoveRange(DbContext.RoleClaims);
            DbContext.ApplicationRole.RemoveRange(DbContext.ApplicationRole);
            DbContext.UserClaims.RemoveRange(DbContext.UserClaims);
            DbContext.UserLogins.RemoveRange(DbContext.UserLogins);
            DbContext.UserRoles.RemoveRange(DbContext.UserRoles);
            DbContext.UserTokens.RemoveRange(DbContext.UserTokens);
            DbContext.ApplicationUser.RemoveRange(DbContext.ApplicationUser);
            DbContext.AuthenticationScheme.RemoveRange(DbContext.AuthenticationScheme);
            DbContext.Client.RemoveRange(DbContext.Client);
            DbContext.ContentPublicationRequest.RemoveRange(DbContext.ContentPublicationRequest);
            DbContext.ContentReductionTask.RemoveRange(DbContext.ContentReductionTask);
            DbContext.ContentType.RemoveRange(DbContext.ContentType);
            DbContext.FileDrop.RemoveRange(DbContext.FileDrop);
            DbContext.FileDropDirectory.RemoveRange(DbContext.FileDropDirectory);
            DbContext.FileDropFile.RemoveRange(DbContext.FileDropFile);
            DbContext.FileDropUserPermissionGroup.RemoveRange(DbContext.FileDropUserPermissionGroup);
            DbContext.FileUpload.RemoveRange(DbContext.FileUpload);
            DbContext.HierarchyField.RemoveRange(DbContext.HierarchyField);
            DbContext.HierarchyFieldValue.RemoveRange(DbContext.HierarchyFieldValue);
            DbContext.NameValueConfiguration.RemoveRange(DbContext.NameValueConfiguration);
            DbContext.ProfitCenter.RemoveRange(DbContext.ProfitCenter);
            DbContext.RootContentItem.RemoveRange(DbContext.RootContentItem);
            DbContext.SelectionGroup.RemoveRange(DbContext.SelectionGroup);
            DbContext.SftpAccount.RemoveRange(DbContext.SftpAccount);
            DbContext.UserInSelectionGroup.RemoveRange(DbContext.UserInSelectionGroup);
            DbContext.UserRoleInClient.RemoveRange(DbContext.UserRoleInClient);
            DbContext.UserRoleInProfitCenter.RemoveRange(DbContext.UserRoleInProfitCenter);
            DbContext.UserRoleInRootContentItem.RemoveRange(DbContext.UserRoleInRootContentItem);
            DbContext.SaveChanges();
        }

        private void ConnectServicesToData()
        {
            // Build initialization data for WsFederation options provider
            IOptionsMonitorCache<WsFederationOptions> wsfedOptionSvc = (IOptionsMonitorCache<WsFederationOptions>)ScopedServiceProvider.GetService(typeof(IOptionsMonitorCache<WsFederationOptions>));
            IOptionsMonitorCache<CookieAuthenticationOptions> cookieOptionSvc = (IOptionsMonitorCache<CookieAuthenticationOptions>)ScopedServiceProvider.GetService(typeof(IOptionsMonitorCache<CookieAuthenticationOptions>));

            var initData = new List<KeyValuePair<string, WsFederationOptions>>();
            foreach (var scheme in DbContext.AuthenticationScheme)
            {
                Type handlerType = null;
                switch (scheme.Type)
                {
                    case AuthenticationType.WsFederation:
                        WsFederationSchemeProperties props = (WsFederationSchemeProperties)scheme.SchemePropertiesObj;
                        WsFederationOptions wsOptions = new WsFederationOptions
                        {
                            MetadataAddress = props.MetadataAddress,
                            Wtrealm = props.Wtrealm,
                        };
                        wsOptions.CallbackPath += $"-{scheme.Name}";
                        wsfedOptionSvc.TryAdd(scheme.Name, wsOptions);
                        handlerType = typeof(WsFederationHandler);
                        break;

                    /*
                    case AuthenticationType.Default:
                        var cookieOptions = new CookieAuthenticationOptions
                        {
                            LoginPath = "/Account/LogIn",
                            LogoutPath = "/Account/LogOut",
                            ExpireTimeSpan = TimeSpan.FromMinutes(30),
                            SlidingExpiration = true,
                        };
                        handlerType = typeof(CookieAuthenticationHandler);
                        cookieOptionSvc.TryAdd(scheme.Name, cookieOptions);
                        break;
                    */
                }

                if (handlerType != null)
                {
                    AuthenticationSchemeProvider.AddScheme(new Microsoft.AspNetCore.Authentication.AuthenticationScheme(scheme.Name, scheme.DisplayName, handlerType));
                }
            }
        }

        public static IConfiguration GenerateConfiguration()
        {
            var appConfigurationBuilder = new ConfigurationBuilder();
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            appConfigurationBuilder.AddJsonFile("appsettings.json", true);
            appConfigurationBuilder.AddJsonFile($"appsettings.{environmentName}.json", true);

            // Determine location to fetch the configuration
            switch (environmentName)
            {
                case "CI":
                case "Production": // Get configuration from Azure Key Vault for Production
                    var keyVaultConfig = new ConfigurationBuilder()
                        .AddJsonFile(path: $"AzureKeyVault.{environmentName}.json", optional: false)
                        .Build();

                    var store = new X509Store(StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var cert = store.Certificates.Find(X509FindType.FindByThumbprint, keyVaultConfig["AzureCertificateThumbprint"], false);

                    appConfigurationBuilder.AddAzureKeyVault(
                        keyVaultConfig["AzureVaultName"],
                        keyVaultConfig["AzureClientID"],
                        cert.OfType<X509Certificate2>().Single());
                    break;

                default: // Get connection string from user secrets in Development (ASPNETCORE_ENVIRONMENT is not set during local unit tests)
                    appConfigurationBuilder.AddUserSecrets<TestInitialization>();
                    break;
            }

            IConfiguration returnVal = appConfigurationBuilder.Build();

            if (returnVal.GetValue("DumpVerboseConfiguration", false))
            {
                //This dumps the entire configuration to Serilog.  
                //ConfigurationDumper.DumpConfigurationDetails(environmentName, appConfigurationBuilder, returnVal);
            }

            return returnVal;
        }

        public void Test()
        {
            int roleCount = default;
            UserManager<ApplicationUser> userManager = default;

            using (var db = Scope.ServiceProvider.GetService<ApplicationDbContext>())
            {
                roleCount = db.ApplicationRole.Count();
            }

            using (IServiceScope scope = ServiceScopeFactory.CreateScope())
            {
                userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
            }

            Debug.Assert(roleCount != default);
            Debug.Assert(userManager != default);
        }
    }
}
