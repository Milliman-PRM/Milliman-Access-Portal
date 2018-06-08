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

                GlobalFunctions.domainValRegex = Configuration.GetValue("Global:DomainValidationRegex", GlobalFunctions.domainValRegex);
                GlobalFunctions.emailValRegex = Configuration.GetValue("Global:EmailValidationRegex", GlobalFunctions.emailValRegex);
                GlobalFunctions.maxFileUploadSize = Configuration.GetValue("Global:MaxFileUploadSize", GlobalFunctions.maxFileUploadSize);
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
                    string EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToUpper();
                    switch (EnvironmentName)
                    {
                        case "AZURECI":
                        case "PRODUCTION":
                            config.AddJsonFile($"AzureKeyVault.{EnvironmentName}.json", optional: true, reloadOnChange: true);

                            var builtConfig = config.Build();
                        
                            var store = new X509Store(StoreName.My, (EnvironmentName == "PRODUCTION" ? StoreLocation.LocalMachine : StoreLocation.CurrentUser));
                            store.Open(OpenFlags.ReadOnly);
                            var cert = store.Certificates.Find(X509FindType.FindByThumbprint, builtConfig["AzureCertificateThumbprint"], false);

                            config.AddAzureKeyVault(
                                builtConfig["AzureVaultName"],
                                builtConfig["AzureClientID"],
                                cert.OfType<X509Certificate2>().Single()
                                );
                            break;
                        case "DEVELOPMENT":
                            config.AddUserSecrets<Startup>();
                            break;

                        default: // Unsupported environment name	
                            throw new InvalidOperationException($"Current environment name ({EnvironmentName}) is not supported in Program.cs");

                    }
                    #endregion
                })
            .UseApplicationInsights()    
            .Build();
    }
}
