/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Entry point for the application
 * DEVELOPER NOTES: Can be run directly as a console application or installed/run as a Windows service
 */

using ContentPublishingLib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prm.SerilogCustomization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ContentPublishingService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development", EnvironmentVariableTarget.Process);

            IHost host = CreateHostBuilder(args).Build();

            Configuration.ApplicationConfiguration = host.Services.GetRequiredService<IConfiguration>() as ConfigurationRoot;

            Thread.Sleep(Configuration.ApplicationConfiguration.GetValue("ServiceLaunchDelaySec", 0) * 1000);

            #region Use the built configuration to adjust service behaviors
            ((OptionsManager<HostOptions>)host.Services.GetRequiredService<IOptions<HostOptions>>()).Value.ShutdownTimeout = 
                Configuration.ApplicationConfiguration.GetValue("StopWaitTimeSeconds", TimeSpan.FromSeconds(180));  // Allow time at server shutdown for server tasks to complete
            #endregion

            Assembly processAssembly = Assembly.GetAssembly(typeof(Program));
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(processAssembly.Location);
            bool isService = WindowsServiceHelpers.IsWindowsService();
            string introMsg = $"Process launched:{Environment.NewLine}" +
                              $"    Product Name <{fileVersionInfo.ProductName}>{Environment.NewLine}" +
                              $"    assembly version <{fileVersionInfo.ProductVersion}>{Environment.NewLine}" +
                              $"    assembly location <{processAssembly.Location}>{Environment.NewLine}" +
                              $"    host environment is <{host.Services.GetService<IHostEnvironment>().EnvironmentName}>{Environment.NewLine}" +
                              $"    running as {(isService ? "Windows service" : "console application")}";

            Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(Configuration.ApplicationConfiguration)
                    .Enrich.With<UtcTimestampEnricher>()
                    .CreateLogger();

            Log.Information(introMsg);

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureAppConfiguration((context, config) =>
                {
                    AddEnvironmentDependentConfigSources(config, context.HostingEnvironment.EnvironmentName, args);
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.ClearProviders();  // remove default logger(s)
                    builder.AddSerilog();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Configure DI services here.  <See https://codeburst.io/create-a-windows-service-app-in-net-core-3-0-5ecb29fb5ad0>
                    services.AddHostedService<Worker>();
                });

            return host;
        }

        public static IConfigurationBuilder AddEnvironmentDependentConfigSources(IConfigurationBuilder builder, string environmentName, string[] args)
        {
            switch (environmentName)
            {
                case string c when c.Equals("CI", StringComparison.InvariantCultureIgnoreCase):
                case string i when i.Equals("Internal", StringComparison.InvariantCultureIgnoreCase):
                case string a when a.Equals("AzureCI", StringComparison.InvariantCultureIgnoreCase):
                case string p when p.Equals("PRODUCTION", StringComparison.InvariantCultureIgnoreCase):
                case string s when s.Equals("STAGING", StringComparison.InvariantCultureIgnoreCase):
                    IConfigurationRoot vaultConfig = new ConfigurationBuilder()
                        .AddJsonFile($"AzureKeyVault.{environmentName}.json", optional: true, reloadOnChange: true)
                        .Build();
                    var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var cert = store.Certificates.Find(X509FindType.FindByThumbprint, vaultConfig["AzureCertificateThumbprint"], false);

                    try
                    {
                        builder.AddAzureKeyVault(vaultConfig["AzureVaultName"], vaultConfig["AzureClientID"], cert.OfType<X509Certificate2>().Single());
                    }
                    catch (InvalidOperationException)
                    {
                        throw new ApplicationException($"Found {cert.OfType<X509Certificate2>().Count()} certificate(s) to access Azure Key Vault for environment {environmentName}, expected Single");
                    }

                    break;
            }

            return builder;
        }

    }
}
