/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Provides get/set access to configuration information for use throughout the application
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace ContentReductionLib
{
    public static class Configuration
    {
        /// <summary>
        /// Intended to be invoked at the application project level using json managed by the application
        /// </summary>
        public static void LoadConfiguration()
        {
            IConfigurationBuilder CfgBuilder = new ConfigurationBuilder()
                .AddJsonFile(path: "contentReductionLibSettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(path: "appSettings.json", optional: true, reloadOnChange: true)
                ;

            // Add environment dependent configuration
            string EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            switch (EnvironmentName)
            {
                case "CI":
                case "AzureProduction":
                    CfgBuilder.AddJsonFile($"AzureKeyVault.{EnvironmentName}.json", optional: true, reloadOnChange: true);

                    var builtConfig = CfgBuilder.Build();
                    System.Console.WriteLine($"AzureVaultName = {builtConfig["AzureVaultName"]}");
                    System.Console.WriteLine($"AzureClientID = {builtConfig["AzureClientID"]}");
                    System.Console.WriteLine($"AzureCertificateThumbprint = {builtConfig["AzureCertificateThumbprint"]}");

                    var store = new X509Store(StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    System.Console.WriteLine($"store has {store.Certificates.Count} certificates");
                    System.Console.WriteLine($"store name is {store.Name}");
                    var cert = store.Certificates.Find(X509FindType.FindByThumbprint, builtConfig["AzureCertificateThumbprint"], false);

                    CfgBuilder.AddAzureKeyVault(
                        builtConfig["AzureVaultName"],
                        builtConfig["AzureClientID"],
                        cert.OfType<X509Certificate2>().Single()
                        );
                    break;

                case "Development":
                    CfgBuilder.AddUserSecrets<MapDbJobMonitor>();
                    break;

                default: // Unsupported environment name	
                    throw new InvalidOperationException($"Current environment name ({EnvironmentName}) is not supported in Configuration.cs");

            }

            ApplicationConfiguration = CfgBuilder.Build();
            foreach (var x in ApplicationConfiguration.AsEnumerable().ToList())
            {
                System.Console.WriteLine($"config entry key {x.Key} value is {x.Value}");
            }
            
        }

        public static IConfigurationRoot ApplicationConfiguration { get; set; } = null;

        public static string GetConnectionString(string CxnStringName)
        {
            return ApplicationConfiguration.GetConnectionString(CxnStringName);
        }
    }
}
