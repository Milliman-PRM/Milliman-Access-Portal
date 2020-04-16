/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace ContentPublishingServiceTests
{
    [CollectionDefinition("DatabaseLifetime collection")]
    public class DatabaseLifeTimeCollection : ICollectionFixture<DatabaseLifetimeFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    public class DatabaseLifetimeFixture : IDisposable
    {
        public string ConnectionString { get; private set; }
        public IConfiguration Configuration { get; set; }

        public DatabaseLifetimeFixture()
        {
            // Initialize Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Debug()
                .MinimumLevel.Information()
                .CreateLogger();

            Configuration = GenerateConfiguration();

            #region Get configuration and set instance properties
            Dictionary<string, string> DbConfig = new Dictionary<string, string>
            {
                { "DbHost", Configuration.GetValue<string>("UnitTestPgServerHost") },
                { "DbPort", Configuration.GetValue<string>("UnitTestPgServerPort", "5432") },
                { "DbUser", Configuration.GetValue<string>("UnitTestPgServerUser") },
                { "DbPass", Configuration.GetValue<string>("UnitTestPgServerPass") },
            };

            if (DbConfig.Any(v => string.IsNullOrWhiteSpace(v.Value)))
            {
                throw new ApplicationException("Database configuration is incomplete or not found");
            }

            Npgsql.NpgsqlConnectionStringBuilder cxnStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Database = Guid.NewGuid().ToString(),
                Port = int.Parse(DbConfig["DbPort"]),
                Host = DbConfig["DbHost"],
                Username = DbConfig["DbUser"],
                Password = DbConfig["DbPass"],
                SslMode = Npgsql.SslMode.Prefer,
            };

            ConnectionString = cxnStringBuilder.ConnectionString;
            #endregion

            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            builder.UseNpgsql(ConnectionString, o => o.SetPostgresVersion(9, 6));
            using (ApplicationDbContext db = new ApplicationDbContext(builder.Options))
            {
                db.Database.EnsureCreated();
            }
        }

        private IConfiguration GenerateConfiguration()
        {
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("contentPublicationLibSettings.json", true)
                .AddJsonFile("appsettings.json", true);

            if (!string.IsNullOrEmpty(environmentName))
            {
                configurationBuilder.AddJsonFile($"appsettings.{environmentName}.json", true);

                if (!string.IsNullOrEmpty(environmentName) && environmentName.Equals("Development", StringComparison.InvariantCultureIgnoreCase))
                {
                    configurationBuilder.AddUserSecrets<TestInitialization>();
                }
            }

            return configurationBuilder.Build();
        }

        public void Dispose()
        {
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            builder.UseNpgsql(ConnectionString);
            using (ApplicationDbContext db = new ApplicationDbContext(builder.Options))
            {
                db.Database.EnsureDeleted();
            }
        }
    }
}
