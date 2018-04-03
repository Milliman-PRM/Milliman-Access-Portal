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
        public static void GetConfiguration()
        {
            ConfigurationBuilder CfgBuilder = new ConfigurationBuilder();
            CfgBuilder.AddUserSecrets<MapDbJobMonitor>()
                        .AddJsonFile(path: "appsettings.json", optional: true, reloadOnChange: true);

            // TODO add something for AzureKeyVault in CI and production environments

            Cfg = CfgBuilder.Build();
        }
        public static IConfigurationRoot Cfg { get; set; } = null;
    }
}
