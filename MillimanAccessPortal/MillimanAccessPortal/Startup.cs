/*
 * CODE OWNERS: Ben Wyatt, Michael Reisz
 * OBJECTIVE: Configure application runtime environment at startup
 * DEVELOPER NOTES: 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.DataQueries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using QlikviewLib;
using AuditLogLib;
using AuditLogLib.Services;
using EmailQueue;
using MillimanAccessPortal.Authorization;
using Microsoft.AspNetCore.Identity;

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

            // Add framework services.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("MillimanAccessPortal")));

            // Do not add AuditLogDbContext.  This context should be protected from direct access.  Use the api class instead.  -TP

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = false;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 5;
                
                // User settings
                options.User.RequireUniqueEmail = true;
            });

            // Cookie settings
            services.ConfigureApplicationCookie(options =>
                {
                    options.LoginPath = "/Account/LogIn";
                    options.LogoutPath = "/Account/LogOut";
                    options.ExpireTimeSpan = TimeSpan.FromDays(150);
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
            .AddJsonOptions(opt =>
            {
                var resolver = opt.SerializerSettings.ContractResolver;
                if (resolver != null)
                {
                    var res = resolver as Newtonsoft.Json.Serialization.DefaultContractResolver;
                    res.NamingStrategy = null;  // Remove the default lowerCamelCasing of the json output
                }
            });

            // Depends on UserManager from Identity, which is scoped, so don't add the following as singleton
            services.AddScoped<IAuthorizationHandler, MapAuthorizationHandler>();
            services.AddScoped<IAuditLogger, AuditLogger>();

            // Add application services.
            services.AddTransient<MessageQueueServices>();
            services.AddScoped<StandardQueries>();
            //services.AddTransient<IEmailSender, AuthMessageSender>();
            //services.AddTransient<ISmsSender, AuthMessageSender>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            var options = new RewriteOptions()
               .AddRedirectToHttps();

            app.UseRewriter(options);

            if (env.IsDevelopment() || env.EnvironmentName == "CI")
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            // Add external authentication middleware below. To configure them please see https://go.microsoft.com/fwlink/?LinkID=532715

            app.UseSession();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=HostedContent}/{action=Index}/{id?}");
            });

            MailSender.ConfigureMailSender(new SmtpConfig
            {
                SmtpServer = Configuration.GetValue<string>("SmtpServer"),
                SmtpPort = Configuration.GetValue<int>("SmtpPort"),
                SmtpFromAddress = Configuration.GetValue<string>("SmtpFromAddress"),
                SmtpFromName = Configuration.GetValue<string>("SmtpFromName")
            });

            AuditLogger.Config = new AuditLoggerConfiguration
            {
                AuditLogConnectionString = Configuration.GetConnectionString("AuditLogConnectionString"),
            };
            
        }
    }
}
