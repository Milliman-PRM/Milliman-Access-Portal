/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Dumps configuration provider and key/value pair details
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SftpServerLib
{
    public class ConfigurationDumper
    {
        public enum DumpTarget
        {
            Serilog,
            Console,
        }

        public static void DumpConfigurationDetails(string environmentName, IConfigurationBuilder appConfigurationBuilder, IConfiguration config, DumpTarget target = DumpTarget.Serilog)
        {
            StringBuilder dumpStringBuilder = new StringBuilder();
            dumpStringBuilder.AppendLine($"ASPNETCORE_ENVIRONMENT is <{environmentName}>, there are {appConfigurationBuilder.Sources.Count} configuration sources");
            int keyCounter = 0;
            var allSources = appConfigurationBuilder.Sources.ToList();
            foreach (var oneSource in allSources)
            {
                switch (oneSource)
                {
                    case var s when s is ChainedConfigurationSource:
                        {
                            ChainedConfigurationSource source = s as ChainedConfigurationSource;
                            var provider = source.Build(appConfigurationBuilder);
                            dumpStringBuilder.AppendLine($"ChainedConfigurationSource source with ...");
                            foreach (var key in provider.GetAllChildKeyNames())
                            {
                                provider.TryGet(key, out string val);
                                dumpStringBuilder.AppendLine($"    Config Key {++keyCounter} named <{key}>: Value <{val}>");
                            }
                        }
                        break;

                    case var s when s is JsonConfigurationSource:
                        {
                            JsonConfigurationSource source = s as JsonConfigurationSource;
                            IConfigurationProvider provider = source.Build(appConfigurationBuilder);
                            provider.Load();
                            dumpStringBuilder.AppendLine($"JsonConfigurationSource source with path {Path.Combine(source.FileProvider is PhysicalFileProvider ? (source.FileProvider as PhysicalFileProvider).Root : "", source.Path)}");
                            foreach (var key in provider.GetAllChildKeyNames())
                            {
                                provider.TryGet(key, out string val);
                                dumpStringBuilder.AppendLine($"    Config Key {++keyCounter} named <{key}>: Value <{val}>");
                            }
                        }
                        break;

                    // case var s when s is AzureKeyVaultConfigurationSource 
                    // AzureKeyVaultConfigurationSource is inaccessible due to class declaration as `internal`, so key vault source details can't be dumped here

                    default:
                        {
                            IConfigurationProvider provider = oneSource.Build(appConfigurationBuilder);
                            provider.Load();
                            dumpStringBuilder.AppendLine($"Generic (for logging purposes) configuration source of type {oneSource.GetType().Name}");
                            foreach (var key in provider.GetAllChildKeyNames())
                            {
                                provider.TryGet(key, out string val);
                                dumpStringBuilder.AppendLine($"    Config Key {++keyCounter} named <{key}>: Value <{val}>");
                            }
                        }
                        break;
                }
            }

            dumpStringBuilder.AppendLine($"From combined providers/sources:");
            foreach (var kvp in config.AsEnumerable())
            {
                dumpStringBuilder.AppendLine($"    Config Key <{kvp.Key}>: Value <{kvp.Value}>");
            }

            switch (target)
            {
                case DumpTarget.Console:
                    Console.Write(dumpStringBuilder.ToString());
                    break;
                case DumpTarget.Serilog:
                    Log.Information(dumpStringBuilder.ToString());
                    break;
            }
        }
    }
}

namespace Microsoft.Extensions.Configuration
{
    internal static class ConfigurationProviderExtensions
    {
        internal static HashSet<string> GetAllChildKeyNames(this IConfigurationProvider provider, string parentKey = null)
        {
            HashSet<string> resultKeys = new HashSet<string>();

            var uniqueChildKeys = provider.GetChildKeys(Enumerable.Empty<string>(), parentKey).ToHashSet();
            foreach (var key in uniqueChildKeys)
            {
                string foundChildKey =
                    parentKey != null
                    ? parentKey + ":" + key
                    : key;

                var children = GetAllChildKeyNames(provider, foundChildKey);

                if (!children.Any())
                {
                    resultKeys.Add(foundChildKey);
                }
                else
                {
                    resultKeys = resultKeys.Concat(children).ToHashSet();
                }
            }

            return resultKeys;
        }
    }
}
