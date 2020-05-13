/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Dumps configuration provider and key/value pair details
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestResourcesLib
{
    public class ConfigurationDumper
    {
        public static void DumpConfigurationDetails(string environmentName, IConfigurationBuilder appConfigurationBuilder, IConfiguration config)
        {
            Log.Information($"ASPNETCORE_ENVIRONMENT is <{environmentName}>, there are {appConfigurationBuilder.Sources.Count} configuration sources");
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
                            Log.Information($"ChainedConfigurationSource source with ...");
                            foreach (var key in provider.GetAllChildKeyNames())
                            {
                                provider.TryGet(key, out string val);
                                Log.Information($"    Config Key {++keyCounter} named <{key}>: Value <{val}>");
                            }
                        }
                        break;

                    case var s when s is JsonConfigurationSource:
                        {
                            JsonConfigurationSource source = s as JsonConfigurationSource;
                            IConfigurationProvider provider = source.Build(appConfigurationBuilder);
                            provider.Load();
                            Log.Information($"JsonConfigurationSource source with path {Path.Combine(source.FileProvider is PhysicalFileProvider ? (source.FileProvider as PhysicalFileProvider).Root : "", source.Path)}");
                            foreach (var key in provider.GetAllChildKeyNames())
                            {
                                provider.TryGet(key, out string val);
                                Log.Information($"    Config Key {++keyCounter} named <{key}>: Value <{val}>");
                            }
                        }
                        break;

                    // case var s when s is AzureKeyVaultConfigurationSource 
                    // AzureKeyVaultConfigurationSource is inaccessible due to class declaration as `internal`, so source details can't be dumped here

                    default:
                        {
                            IConfigurationProvider provider = oneSource.Build(appConfigurationBuilder);
                            provider.Load();
                            Log.Information($"Generic (for logging purposes) configuration source of type {oneSource.GetType().Name}");
                            foreach (var key in provider.GetAllChildKeyNames())
                            {
                                provider.TryGet(key, out string val);
                                Log.Information($"    Config Key {++keyCounter} named <{key}>: Value <{val}>");
                            }
                        }
                        break;
                }
            }

            foreach (var kvp in config.AsEnumerable())
            {
                Log.Information($"    From combined providers/sources, config Key <{kvp.Key}>: Value <{kvp.Value}>");
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
