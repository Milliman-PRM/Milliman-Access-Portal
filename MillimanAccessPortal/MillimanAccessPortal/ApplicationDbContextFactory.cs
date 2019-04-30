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
            return new ApplicationDbContext(contextOptionsBuilder.Options);
        }
    }
}
