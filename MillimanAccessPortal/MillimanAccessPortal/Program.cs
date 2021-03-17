/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Entry point of the MAP server application process
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Event;
using AuditLogLib.Models;
using FileDropLib;
using System;
using System.Collections.Generic;
using MapCommonLib;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MapDbContextLib.Context;
using Npgsql;
using Prm.EmailQueue;
using Prm.SerilogCustomization;
using Serilog;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MapDbContextLib.Identity;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

namespace MillimanAccessPortal
{
    public class Program
    {

        public static async Task Main(string[] args)
        {
            var host = RunTimeBuildHost(args);

            using (var scope = host.Services.CreateScope())
            {
                IServiceProvider serviceProvider = scope.ServiceProvider;

                #region Initialize global resources
                IConfiguration Configuration = serviceProvider.GetService<IConfiguration>();

                GlobalFunctions.DomainValRegex = Configuration.GetValue("Global:DomainValidationRegex", GlobalFunctions.DomainValRegex);
                GlobalFunctions.EmailValRegex = Configuration.GetValue("Global:EmailValidationRegex", GlobalFunctions.EmailValRegex);
                GlobalFunctions.FileDropValRegex = Configuration.GetValue("Global:FileDropValidationRegex", GlobalFunctions.FileDropValRegex);
                GlobalFunctions.MaxFileUploadSize = Configuration.GetValue("Global:MaxFileUploadSize", GlobalFunctions.MaxFileUploadSize);
                GlobalFunctions.VirusScanWindowSeconds = Configuration.GetValue("Global:VirusScanWindowSeconds", GlobalFunctions.VirusScanWindowSeconds);
                GlobalFunctions.DefaultClientDomainListCountLimit = Configuration.GetValue("Global:DefaultClientDomainListCountLimit", GlobalFunctions.DefaultClientDomainListCountLimit);
                GlobalFunctions.MillimanSupportEmailAlias = Configuration.GetValue("SupportEmailAlias", "map.support@milliman.com");
                GlobalFunctions.NonLimitedDomains = 
                    (
                        Configuration.GetValue<string>("Global:NonLimitedDomains", null)
                        ?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        ?.Select(d => d.Trim())
                        ?? GlobalFunctions.NonLimitedDomains
                    ).ToList();

                // Initialize Serilog
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(Configuration)
                    .Enrich.With<UtcTimestampEnricher>()
                    .CreateLogger();

                #region Configure Audit Logger connection string
                string auditLogConnectionString = Configuration.GetConnectionString("AuditLogConnectionString");

                // If the database name is defined in the environment, update the connection string
                if (Environment.GetEnvironmentVariable("AUDIT_LOG_DATABASE_NAME") != null)
                {
                    Npgsql.NpgsqlConnectionStringBuilder stringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(auditLogConnectionString)
                    {
                        Database = Environment.GetEnvironmentVariable("AUDIT_LOG_DATABASE_NAME")
                    };
                    auditLogConnectionString = stringBuilder.ConnectionString;
                }

                AuditLogger.Config = new AuditLoggerConfiguration
                {
                    AuditLogConnectionString = auditLogConnectionString,
                    ErrorLogRootFolder = Configuration.GetValue<string>("Storage:ApplicationLog"),
                };
                #endregion
                #endregion

                Assembly processAssembly = Assembly.GetEntryAssembly();
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(processAssembly.Location);
                NpgsqlConnectionStringBuilder cxnStrBuilder = new NpgsqlConnectionStringBuilder(Configuration.GetConnectionString("DefaultConnection"));

                FileDropOperations.MapDbConnectionString = cxnStrBuilder.ConnectionString;

                Log.Information($"Process launched:{Environment.NewLine}" +
                                $"    Product Name <{fileVersionInfo.ProductName}>{Environment.NewLine}" +
                                $"    Assembly version <{fileVersionInfo.ProductVersion}>{Environment.NewLine}" +
                                $"    Assembly location <{processAssembly.Location}>{Environment.NewLine}" +
                                $"    Host environment is <{host.Services.GetService<IHostEnvironment>().EnvironmentName}>{Environment.NewLine}" +
                                $"    Using MAP database {cxnStrBuilder.Database} on host {cxnStrBuilder.Host}");

                try
                {
                    await ApplicationDbContext.InitializeAllAsync(serviceProvider);

                    MailSender.ConfigureMailSender(new SmtpConfig
                    {
                        SmtpServer = Configuration.GetValue<string>("SmtpServer"),
                        SmtpPort = Configuration.GetValue<int>("SmtpPort"),
                        SmtpFromAddress = Configuration.GetValue<string>("SmtpFromAddress"),
                        SmtpFromName = Configuration.GetValue<string>("SmtpFromName"),
                        SmtpUsername = Configuration.GetValue<string>("SmtpUsername"),
                        SmtpPassword = Configuration.GetValue<string>("SmtpPassword"),
                        SendGridApiKey = Configuration.GetValue<string>("SendGridApiKey"),
                        EmailDisclaimer = Configuration.GetValue<string>("EmailDisclaimer"),
                        DisclaimerExemptDomainString = Configuration.GetValue<string>("DisclaimerExemptDomainString"),
                    });

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
        /// Renamed implementation of BuildHost, forces execution of <see cref="ApplicationDbContextFactory.CreateDbContext"/> 
        /// when deploying migrations, rather than running entire Startup configuration.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        //public static IWebHost RunTimeBuildWebHost(string[] args)
        public static IHost RunTimeBuildHost(string[] args)
        {
            string EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                                  ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                    })
                    .UseStartup<Startup>()
                    .ConfigureAppConfiguration((hostContext, config) => SetApplicationConfiguration(hostContext.HostingEnvironment.EnvironmentName, config))
                    .ConfigureLogging((hostingContext, config) => config.ClearProviders());  // remove ASP default logger
                });

            string doLogHostSetting = Environment.GetEnvironmentVariable("ENABLE_MAP_HOST_LOGGING");
            if (!string.IsNullOrWhiteSpace(doLogHostSetting) && doLogHostSetting.Equals("true", StringComparison.InvariantCultureIgnoreCase))
            {
                // includes highly detailed .NET logging to the Serilog sinks
                hostBuilder = hostBuilder.UseSerilog();
            }

            var host = hostBuilder.Build();
            return host;
        }

        internal static void SetApplicationConfiguration(string environmentName, IConfigurationBuilder configBuilder)
        {
            configBuilder
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
                    case "INTERNAL":
                        configBuilder.AddJsonFile($"AzureKeyVault.{environmentName}.json", optional: true, reloadOnChange: true);

                        var builtConfig = configBuilder.Build();

                        var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                        store.Open(OpenFlags.ReadOnly);
                        var cert = store.Certificates.Find(X509FindType.FindByThumbprint, builtConfig["AzureCertificateThumbprint"], false);

                        configBuilder.AddAzureKeyVault(
                            builtConfig["AzureVaultName"],
                            builtConfig["AzureClientID"],
                            cert.OfType<X509Certificate2>().Single()
                            );
                        break;

                    case "AZURE-ISDEV":
                    case "AZURE-DEV":
                    case "AZURE-UAT":
                    case "AZURE-PROD":
                        // These environments are in Azure Web Apps and don't require certificates to access the Key Vault
                        configBuilder.AddJsonFile($"AzureKeyVault.{environmentName}.json", optional: true, reloadOnChange: true);
                        var azureBuiltConfig = configBuilder.Build();

                        var secretClient = new SecretClient(new Uri(azureBuiltConfig["AzureVaultName"]), new DefaultAzureCredential());
                        configBuilder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                        break;
                    case "DEVELOPMENT":
                        configBuilder.AddUserSecrets<Startup>();
                        break;

                    default: // Unsupported environment name	
                        throw new InvalidOperationException($"Current environment name ({environmentName}) is not supported in Program.cs");

                }
            }
            #endregion

            AddEnvironmentSuppliedConfigurationOverrides(configBuilder);
        }

        private static void AddEnvironmentSuppliedConfigurationOverrides(IConfigurationBuilder appCfgBuilder)
        {
            MemoryConfigurationSource newSource = null;

            var localConfig = appCfgBuilder.Build();

            string mapDbConnectionString = localConfig.GetConnectionString("DefaultConnection");
            NpgsqlConnectionStringBuilder mapDbConnectionStringBuilder = new NpgsqlConnectionStringBuilder(mapDbConnectionString);

            string auditLogDbConnectionString = localConfig.GetConnectionString("AuditLogConnectionString");
            NpgsqlConnectionStringBuilder auditLogDbConnectionStringBuilder = new NpgsqlConnectionStringBuilder(auditLogDbConnectionString);

            string dbNameOverride = Environment.GetEnvironmentVariable("APP_DATABASE_NAME");
            if (!string.IsNullOrWhiteSpace(dbNameOverride))
            {
                newSource = newSource ?? new MemoryConfigurationSource();

                mapDbConnectionStringBuilder.Database = dbNameOverride;
            }

            string dbServerOverride = Environment.GetEnvironmentVariable("MAP_DATABASE_SERVER");
            if (!string.IsNullOrWhiteSpace(dbServerOverride))
            {
                newSource = newSource ?? new MemoryConfigurationSource();

                mapDbConnectionStringBuilder.Host = dbServerOverride;
                auditLogDbConnectionStringBuilder.Host = dbServerOverride;
            }

            if (newSource != null)
            {
                var newData = new List<KeyValuePair<string, string>>();
                if (!mapDbConnectionStringBuilder.EquivalentTo(new NpgsqlConnectionStringBuilder(mapDbConnectionString)))
                {
                    newData.Add(new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", mapDbConnectionStringBuilder.ConnectionString));
                }
                if (!auditLogDbConnectionStringBuilder.EquivalentTo(new NpgsqlConnectionStringBuilder(auditLogDbConnectionString)))
                {
                    newData.Add(new KeyValuePair<string, string>("ConnectionStrings:AuditLogConnectionString", auditLogDbConnectionStringBuilder.ConnectionString));
                }
                newSource.InitialData = newData;
                var newConnectionStringCfg = new ConfigurationRoot(new List<IConfigurationProvider> { new MemoryConfigurationProvider(newSource) });

                appCfgBuilder.AddConfiguration(newConnectionStringCfg);
            }
        }

    }
}
