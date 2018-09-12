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
                .AddTop100000PasswordValidator<ApplicationUser>()
                .AddRecentPasswordInDaysValidator<ApplicationUser>(passwordHistoryDays)
                .AddPasswordValidator<PasswordIsNotEmailOrUsernameValidator<ApplicationUser>>()
                .AddCommonWordsValidator<ApplicationUser>(commonWords)
                .AddTokenProvider<PasswordResetSecurityTokenProvider<ApplicationUser>>(tokenProviderName);
            
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

            // Cookie settings
            services.ConfigureApplicationCookie(options =>
                {
                    options.LoginPath = "/Account/LogIn";
                    options.LogoutPath = "/Account/LogOut";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // TODO: read from configuration
                    options.SlidingExpiration = true;
                }
            );

            services.Configure<QlikviewConfig>(Configuration);
            services.Configure<AuditLoggerConfiguration>(Configuration);
            services.Configure<SmtpConfig>(Configuration);

            services.AddMemoryCache();
            services.AddSession();

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
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, ApplicationDbContext db)
        {
            
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            var options = new RewriteOptions()
               .AddRedirectToHttps();

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
                    });
                }
            }
            else
            {
                app.UseHttpsRedirection();
                app.UseHsts();
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            // Conditionally omit auth cookie
            app.Use(next => context =>
            {
                context.Response.OnStarting(state =>
                {
                    if (context.Items.ContainsKey("PreventAuthRefresh"))
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

            // Add external authentication middleware below. To configure them please see https://go.microsoft.com/fwlink/?LinkID=532715

            app.UseSession();

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
            };
            #endregion

        }
    }
}
