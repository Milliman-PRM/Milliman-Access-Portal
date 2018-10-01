using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MapQueryAdminWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                #region get Environment
                string envName;

                envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                if (string.IsNullOrWhiteSpace(envName))
                {
                    envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", EnvironmentVariableTarget.User);
                }

                // Return a default value if we haven't found anything yet
                if (string.IsNullOrWhiteSpace(envName))
                {
                    envName = "Development";
                }
                #endregion

                config.AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{envName}.json", true)
                .AddJsonFile($"AzureKeyVault.{envName}.json", true);

                #region configure key vault
                // If a key vault URL was defined, add Azure Key Vault configuration values
                IConfigurationRoot root = config.Build();
                string vaultUrl = root["AzureVaultName"];
                if (!string.IsNullOrWhiteSpace(vaultUrl))
                {
                    var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var cert = store.Certificates.Find(X509FindType.FindByThumbprint, root["AzureCertificateThumbprint"], false);

                    config.AddAzureKeyVault(
                        root["AzureVaultName"],
                        root["AzureClientID"],
                        cert.OfType<X509Certificate2>().Single()
                        );
                }

                if (envName == "Development")
                {
                    config.AddUserSecrets<Program>();
                }

                #endregion
            })
                .UseStartup<Startup>();
    }
}
