/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Provides access to resources that may be used throughout the application
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Prm.SerilogCustomization;
using Serilog;
using System;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace SftpServerLib
{
    public static class GlobalResources
    {
        private static DbContextOptions<ApplicationDbContext> MapDbContextOptions = null;

        public static ConfigurationRoot ApplicationConfiguration { get; private set; } = null;

        /// <summary>
        /// Sets the connection string to be used when constructing instances of ApplicationDbContext
        /// </summary>
        public static string MapDbConnectionString
        {
            set
            {
                DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                ContextBuilder.UseNpgsql(value);
                MapDbContextOptions = ContextBuilder.Options;
            }
        }

        /// <summary>
        /// Intended to be invoked from the application project
        /// </summary>
        public static void LoadConfiguration()
        {
            string EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToUpper();

            IConfigurationBuilder CfgBuilder = new ConfigurationBuilder()
                .AddJsonFile("SftpServerLibSettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"SftpServerLibSettings.{EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appSettings.{EnvironmentName}.json", optional: true, reloadOnChange: true);

            #region Add additional environment specific configuration sources
            switch (EnvironmentName)
            {
                case "CI":
                case "AZURECI":
                case "PRODUCTION":
                case "STAGING":
                    // get (environment dependent) settings from Azure key vault if any exist
                    IConfigurationRoot vaultConfig = new ConfigurationBuilder()
                        .AddJsonFile($"AzureKeyVault.{EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .Build();

                    var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var cert = store.Certificates.Find(X509FindType.FindByThumbprint, vaultConfig["AzureCertificateThumbprint"], false);

                    if (cert.OfType<X509Certificate2>().Count() == 1)
                    {
                        CfgBuilder.AddAzureKeyVault(vaultConfig["AzureVaultName"], vaultConfig["AzureClientID"], cert.OfType<X509Certificate2>().Single());
                    }
                    else
                    {
                        throw new ApplicationException($"Found {cert.OfType<X509Certificate2>().Count()} certificate(s) to access Azure Key Vault for environment {EnvironmentName}, expected 1");
                    }

                    break;

                case null:  // for framework GUI project
                case "DEVELOPMENT":
                    CfgBuilder.AddUserSecrets(Assembly.GetEntryAssembly());
                    break;

                default: // Unsupported environment name	
                    throw new InvalidOperationException($"Current environment name ({EnvironmentName}) is not supported in Configuration.cs");

            }
            #endregion

            ApplicationConfiguration = CfgBuilder.Build() as ConfigurationRoot;
        }

        /// <summary>
        /// Initializes the global Serilog Log object based on configuration provided by the caller
        /// </summary>
        /// <param name="configuration"></param>
        public static void InitializeSerilog(IConfiguration configuration)
        {
            // Initialize Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.With<UtcTimestampEnricher>()
                .CreateLogger();
            Assembly processAssembly = Assembly.GetEntryAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(processAssembly.Location);
            Log.Information($"Process launched:{Environment.NewLine}" +
                            $"\tProduct Name <{fileVersionInfo.ProductName}>{Environment.NewLine}" +
                            $"\tAssembly version <{fileVersionInfo.ProductVersion}>{Environment.NewLine}" +
                            $"\tAssembly location <{processAssembly.Location}>{Environment.NewLine}" +
                            $"\tASPNETCORE_ENVIRONMENT = <{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}>{Environment.NewLine}");
        }

        /// <summary>
        /// Provides a newly constructed instance of ApplicationDbContext based on a previously assigned connection string
        /// </summary>
        public static ApplicationDbContext NewMapDbContext
        {
            get
            {
                if (MapDbContextOptions == null)
                {
                    throw new ApplicationException("Attempt to create an instance of ApplicationDbContext without assigning a connection string");
                }

                return new ApplicationDbContext(MapDbContextOptions);
            }
        }

    }
}
