using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MapDbContextLib.Context;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using MapCommonLib;

namespace MillimanAccessPortal
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);

            using (var scope = host.Services.CreateScope())
            {
                IServiceProvider serviceProvider = scope.ServiceProvider;

                #region Initialize global expressions
                IConfiguration Configuration = serviceProvider.GetService<IConfiguration>();
                if (!string.IsNullOrWhiteSpace(Configuration.GetValue<string>("Global:DomainValidationRegex")))
                {
                    GlobalFunctions.domainValRegex = Configuration.GetValue<string>("Global:DomainValidationRegex");
                }
                if (!string.IsNullOrWhiteSpace(Configuration.GetValue<string>("Global:EmailValidationRegex")))
                {
                    GlobalFunctions.emailValRegex = Configuration.GetValue<string>("Global:EmailValidationRegex");
                }
                #endregion

                try
                {
                    ApplicationDbContext.InitializeAll(serviceProvider);
                }
                catch (Exception e)
                {
                    serviceProvider.GetService<ILoggerFactory>().CreateLogger<ApplicationDbContext>().LogError($"ApplicationDbContext.InitializeAll() failed: {e.Message}");
                    throw;
                }
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

                    #region Configure Azure Key Vault for CI & Production
                    if (hostContext.HostingEnvironment.EnvironmentName == "CI" || hostContext.HostingEnvironment.EnvironmentName == "Production")
                    {
                        config.AddJsonFile($"AzureKeyVault.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);

                        var builtConfig = config.Build();

                        var store = new X509Store(StoreLocation.LocalMachine);
                        store.Open(OpenFlags.ReadOnly);
                        var cert = store.Certificates.Find(X509FindType.FindByThumbprint, builtConfig["AzureCertificateThumbprint"], false);

                        config.AddAzureKeyVault(
                            builtConfig["AzureVaultName"],
                            builtConfig["AzureClientID"],
                            cert.OfType<X509Certificate2>().Single()
                            );
                    }
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
