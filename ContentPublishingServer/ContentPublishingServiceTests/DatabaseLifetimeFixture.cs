/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using ContentPublishingLib;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using TestResourcesLib;
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

            Log.Information($"Running as user {Environment.UserDomainName} \\ {Environment.UserName}");
            Log.Information($"or {Environment.UserDomainName} \\ {Environment.GetEnvironmentVariable("Username", EnvironmentVariableTarget.Machine)}");
            Log.Information($"or {Environment.UserDomainName} \\ {System.Security.Principal.WindowsIdentity.GetCurrent().Name}");

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
            }

            switch (environmentName)
            {
                case null:
                case string name when name.Equals("Development", StringComparison.InvariantCultureIgnoreCase):
                    configurationBuilder.AddUserSecrets<ProcessManager>();
                    break;

                case string name when name.Equals("CI", StringComparison.InvariantCultureIgnoreCase):
                    IConfigurationRoot keyVaultAccessConfig = new ConfigurationBuilder().AddJsonFile($"AzureKeyVault.{environmentName}.json", true).Build();

                    var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var cert = store.Certificates.Find(X509FindType.FindByThumbprint, keyVaultAccessConfig["AzureCertificateThumbprint"], false);

                    if (cert.OfType<X509Certificate2>().Count() == 1)
                    {
                        configurationBuilder.AddAzureKeyVault(keyVaultAccessConfig["AzureVaultName"], keyVaultAccessConfig["AzureClientID"], cert.OfType<X509Certificate2>().Single());
                    }
                    else
                    {
                        throw new ApplicationException($"Found {cert.OfType<X509Certificate2>().Count()} certificate(s) to access Azure Key Vault for environment {environmentName}, expected 1");
                    }
                    break;
            }

            IConfiguration returnVal = configurationBuilder.Build();

            // TODO Decide whether and how to dump the entire configuration to Serilog.  One idea is:
            //if (Environment.GetEnvironmentVariable("MapCiVerboseConfigDump") != null)
            //{
            //    ConfigurationDumper.DumpConfigurationDetails(environmentName, configurationBuilder, returnVal);
            //}

            return returnVal;
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
