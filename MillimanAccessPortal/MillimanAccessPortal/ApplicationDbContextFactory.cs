/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Generates an instance of ApplicationDbContext without full configuration of Startup class instance
 * DEVELOPER NOTES: When deploying a database migration, it is possible that code in the Startup class uses a feature 
 * of a new migration. In that situation an exception occurs when trying to deploy the migration if the Startup class 
 * is configured. That is the default behavior in dotnet, where if a method exists named Program.BuildWebHost(), 
 * that method is run, leading to full Startup configuration. If that method is not found, dotnet searches the main 
 * assembly for an implementation of IDesignTimeDbContextFactory<ApplicationDbContext> (this file). If found, this 
 * code is used to instantiate a database context to use in migration deployment. In MAP the method BuildWebHost() 
 * has been renamed to RunTimeBuildWebHost() so that it does not execute during deployment of migrations, removing 
 * the risk of an exception during that process. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MillimanAccessPortal
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            string EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var cfgBuilder = new ConfigurationBuilder();
            Program.SetApplicationConfiguration(EnvironmentName, cfgBuilder);
            IConfiguration cfg = cfgBuilder.Build();

            var connectionString = cfg.GetConnectionString("DefaultConnection");

            var contextOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString, b => b.MigrationsAssembly("MillimanAccessPortal"));
            ApplicationDbContext newContext = new ApplicationDbContext(contextOptionsBuilder.Options);

            # region Database initialization for configuration dependent migrationss
            Dictionary<string, Type> mandatoryTypedConfigs = new Dictionary<string, Type>
            {
                { "ClientReviewRenewalPeriodDays", typeof(int) },
                { "ClientReviewGracePeriodDays", typeof(int) },
                { "ClientReviewEarlyWarningDays", typeof(int) },
            };

            foreach (string key in mandatoryTypedConfigs.Keys)
            {
                dynamic configValue = cfg.GetValue(mandatoryTypedConfigs[key], key);
                if (configValue == null)
                {
                    throw new ApplicationException($"Required parameter \"{key}\" is missing from application configuration");
                }
                string typedConfigValue = Convert.ChangeType(configValue, typeof(string));

                NameValueConfiguration record = newContext.NameValueConfiguration.SingleOrDefault(c => c.Key == key);
                if (record == null)
                {
                    record = new NameValueConfiguration { Key = key, Value = typedConfigValue };
                    newContext.Add(record);
                }
                else if (record.Value != typedConfigValue)
                {
                    record.Value = typedConfigValue;
                }
            }
            newContext.SaveChanges();
            #endregion

            return newContext;
        }
    }
}
