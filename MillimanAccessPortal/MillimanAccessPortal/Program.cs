using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using MapDbContextLib.Context;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace MillimanAccessPortal
{
    public class Program
    {
        //public static void Main(string[] args)
        //{
        //    var host = new WebHostBuilder()
        //        .UseKestrel()
        //        .UseContentRoot(Directory.GetCurrentDirectory())
        //        .UseIISIntegration()
        //        .UseStartup<Startup>()
        //        .UseApplicationInsights()
        //        .Build();

        //    host.Run();
        //}

        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);

            using (var scope = host.Services.CreateScope())
            {
                IServiceProvider serviceProvider = scope.ServiceProvider;
                ApplicationDbContext.InitializeAll(serviceProvider);
            }

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("qlikview.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("smtp.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"smtp.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    ;

                    #region Configure Azure Key Vault

                    // The configuration provider doesn't provide access to values before Build() is executed below. 
                    // As a result, we need to pull in the AzureKeyVault.json ourselves & parse the values here.
                    
                    string json = File.ReadAllText(@"AzureKeyVault.json");

                    Dictionary<string, string> azureKeyVaultConfig = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                    var store = new X509Store(StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var cert = store.Certificates.Find(X509FindType.FindByThumbprint, azureKeyVaultConfig["AzureCertificateThumbprint"], false);

                    X509Certificate2 certObject = cert.OfType<X509Certificate2>().Single();

                    config.AddAzureKeyVault(
                        azureKeyVaultConfig["AzureVaultName"],
                        azureKeyVaultConfig["AzureClientID"],
                        certObject,
                        new DefaultKeyVaultSecretManager()
                        );
                    
                    #endregion

                    if (hostContext.HostingEnvironment.IsDevelopment())
                    {
                        config.AddUserSecrets<Startup>();
                    }
                })
            .UseApplicationInsights()    
            .Build();
    }
}
