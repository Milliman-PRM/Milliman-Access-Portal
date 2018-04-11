/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Provides get/set access to configuration information for use throughout the application
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

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
                .AddUserSecrets<MapDbJobMonitor>();

            // TODO .Add... environment dependent configuration content (e.g. for AzureKeyVault in CI and production environments)
            // Make sure to consider running both as service and GUI app, (secrets not available as service)

            ApplicationConfiguration = CfgBuilder.Build();
        }

        public static IConfigurationRoot ApplicationConfiguration { get; set; } = null;

        public static string GetConnectionString(string CxnStringName)
        {
            return ApplicationConfiguration.GetConnectionString(CxnStringName);
        }
    }
}
