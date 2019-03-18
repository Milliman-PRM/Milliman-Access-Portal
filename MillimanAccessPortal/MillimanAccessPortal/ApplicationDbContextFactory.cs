using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
