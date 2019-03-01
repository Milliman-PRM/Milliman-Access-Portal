/*
 * CODE OWNERS: Ben Wyatt, Michael Reisz
 * OBJECTIVE: Configure application runtime environment at startup
 * DEVELOPER NOTES: 
 */

using AuditLogLib;
using AuditLogLib.Services;
using EmailQueue;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using MillimanAccessPortal.Authorization;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Services;
using QlikviewLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NetEscapades.AspNetCore.SecurityHeaders;
using MillimanAccessPortal.Utilities;
using System.Diagnostics;
using Serilog;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AzureKeyVault;
using System.Security.Cryptography.X509Certificates;

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

            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add(new RequireHttpsAttribute());
            });

            #region Configure application connection string

            string appConnectionString = Configuration.GetConnectionString("DefaultConnection");
            
            // If the database name is defined in the environment, update the connection string
            if (Environment.GetEnvironmentVariable("APP_DATABASE_NAME") != null)
            {
                Npgsql.NpgsqlConnectionStringBuilder stringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(appConnectionString);
                stringBuilder.Database = Environment.GetEnvironmentVariable("APP_DATABASE_NAME");
                appConnectionString = stringBuilder.ConnectionString;
            }

            // Add framework services.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(appConnectionString, b => b.MigrationsAssembly("MillimanAccessPortal")));
            #endregion

            int passwordHistoryDays = Configuration.GetValue<int?>("PasswordHistoryValidatorDays") ?? GlobalFunctions.fallbackPasswordHistoryDays;
            List<string> commonWords = Configuration.GetSection("PasswordBannedWords").GetChildren().Select(c => c.Value).ToList<string>();
            int passwordHashingIterations = Configuration.GetValue<int?>("PasswordHashingIterations") ?? GlobalFunctions.fallbackPasswordHashingIterations; 
            int accountActivationTokenTimespanDays = Configuration.GetValue<int?>("AccountActivationTokenTimespanDays") ?? GlobalFunctions.fallbackAccountActivationTokenTimespanDays;
            int passwordResetTokenTimespanHours = Configuration.GetValue<int?>("PasswordResetTokenTimespanHours") ?? GlobalFunctions.fallbackPasswordResetTokenTimespanHours;

            string tokenProviderName = "MAPResetToken";

            // Do not add AuditLogDbContext.  This context should be protected from direct access.  Use the api class instead.  -TP

            services.AddIdentity<ApplicationUser, ApplicationRole>(config =>
                {
                    config.SignIn.RequireConfirmedEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                //.AddTop100000PasswordValidator<ApplicationUser>()
                //.AddRecentPasswordInDaysValidator<ApplicationUser>(passwordHistoryDays)
                //.AddPasswordValidator<PasswordIsNotEmailOrUsernameValidator<ApplicationUser>>()
                //.AddCommonWordsValidator<ApplicationUser>(commonWords)
                //.AddTokenProvider<PasswordResetSecurityTokenProvider<ApplicationUser>>(tokenProviderName)
                ;

            #region Configure authentication services
            var WsFederationConfigSections = Configuration.GetSection("WsFederationSources").GetChildren();
            if (WsFederationConfigSections.Select(s => s.GetValue<string>("Scheme")).Distinct().Count() != WsFederationConfigSections.Count())
            {
                // Error, multiple configured schemes with the same name
            }

            AuthenticationBuilder authenticationBuilder = services.AddAuthentication(sharedOptions => 
            {
                sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            foreach (ConfigurationSection section in WsFederationConfigSections)
            {
                WsFederationConfig wsFederationConfig;
                try
                {
                    wsFederationConfig = (WsFederationConfig)section;
                }
                catch (ApplicationException ex)
                {
                    string Msg = ex.Message;
                    // Error, log a configuration failure
                    continue;
                }

                authenticationBuilder = authenticationBuilder.AddWsFederation(wsFederationConfig.Scheme, $"{wsFederationConfig.DisplayName}", options =>
                {
                    options.MetadataAddress = wsFederationConfig.MetadataAddress;
                    options.Wtrealm = wsFederationConfig.Wtrealm;
                    options.CallbackPath = $"{options.CallbackPath}-{wsFederationConfig.Scheme}";

                    options.Events.OnAuthenticationFailed = context => { var xx = context; return Task.CompletedTask; };
                    options.Events.OnMessageReceived = context => { var xx = context; return Task.CompletedTask; };
                    options.Events.OnRedirectToIdentityProvider = context => { var xx = context; return Task.CompletedTask; };
                    options.Events.OnRemoteFailure = context => { var xx = context; return Task.CompletedTask; };
                    options.Events.OnRemoteSignOut = context => { var xx = context; return Task.CompletedTask; };
                    options.Events.OnSecurityTokenReceived = context => { var xx = context; return Task.CompletedTask; };
                    options.Events.OnSecurityTokenValidated = context => { var xx = context; return Task.CompletedTask; };
                    options.Events.OnTicketReceived = async context => { var xx = context; await context.HttpContext.SignInAsync(context.Principal); };
                });
            }
            authenticationBuilder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                //options.LoginPath = "/Account/LogIn";
                //options.LogoutPath = "/Account/LogOut";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
            });

            #endregion

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

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

                // Enable custom token provider for password resets
                options.Tokens.PasswordResetTokenProvider = tokenProviderName;
            });

            // Configure custom security token provider
            services.Configure<PasswordResetSecurityTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromHours(passwordResetTokenTimespanHours);
            });

            // Configure the default token provider used for account activation
            services.Configure<DataProtectionTokenProviderOptions>(options =>
                {
                    options.TokenLifespan = TimeSpan.FromDays(accountActivationTokenTimespanDays);
                }
            );

            services.Configure<QlikviewConfig>(Configuration);
            services.Configure<AuditLoggerConfiguration>(Configuration);
            services.Configure<SmtpConfig>(Configuration);

            services.AddMemoryCache();
            services.AddSession();

            services.AddResponseCaching();

            services.AddMvc(config =>
            {
                var policy = new AuthorizationPolicyBuilder()
                             .RequireAuthenticatedUser()
                             .Build();
                config.Filters.Add(new AuthorizeFilter(policy));
            })
            .AddControllersAsServices()
            .AddJsonOptions(opt =>
            {
                var resolver = opt.SerializerSettings.ContractResolver;
                if (resolver != null)
                {
                    var res = resolver as Newtonsoft.Json.Serialization.DefaultContractResolver;
                    res.NamingStrategy = null;  // Remove the default lowerCamelCasing of the json output
                }
            });

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
            services.AddScoped<StandardQueries>();

            // Add application services.
            services.AddTransient<IMessageQueue, MessageQueueServices>();
            services.AddScoped<IUploadHelper, UploadHelper>();
            services.AddHostedService<QueuedUploadTaskHostedService>();
            services.AddSingleton<IUploadTaskQueue, UploadTaskQueue>();
            services.AddHostedService<QueuedGoLiveTaskHostedService>();
            services.AddSingleton<IGoLiveTaskQueue, GoLiveTaskQueue>();
            services.AddHostedService<QueuedPublicationPostProcessingHostedService>();
            services.AddSingleton<IPublicationPostProcessingTaskQueue, PublicationPostProcessingTaskQueue>();
            services.AddScoped<FileSystemTasks>();

            string EnvironmentNameUpper = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToUpper();

            // Configure Data Protection for production and staging
            switch (EnvironmentNameUpper)
            {
                case "PRODUCTION":
                case "STAGING":

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
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ApplicationDbContext db)
        {
            var options = new RewriteOptions()
               .AddRedirectToHttps();

            // time the entire middleware execution
            app.Use(async (context, next) =>
            {
                var stopwatch = new Stopwatch();

                stopwatch.Start();
                await next();
                stopwatch.Stop();

                Log.Information("Middleware pipeline took {elapsed}ms", stopwatch.Elapsed.TotalMilliseconds);
            });

            app.UseRewriter(options);

            if (env.IsDevelopment() || env.EnvironmentName.ToUpper() == "AZURECI")
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();

                if (env.IsDevelopment())
                {
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

            // Send header to prevent IE from going into compatibility mode
            // Should work for internal and external users
            var policyCollection = new HeaderPolicyCollection()
                .AddCustomHeader("X-UA-Compatible", "IE=Edge");
            app.UseSecurityHeaders(policyCollection);

            // Conditionally omit authentication cookie, intended for status calls that should not extend the user session
            app.Use(next => context =>
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
                return next(context);
            });

            app.UseAuthentication();
            //Todo: read this: https://github.com/aspnet/Security/issues/1310

            // Add external authentication middleware below. To configure them please see https://go.microsoft.com/fwlink/?LinkID=532715

            app.UseSession();

            // time action execution
            app.Use(async (context, next) =>
            {
                var stopwatch = new Stopwatch();

                stopwatch.Start();
                await next();
                stopwatch.Stop();

                Log.Information("MVC took {elapsed}ms", stopwatch.Elapsed.TotalMilliseconds);
            });


            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=AuthorizedContent}/{action=Index}/{id?}");
            });

            MailSender.ConfigureMailSender(new SmtpConfig
            {
                SmtpServer = Configuration.GetValue<string>("SmtpServer"),
                SmtpPort = Configuration.GetValue<int>("SmtpPort"),
                SmtpFromAddress = Configuration.GetValue<string>("SmtpFromAddress"),
                SmtpFromName = Configuration.GetValue<string>("SmtpFromName"),
                SmtpUsername = Configuration.GetValue<string>("SmtpUsername"),
                SmtpPassword = Configuration.GetValue<string>("SmtpPassword")
            });

            #region Configure Audit Logger connection string
            string auditLogConnectionString = Configuration.GetConnectionString("AuditLogConnectionString");

            // If the database name is defined in the environment, update the connection string
            if (Environment.GetEnvironmentVariable("AUDIT_LOG_DATABASE_NAME") != null)
            {
                Npgsql.NpgsqlConnectionStringBuilder stringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(auditLogConnectionString);
                stringBuilder.Database = Environment.GetEnvironmentVariable("AUDIT_LOG_DATABASE_NAME");
                auditLogConnectionString = stringBuilder.ConnectionString;
            }

            AuditLogger.Config = new AuditLoggerConfiguration
            {
                AuditLogConnectionString = auditLogConnectionString,
                ErrorLogRootFolder = Configuration.GetValue<string>("Storage:ApplicationLog"),
            };
            #endregion

        }
    }
}
