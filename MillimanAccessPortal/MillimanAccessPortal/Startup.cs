/*
 * CODE OWNERS: Ben Wyatt, Michael Reisz, Tom Puckett
 * OBJECTIVE: Configure application runtime environment at startup
 * DEVELOPER NOTES: 
 */

using AuditLogLib;
using AuditLogLib.Event;
using AuditLogLib.Services;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.Utilities;
using Newtonsoft.Json;
using PowerBiLib;
using Prm.EmailQueue;
using QlikviewLib;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using MillimanAccessPortal.Models.SharedModels;

namespace MillimanAccessPortal
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string appConnectionString = Configuration.GetConnectionString("DefaultConnection");
            // Add framework services.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(appConnectionString, b => b.MigrationsAssembly("MillimanAccessPortal")));

            int passwordHistoryDays = Configuration.GetValue<int?>("PasswordHistoryValidatorDays") ?? GlobalFunctions.fallbackPasswordHistoryDays;
            List<string> commonWords = Configuration.GetSection("PasswordBannedWords").GetChildren().Select(c => c.Value).ToList<string>();
            int passwordHashingIterations = Configuration.GetValue<int?>("PasswordHashingIterations") ?? GlobalFunctions.fallbackPasswordHashingIterations; 
            int accountActivationTokenTimespanDays = Configuration.GetValue<int?>("AccountActivationTokenTimespanDays") ?? GlobalFunctions.fallbackAccountActivationTokenTimespanDays;
            int passwordResetTokenTimespanHours = Configuration.GetValue<int?>("PasswordResetTokenTimespanHours") ?? GlobalFunctions.fallbackPasswordResetTokenTimespanHours;

            // Do not add AuditLogDbContext.  This context should be protected from direct access.  Use the api class instead.  -TP

            services.AddIdentityCore<ApplicationUser>(config =>
                {
                    config.SignIn.RequireConfirmedEmail = true;
                })
                .AddRoles<ApplicationRole>()
                .AddSignInManager()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddTop100000PasswordValidator<ApplicationUser>()
                .AddRecentPasswordInDaysValidator<ApplicationUser>(passwordHistoryDays)
                .AddPasswordValidator<PasswordIsNotEmailValidator<ApplicationUser>>()
                .AddCommonWordsValidator<ApplicationUser>(commonWords)
                .AddTokenProvider<PasswordResetSecurityTokenProvider<ApplicationUser>>(GlobalFunctions.PasswordResetTokenProviderName)
                .AddTokenProvider<TwoFactorTokenProvider<ApplicationUser>>(GlobalFunctions.TwoFactorEmailTokenProviderName)
                ;

            #region Configure authentication services
            List<MapDbContextLib.Context.AuthenticationScheme> allSchemes = new List<MapDbContextLib.Context.AuthenticationScheme>();

            // get all configured schemes from database (no injected db service is available here)
            DbContextOptions<ApplicationDbContext> ctxOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(appConnectionString).Options;
            ApplicationDbContext applicationDb = new ApplicationDbContext(ctxOptions);
            allSchemes = applicationDb.AuthenticationScheme.ToList();

            if (allSchemes.Select(s => s.Name).Distinct().Count() != allSchemes.Count())
            {
                Log.Error("Multiple configured authentication schemes have the same name");
            }

            AuthenticationBuilder authenticationBuilder = services.AddAuthentication(IdentityConstants.ApplicationScheme);

            foreach (MapDbContextLib.Context.AuthenticationScheme scheme in allSchemes.Where(s => s.Type == AuthenticationType.WsFederation))
            {
                WsFederationSchemeProperties schemeProperties = (WsFederationSchemeProperties)scheme.SchemePropertiesObj;
                authenticationBuilder = authenticationBuilder.AddWsFederation(scheme.Name, scheme.DisplayName, options =>
                {
                    options.MetadataAddress = schemeProperties.MetadataAddress;
                    options.Wtrealm = schemeProperties.Wtrealm;
                    options.CallbackPath = $"{options.CallbackPath}-{scheme.Name}";

                    #region WS-Federation middleware event overrides
                    // Event override to add username query parameter to adfs request
                    options.Events.OnRedirectToIdentityProvider = context =>
                    {
                        // maximum age in minutes of the authentication token; 0 requires authentication on every request
                        context.ProtocolMessage.Wfresh = "0";

                        // requested authentication method
                        if (context.Properties.Items.ContainsKey("wauth"))
                        {
                            context.ProtocolMessage.Wauth = context.Properties.Items["wauth"];
                        }

                        // to pre-populate the user name in the federated login form (forms based authentication only)
                        if (context.Properties.Items.ContainsKey("username"))
                        {
                            context.ProtocolMessage.SetParameter("username", context.Properties.Items["username"]);
                        }
                        return Task.CompletedTask;
                    };

                    // Event override to handle all remote failures from WsFederation middleware
                    options.Events.OnRemoteFailure = context =>
                    {
                        context.Response.Redirect("/");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    };

                    // Event override to handle authentication failures from WsFederation middleware
                    options.Events.OnAuthenticationFailed = context =>
                    {
                        context.Response.Redirect("/");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    };

                    // Event override to avoid default application signin of the externally authenticated ClaimsPrinciple
                    options.Events.OnTicketReceived = async context =>
                    {
                        context.HandleResponse();  // Signals to caller (RemoteAuthenticationHandler.HandleRequestAsync) to forego subsequent processing

                        ClaimsIdentity identity = context?.Principal?.Identity as ClaimsIdentity;

                        string authenticatedUserName = identity?.Name ?? identity?.Claims?.SingleOrDefault(c => c.Type.EndsWith("nameidentifier"))?.Value;
                        if (string.IsNullOrWhiteSpace(authenticatedUserName))
                        {
                            Log.Error($"External authentication token received, but no authenticated user name was included in claim <{ClaimTypes.Name}> or <{ClaimTypes.NameIdentifier}>");
                            UriBuilder msg = new UriBuilder
                            {
                                Path = $"/{nameof(SharedController).Replace("Controller", "")}/{nameof(SharedController.UserMessage)}",
                                Query = $"messageCode={UserMessageEnum.SsoUserNameNotReturned}",
                            };
                            context.Response.Redirect(msg.Uri.PathAndQuery);
                            return;
                        }

                        using (IServiceScope scope = context.HttpContext.RequestServices.CreateScope())
                        {
                            IServiceProvider serviceProvider = scope.ServiceProvider;
                            SignInManager<ApplicationUser> _signInManager = serviceProvider.GetService<SignInManager<ApplicationUser>>();
                            IAuditLogger _auditLogger = serviceProvider.GetService<IAuditLogger>();
                            IConfiguration appConfig = serviceProvider.GetService<IConfiguration>();
                            try
                            {
                                ApplicationUser applicationUser = await _signInManager.UserManager.FindByNameAsync(authenticatedUserName);

                                if (applicationUser == null)
                                {
                                    Log.Warning($"External login succeeded but username {identity.Name} is not in the local MAP database");
                                    _auditLogger.Log(AuditEventType.LoginFailure.ToEvent(identity.Name, context.Scheme.Name, LoginFailureReason.UserAccountNotFound), null, null);

                                    UriBuilder msg = new UriBuilder
                                    {
                                        Path = $"/{nameof(SharedController).Replace("Controller", "")}/{nameof(SharedController.UserMessage)}",
                                        Query = $"messageCode={UserMessageEnum.SsoNoMapAccount}.",
                                    };
                                    context.Response.Redirect(msg.Uri.PathAndQuery);
                                    return;
                                }
                                else if (applicationUser.LastLoginUtc.HasValue &&
                                         applicationUser.LastLoginUtc.Value < DateTime.UtcNow.Date.AddMonths(-appConfig.GetValue("DisableInactiveUserMonths", 12)))
                                {
                                    // Disable login for users with last login date too long ago. Similar logic in AccountController.cs for local authentication
                                    Log.Warning($"External login for username {identity.Name} is disabled due to inactivity.  Last login was {applicationUser.LastLoginUtc.Value}");

                                    AccountController accountController = serviceProvider.GetService<AccountController>();
                                    accountController.NotifyUserAboutDisabledAccount(applicationUser);

                                    UriBuilder msg = new UriBuilder
                                    {
                                        Path = $"/{nameof(SharedController).Replace("Controller", "")}/{nameof(SharedController.UserMessage)}",
                                        Query = $"messageCode={UserMessageEnum.AccountDisabled}",
                                    };
                                    IAuditLogger _auditLog = serviceProvider.GetService<IAuditLogger>();
                                    _auditLog.Log(AuditEventType.LoginFailure.ToEvent(context.Principal.Identity.Name, context.Scheme.Name, LoginFailureReason.UserAccountDisabled), applicationUser.UserName, applicationUser.Id);
                                    context.Response.Redirect(msg.Uri.PathAndQuery);
                                    return;
                                }
                                else if (applicationUser.IsSuspended)
                                {
                                    _auditLogger.Log(AuditEventType.LoginIsSuspended.ToEvent(applicationUser.UserName), applicationUser.UserName, applicationUser.Id);

                                    UriBuilder msg = new UriBuilder
                                    {
                                        Path = $"/{nameof(SharedController).Replace("Controller", "")}/{nameof(SharedController.UserMessage)}",
                                        Query = $"messageCode={UserMessageEnum.AccountSuspended}",
                                    };
                                    context.Response.Redirect(msg.Uri.PathAndQuery);
                                    return;
                                }
                                else if (!applicationUser.EmailConfirmed)
                                {
                                    AccountController accountController = serviceProvider.GetService<AccountController>();
                                    await accountController.SendNewAccountWelcomeEmail(applicationUser, context.Request.Scheme, context.Request.Host, appConfig["Global:DefaultNewUserWelcomeText"]);

                                    UriBuilder msg = new UriBuilder
                                    {
                                        Path = $"/{nameof(SharedController).Replace("Controller","")}/{nameof(SharedController.UserMessage)}",
                                        Query = $"messageCode={UserMessageEnum.AccountNotActivated}",
                                    };
                                    context.Response.Redirect(msg.Uri.PathAndQuery);
                                    return;
                                }
                                else
                                {
                                    if (applicationUser.TwoFactorEnabled)
                                    {
                                        // This technique duplicates code in inaccessible method SignInManager<TUser>.SignInOrTwoFactorAsync().  We shouldn't 
                                        // need to do that. There may be a better way to integrate WsFederation using standard external login logic of Identity.
                                        var userId = await _signInManager.UserManager.GetUserIdAsync(applicationUser);
                                        ClaimsIdentity signInIdentity = new ClaimsIdentity( 
                                            new [] { new Claim(ClaimTypes.AuthenticationMethod, TokenOptions.DefaultEmailProvider), 
                                                     new Claim(ClaimTypes.Name, userId)}, 
                                            IdentityConstants.TwoFactorUserIdScheme);
                                        ClaimsPrincipal signInClaimsPrincipal = new ClaimsPrincipal(signInIdentity);

                                        await _signInManager.Context.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, signInClaimsPrincipal);

                                        string contextReturnUrl = context.ReturnUri
                                                                         .Split('?', '&')
                                                                         .SingleOrDefault(q => q.Contains("returnUrl=", StringComparison.InvariantCultureIgnoreCase))
                                                                      ?? "";

                                        contextReturnUrl = contextReturnUrl ?? "returnUrl=/";

                                        UriBuilder twoFactorUriBuilder = new UriBuilder
                                        {
                                            Host = context.Request.Host.Host,
                                            Scheme = context.Request.Scheme,
                                            Port = context.Request.Host.Port ?? -1,
                                            Path = $"/Account/{nameof(AccountController.LoginStepTwo)}",
                                            Query = $"Username={applicationUser.UserName}&RememberMe=false&{contextReturnUrl}",
                                        };

                                        context.Response.Redirect(twoFactorUriBuilder.Uri.AbsoluteUri);
                                        return;
                                    }
                                    else
                                    {
                                        await _signInManager.SignInAsync(applicationUser, false);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warning(ex, ex.Message);
                                IAuditLogger _auditLog = serviceProvider.GetService<IAuditLogger>();
                                _auditLog.Log(AuditEventType.LoginFailure.ToEvent(context.Principal.Identity.Name, context.Scheme.Name, LoginFailureReason.LoginFailed), null, null);

                                // Make sure nobody remains signed in
                                await _signInManager.SignOutAsync();
                            }
                        }

                        context.Response.Redirect(context.ReturnUri);
                    };
                    #endregion
                });
            }
            authenticationBuilder.AddIdentityCookies(builder =>
            {
                builder.TwoFactorUserIdCookie.Configure(options =>
                {
                    options.Cookie.MaxAge = TimeSpan.FromMinutes(Configuration.GetValue<int>("TwoFactorEmailTokenLifetimeMinutes"));  // MaxAge has precedence over ExpireTimeSpan, see https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Set-Cookie
                });
                builder.ApplicationCookie.Configure(options =>
                {
                    options.LoginPath = "/Account/LogIn";
                    options.LogoutPath = "/Account/LogOut";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.SlidingExpiration = true;
                    options.ReturnUrlParameter = "returnUrl";
                });
            });

            #endregion

            services.Configure<PasswordHasherOptions>(options => options.IterationCount = passwordHashingIterations);

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

                // Replace default token provider(s) with customized alternative(s)
                options.Tokens.PasswordResetTokenProvider = GlobalFunctions.PasswordResetTokenProviderName;
            });

            // Configure custom token providers
            services.Configure<PasswordResetSecurityTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromHours(passwordResetTokenTimespanHours);
            });
            
            services.Configure<TwoFactorTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromMinutes(Configuration.GetValue<int>("TwoFactorEmailTokenLifetimeMinutes"));
            });

            // Configure the default token provider used for account activation
            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromDays(accountActivationTokenTimespanDays);
            });

            services.Configure<QlikviewConfig>(Configuration);
            services.Configure<PowerBiConfig>(Configuration);
            services.Configure<AuditLoggerConfiguration>(Configuration);
            services.Configure<SmtpConfig>(Configuration);

            //services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddSession(options => 
            {
                options.Cookie.IsEssential = true;  // TODO This bypasses cookie consent.  Think about GDPR
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });

            services.AddResponseCaching();
            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365);
                options.IncludeSubDomains = true;
            });

            services
            .AddControllersWithViews(options => 
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
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            })
            .AddControllersAsServices();

            services.AddApplicationInsightsTelemetry(Configuration);

            string fileUploadPath = Path.GetTempPath();
            // The environment variable check enables migrations to be deployed to Staging or Production via the MAP deployment server
            // This variable should never be set on a real production or staging system
            if (!string.IsNullOrWhiteSpace(Configuration.GetValue<string>("Storage:FileUploadPath")) && Environment.GetEnvironmentVariable("MIGRATIONS_RUNNING") == null)
            {
                fileUploadPath = Configuration.GetValue<string>("Storage:FileUploadPath");
            }
            services.AddSingleton<IFileProvider>(new PhysicalFileProvider(fileUploadPath));

            // These depend on UserManager from Identity, which is scoped, so don't add the following as singleton
            services.AddScoped<IAuthorizationHandler, MapAuthorizationHandler>();
            services.AddScoped<IAuditLogger, AuditLogger>();

            // Queries
            services.AddScoped<StandardQueries>();
            services.AddScoped<ClientAdminQueries>();
            services.AddScoped<ContentAccessAdminQueries>();
            services.AddScoped<ContentPublishingAdminQueries>();

            services.AddScoped<FileDropQueries>();
            services.AddScoped<ClientQueries>();
            services.AddScoped<ClientAccessReviewQueries>();
            services.AddScoped<ContentItemQueries>();
            services.AddScoped<HierarchyQueries>();
            services.AddScoped<SelectionGroupQueries>();
            services.AddScoped<PublicationQueries>();
            services.AddScoped<UserQueries>();
            services.AddScoped<AuthorizedContentQueries>();

            //services.AddSingleton<IOptionsMonitorCache<WsFederationOptions>, OptionsCache<WsFederationOptions>>();
            services.AddSingleton<IPostConfigureOptions<WsFederationOptions>, WsFederationPostConfigureOptions>();

            // Add application services.
            services.AddTransient<IMessageQueue, MessageQueueServices>();
            services.AddScoped<IUploadHelper, UploadHelper>();
            services.AddHostedService<QueuedUploadTaskHostedService>();
            services.AddSingleton<IUploadTaskQueue, UploadTaskQueue>();
            services.AddHostedService<QueuedGoLiveTaskHostedService>();
            services.AddSingleton<IGoLiveTaskQueue, GoLiveTaskQueue>();
            services.AddHostedService<QueuedPublicationPostProcessingHostedService>();
            services.AddHostedService<QueuedReductionPostProcessingHostedService>();
            services.AddHostedService <SystemMaintenanceHostedService>();
            services.AddSingleton<IPublicationPostProcessingTaskQueue, PublicationPostProcessingTaskQueue>();
            services.AddHostedService<FileDropUploadProcessingHostedService>();
            services.AddSingleton<IFileDropUploadTaskTracker, FileDropUploadTaskTracker>();
            services.AddScoped<FileSystemTasks>();

            string EnvironmentNameUpper = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToUpper();

            // Configure Data Protection for production and staging
            switch (EnvironmentNameUpper)
            {
                case "PRODUCTION":
                case "STAGING":
                case "INTERNAL":

                    Log.Debug("Configuring Data Protection");

                    var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, Configuration["AzureCertificateThumbprint"], false);
                    var cert = certCollection.OfType<X509Certificate2>().Single();

                    DirectoryInfo keyDirectory = new DirectoryInfo(@"C:\temp-keys");

                    services.AddDataProtection()
                        .PersistKeysToFileSystem(keyDirectory)
                        .ProtectKeysWithAzureKeyVault(Configuration["DataProtectionKeyId"],
                                                        Configuration["AzureClientID"],
                                                        cert);

                    Log.Debug("Finished configuring data protection");

                    break;
                
                case "AZURE-ISDEV":
                case "AZURE-DEV":
                case "AZURE-UAT":
                case "AZURE-PROD":
                    Log.Debug("Configuring Data Protection");

                    DirectoryInfo azKeyDirectory = new DirectoryInfo(@"C:\temp-keys");

                    var provider = new AzureServiceTokenProvider();
                    var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(provider.KeyVaultTokenCallback));


                    services.AddDataProtection()
                        .PersistKeysToFileSystem(azKeyDirectory)
                        .ProtectKeysWithAzureKeyVault(kv, Configuration["DataProtectionKeyId"]);

                    Log.Debug("Finished configuring data protection");

                    break;
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var rewriteOptions = new RewriteOptions().AddRedirectToHttps();
            app.UseRewriter(rewriteOptions);

            if (env.IsDevelopment() || env.EnvironmentName.ToUpper() == "AZURECI")
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();

                if (env.IsDevelopment())
                {
#warning HEY!!!  Figure out a replacement for this obsolete code
                    // TODO need to address what to do here since this is obsolete
                    // https://github.com/dotnet/AspNetCore.Docs/issues/13245
                    app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                    {
                        HotModuleReplacement = true,
                        ConfigFile = "webpack.dev.js",
                    });
                }
            }
            else
            {
                app.UseHttpsRedirection();
                app.UseHsts();
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = (context) =>
                {
                    // Only cache static files with content hashes in the filename
                    var cacheableFileTypes = new List<string> { ".js", ".css" };
                    if (cacheableFileTypes.Contains(Path.GetExtension(context.File.PhysicalPath)))
                    {
                        context.Context.Response.Headers[HeaderNames.CacheControl] = "max-age=31536000";
                    }
                },
            });

            // Configure response caching to not cache requests
            // CSRF protection disables caching for unsafe HTTP methods only, so this additional configuration
            // is required to prevent some browsers (IE, ) from caching Ajax requests
            app.UseResponseCaching();
            app.Use(async (context, next) =>
            {
                context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                };

                await next();
            });

            var policyCollection = new HeaderPolicyCollection()
                .AddCustomHeader("X-UA-Compatible", "IE=Edge") // Prevents IE from going into compatibility mode, should work for internal and external users
                .AddCustomHeader("X-Frame-Options", "SAMEORIGIN"); // Prevents clickjacking
            app.UseSecurityHeaders(policyCollection);

            // Conditionally omit authentication cookie, intended for status calls that should not extend the user session
            app.Use(async (context,next) =>
            {
                context.Response.OnStarting(state =>
                {
                    if (context.Items.ContainsKey("PreventAuthRefresh"))  // if the action was invoked with [PreventAuthRefreshAttribute]
                    {
                        var response = (HttpResponse) state;

                        // Omit Set-Cookie header with the offending cookie name
                        var cookieHeader = response.Headers[HeaderNames.SetCookie]
                            .Where(s => !s.Contains(".AspNetCore.Identity.Application"))
                            .Aggregate(new StringValues(), (current, s) => StringValues.Concat(current, s));
                        response.Headers[HeaderNames.SetCookie] = cookieHeader;
                    }
                    return Task.CompletedTask;
                }, context.Response);
                await next();
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            // Redirect to the user agreement view if an authenticated user has not accepted. 
            app.Use(async (context, next) =>
            {
                string redirectPath = $"/{nameof(AccountController).Replace("Controller", "")}/{nameof(AccountController.UserAgreement)}";

                // Only do this expensive thing for appropriate requests
                if (context.Request.Path != redirectPath &&
                    context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                    context.User.Identity.IsAuthenticated)
                {
                    ApplicationDbContext db = context.RequestServices.GetService<ApplicationDbContext>();
                    var user = await db.ApplicationUser.SingleAsync(u => u.UserName == context.User.Identity.Name);
                    TimeSpan renewInterval = TimeSpan.FromDays(Configuration.GetValue<int>("UserAgreementRenewalIntervalDays"));

                    if (!user.UserAgreementAcceptedUtc.HasValue ||
                        DateTime.UtcNow - user.UserAgreementAcceptedUtc > renewInterval) // need to accept now
                    {
                        UriBuilder userAgreementUri = new UriBuilder
                        {
                            Scheme = context.Request.Scheme,
                            Host = context.Request.Host.Host,
                            Port = context.Request.Host.Port.GetValueOrDefault(-1),
                            Path = redirectPath,
                            Query = $"isRenewal={user.UserAgreementAcceptedUtc.HasValue}&returnUrl={UriHelper.GetEncodedUrl(context.Request)}",
                        };

                        context.Response.Redirect(userAgreementUri.Uri.AbsoluteUri);
                        return;
                    }
                }

                await next();
            });

            // for debugging
            app.Use(async (context, next) =>
            {
                await next();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute("default", "{controller=AuthorizedContent}/{action=Index}/{id?}");
                //endpoints.MapRazorPages();
            });
        }
    }
}
