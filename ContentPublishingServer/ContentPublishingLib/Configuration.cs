/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Provides get/set access to configuration information for use throughout the application
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace ContentPublishingLib
{
    public static class Configuration
    {
        /// <summary>
        /// Intended to be invoked at the application project level using json managed by the application
        /// </summary>
        public static void LoadConfiguration()
        {
            string EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToUpper();

            IConfigurationBuilder CfgBuilder = new ConfigurationBuilder()
                .AddJsonFile("contentPublicationLibSettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"contentPublicationLibSettings.{EnvironmentName}.json", optional: true, reloadOnChange: true)
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

                    break;

                case null:  // for framework GUI project
                case "DEVELOPMENT":
                    CfgBuilder.AddUserSecrets<ProcessManager>();
                    break;

                default: // Unsupported environment name	
                    throw new InvalidOperationException($"Current environment name ({EnvironmentName}) is not supported in Configuration.cs");

            }
            #endregion

            ApplicationConfiguration = CfgBuilder.Build();

        }

        public static IConfigurationRoot ApplicationConfiguration { get; set; } = null;

        public static string GetConfigurationValue(string Key)
        {
            return ApplicationConfiguration[Key];
        }

        public static string GetConnectionString(string CxnStringName)
        {
            return ApplicationConfiguration.GetConnectionString(CxnStringName);
        }
    }
}
