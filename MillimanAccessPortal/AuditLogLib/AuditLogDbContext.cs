/*
 * CODE OWNERS: Tom Puckett, Ben Wyatt
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: This project manages its own migrations and database updates.  
 *                  The dotnet command must be run from this project folder.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace AuditLogLib
{
    internal class AuditLogDbContext : DbContext
    {
        /// <summary>
        /// A convenience method allowing the user simplest access to an instance without the caller going through the contextbuilder for each instantiation
        /// </summary>
        /// <param name="ConnectionString">If not provided, a configured value (or default) is used</param>
        /// <returns></returns>
        internal static AuditLogDbContext Instance(string ConnectionString = "")
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                ConnectionString = GetConfiguredConnectionString();  // I could pass a connection string name to get a designated configured value
            }

            var builder = new DbContextOptionsBuilder<AuditLogDbContext>();
            builder.UseNpgsql(ConnectionString);
            return new AuditLogDbContext(builder.Options);
        }

        public DbSet<AuditEvent> AuditEvent { get; set; }

        public AuditLogDbContext()
            : base()
        {
            // This constructor works in combination with the OnConfiguring override to establish the connection
            // When the application uses this provider through when building a migration, or if the application instantiates with no argument. 
        }

        protected AuditLogDbContext(DbContextOptions<AuditLogDbContext> options)
            : base(options)
        {
            string EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToUpper();

            switch (EnvironmentName)
            {
                case "AZURECI":
                case "PRODUCTION":
                    Database.Migrate(); // Run migrations when the logger is instantiated
                    break;

                case "DEVELOPMENT":
                default:
                    // nothing
                    break;
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder Builder)
        {
            if (Builder.Options.Extensions.Any(e => e.GetType() == typeof(Microsoft.EntityFrameworkCore.Infrastructure.Internal.NpgsqlOptionsExtension)))
            {
                // This block supports the use of a connection string provided through dependency injection
                Microsoft.EntityFrameworkCore.Infrastructure.Internal.NpgsqlOptionsExtension Extension =
                    Builder.Options.Extensions.First(x => x.GetType() == typeof(Microsoft.EntityFrameworkCore.Infrastructure.Internal.NpgsqlOptionsExtension)) as Microsoft.EntityFrameworkCore.Infrastructure.Internal.NpgsqlOptionsExtension;
                Builder.UseNpgsql(Extension.ConnectionString);
            }
            else
            {
                // This block supports ef migration add, where no connection string is provided through dependency injection
                Builder.UseNpgsql(GetConfiguredConnectionString());
            }
        }

        /// <summary>
        /// Has responsibility for extracting the proper ConfigurationString for this data context. Only called when performing database migrations.
        /// </summary>
        /// <param name="ConnectionStringName"></param>
        /// <returns></returns>
        internal static string GetConfiguredConnectionString(string ConnectionStringName = "AuditLogConnectionString")
        {
            var configurationBuilder = new ConfigurationBuilder();
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToUpper();
            string auditLogConnectionString;

            var built = configurationBuilder.Build();

            switch (environmentName)
            {
                case "AZURECI":
                case "PRODUCTION":
                    configurationBuilder.AddJsonFile(path: $"AzureKeyVault.{environmentName}.json", optional: false);
                    built = configurationBuilder.Build();
                    var store = new X509Store(StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var cert = store.Certificates.Find(X509FindType.FindByThumbprint, built["AzureCertificateThumbprint"], false);

                    configurationBuilder.AddAzureKeyVault(
                        built["AzureVaultName"],
                        built["AzureClientID"],
                        cert.OfType<X509Certificate2>().Single());

                    built = configurationBuilder.Build();

                    string branchName = Environment.GetEnvironmentVariable("BranchName");
                    string logDbName = $"auditlogdb_{branchName}";
                    Npgsql.NpgsqlConnectionStringBuilder logConnBuilder = new Npgsql.NpgsqlConnectionStringBuilder(built.GetConnectionString(ConnectionStringName));
                    logConnBuilder.Database = logDbName;
                    auditLogConnectionString = logConnBuilder.ConnectionString;
                    break;

                case "DEVELOPMENT":
                    configurationBuilder.AddUserSecrets<AuditLogDbContext>();
                    built = configurationBuilder.Build();

                    auditLogConnectionString = built.GetConnectionString(ConnectionStringName);
                    break;

                default: // Unsupported environment name	
                    throw new InvalidOperationException($"Current environment name ({environmentName}) is not supported for AuditLogLib migrations");
            }

            return auditLogConnectionString;
        }

    }
}
