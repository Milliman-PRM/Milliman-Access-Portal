using AuditLogLib.Event;
using System;
using System.Collections.Generic;
using MapCommonLib;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MapDbContextLib.Context;
using Serilog;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal
{
    public class Program
    {

        public static async Task Main(string[] args)
        {
            var host = RunTimeBuildWebHost(args);

            using (var scope = host.Services.CreateScope())
            {
                IServiceProvider serviceProvider = scope.ServiceProvider;

                #region Initialize global resources
                IConfiguration Configuration = serviceProvider.GetService<IConfiguration>();

                GlobalFunctions.DomainValRegex = Configuration.GetValue("Global:DomainValidationRegex", GlobalFunctions.DomainValRegex);
                GlobalFunctions.EmailValRegex = Configuration.GetValue("Global:EmailValidationRegex", GlobalFunctions.EmailValRegex);
                GlobalFunctions.MaxFileUploadSize = Configuration.GetValue("Global:MaxFileUploadSize", GlobalFunctions.MaxFileUploadSize);
                GlobalFunctions.VirusScanWindowSeconds = Configuration.GetValue("Global:VirusScanWindowSeconds", GlobalFunctions.VirusScanWindowSeconds);
                GlobalFunctions.ClientDomainListCountLimit = Configuration.GetValue("ClientDomainListCountLimit", GlobalFunctions.ClientDomainListCountLimit);

                // Initialize Serilog
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(Configuration)
                    .CreateLogger();
                #endregion

                try
                {
                    await ApplicationDbContext.InitializeAll(serviceProvider);
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

        /// <summary>
        /// Renamed implementation of BuildWebHost, forces execution of <see cref="ApplicationDbContextFactory.CreateDbContext"/> 
        /// when deploying migrations, rather than running entire Startup configuration.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IWebHost RunTimeBuildWebHost(string[] args)
        {
            string EnvironmentNameUpper = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToUpper();

            var webHost = 
            WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((hostContext, config) => SetApplicationConfiguration(hostContext.HostingEnvironment.EnvironmentName, config))
            .ConfigureLogging((hostingContext, config) => config.ClearProviders())  // remove ASP default logger
            ;

            if (new List<string> { "DEVELOPMENT", "STAGING" }.Contains(EnvironmentNameUpper))
            {
                // includes highly detailed .NET logging to the Serilog sinks
                webHost = webHost.UseSerilog();
            }

            return webHost.Build();
        }

        internal static void SetApplicationConfiguration(string environmentName, IConfigurationBuilder config)
        {
            config
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("powerbi.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"powerbi.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("qlikview.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"qlikview.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("smtp.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"smtp.{environmentName}.json", optional: true, reloadOnChange: true)
            ;

            #region Load environment specific additional configuration
            if (!string.IsNullOrWhiteSpace(environmentName))
            {
                switch (environmentName.ToUpper())
                {
                    case "AZURECI":
                    case "PRODUCTION":
                    case "STAGING":
                        config.AddJsonFile($"AzureKeyVault.{environmentName}.json", optional: true, reloadOnChange: true);

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
                        throw new InvalidOperationException($"Current environment name ({environmentName}) is not supported in Program.cs");

                }
            }
            #endregion

            string dbNameOverride = Environment.GetEnvironmentVariable("APP_DATABASE_NAME");
            if (!string.IsNullOrWhiteSpace(dbNameOverride))
            {
                var localConfig = config.Build();
                string configuredConnectionString = localConfig.GetConnectionString("DefaultConnection");

                Npgsql.NpgsqlConnectionStringBuilder connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(configuredConnectionString);
                connectionStringBuilder.Database = dbNameOverride;

                string newConnectionString = connectionStringBuilder.ConnectionString;

                MemoryConfigurationSource newSource = new MemoryConfigurationSource();
                newSource.InitialData = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", newConnectionString) };
                var newConnectionStringCfg = new ConfigurationRoot(new List<IConfigurationProvider> { new MemoryConfigurationProvider(newSource) });
                config.AddConfiguration(newConnectionStringCfg);
            }
        }
    }
}
