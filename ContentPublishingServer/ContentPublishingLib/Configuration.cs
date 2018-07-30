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
            IConfigurationBuilder CfgBuilder = new ConfigurationBuilder()
                .AddJsonFile(path: "contentPublicationLibSettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(path: "appSettings.json", optional: true, reloadOnChange: true)
                ;

            // Add environment dependent configuration
            #region Configure Azure Key Vault for CI & Production
            string EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToUpper();
            switch (EnvironmentName)
            {
                case "AZURECI":
                case "PRODUCTION":
                    CfgBuilder.AddJsonFile($"AzureKeyVault.{EnvironmentName}.json", optional: true, reloadOnChange: true);

                    var builtConfig = CfgBuilder.Build();

                    var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var cert = store.Certificates.Find(X509FindType.FindByThumbprint, builtConfig["AzureCertificateThumbprint"], false);

                    CfgBuilder.AddAzureKeyVault(
                        builtConfig["AzureVaultName"],
                        builtConfig["AzureClientID"],
                        cert.OfType<X509Certificate2>().Single()
                        );
                    break;
                case "DEVELOPMENT":
                    
                    break;

                default: // Unsupported environment name	
                    throw new InvalidOperationException($"Current environment name ({EnvironmentName}) is not supported in Program.cs");

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
