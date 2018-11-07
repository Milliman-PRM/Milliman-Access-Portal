using AuditLogLib.Event;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MapDbContextLib.Context;
using Serilog;
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

                #region Initialize global resources
                IConfiguration Configuration = serviceProvider.GetService<IConfiguration>();

                GlobalFunctions.domainValRegex = Configuration.GetValue("Global:DomainValidationRegex", GlobalFunctions.domainValRegex);
                GlobalFunctions.emailValRegex = Configuration.GetValue("Global:EmailValidationRegex", GlobalFunctions.emailValRegex);
                GlobalFunctions.maxFileUploadSize = Configuration.GetValue("Global:MaxFileUploadSize", GlobalFunctions.maxFileUploadSize);
                GlobalFunctions.virusScanWindowSeconds = Configuration.GetValue("Global:VirusScanWindowSeconds", GlobalFunctions.virusScanWindowSeconds);

                // Initialize Serilog
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(Configuration)
                    .CreateLogger();
                #endregion

                try
                {
                    ApplicationDbContext.InitializeAll(serviceProvider);
                }
                catch (Exception e)
                {
                    Log.Error($"ApplicationDbContext.InitializeAll() failed: {e.Message}");
                    throw;
                }
            }

            AuditEventTypeBase.SetPathToRemove();

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            string EnvironmentNameUpper = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToUpper();

            var webHost = 
            WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("qlikview.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"qlikview.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("smtp.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"smtp.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    ;

                    #region Configure Azure Key Vault for CI & Production
                    switch (EnvironmentNameUpper)
                    {
                        case "AZURECI":
                        case "PRODUCTION":
                        case "STAGING":
                            config.AddJsonFile($"AzureKeyVault.{EnvironmentNameUpper}.json", optional: true, reloadOnChange: true);

                            var builtConfig = config.Build();

                            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
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
                            throw new InvalidOperationException($"Current environment name ({EnvironmentNameUpper}) is not supported in Program.cs");

                    }
                    #endregion
                })
            .ConfigureLogging((hostingContext, config) => config.ClearProviders())  // remove asp default logger
            // .UseApplicationINsights() is removed due to use of Serilog. Consider package serilog.sinks.applicationinsights if this is needed.
            ;

            IWebHost TempIWebHost = webHost.Build();
            bool VerboseDotNetLogging = TempIWebHost.Services.GetService<IConfiguration>().GetValue("VerboseDotNetLogging", false);

            if (VerboseDotNetLogging)
            {
                // includes highly detailed .NET logging to the Serilog sinks
                webHost = webHost.UseSerilog();
            }

            return webHost.Build();
        }
    }
}
