using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuditLogLib;
using AuditLogLib.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MapQueryAdminWeb
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.Configure<AuditLoggerConfiguration>(Configuration);

            services.AddScoped<IAuditLogger, AuditLogger>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Query}/{action=RunQuery}/{id?}");
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
