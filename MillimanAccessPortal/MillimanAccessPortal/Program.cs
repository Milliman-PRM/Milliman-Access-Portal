using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using MapDbContextLib.Context;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
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
                    .AddJsonFile("AzureKeyVault.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"AzureKeyVault.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    ;

                    #region Configure Azure Key Vault
                    var builtConfig = config.Build();

                    var store = new X509Store(StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var cert = store.Certificates.Find(X509FindType.FindByThumbprint, builtConfig["AzureCertificateThumbprint"], false);
                    
                    config.AddAzureKeyVault(
                        builtConfig["AzureVaultName"],
                        builtConfig["AzureClientID"],
                        cert.OfType<X509Certificate2>().Single()
                        );

                    // TODO: Remove this line
                    var newConfig = config.Build();

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
