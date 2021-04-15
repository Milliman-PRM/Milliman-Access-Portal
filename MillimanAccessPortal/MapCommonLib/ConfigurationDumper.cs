/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Generates a represent of key/value pairs in all sources of appsettings currently configured for the current process
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MapCommonLib
{
    public class ConfigurationDumper
    {
        public static string DumpConfigurationDetails(string environmentName, IConfigurationBuilder appConfigurationBuilder, IConfiguration config = null)
        {
            StringBuilder resultString = new StringBuilder($"Application environment is <{environmentName}>, there are {appConfigurationBuilder.Sources.Count} configuration sources");
            resultString.AppendLine();

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
                            resultString.AppendLine($"ChainedConfigurationSource source with ...");
                            foreach (var key in provider.GetAllChildKeyNames())
                            {
                                provider.TryGet(key, out string val);
                                resultString.AppendLine($"    Config Key {++keyCounter} named <{key}>: Value <{val}>");
                            }
                        }
                        break;

                    case var s when s is JsonConfigurationSource:
                        {
                            JsonConfigurationSource source = s as JsonConfigurationSource;
                            IConfigurationProvider provider = source.Build(appConfigurationBuilder);
                            provider.Load();
                            resultString.AppendLine($"JsonConfigurationSource source with path {Path.Combine(source.FileProvider is PhysicalFileProvider ? (source.FileProvider as PhysicalFileProvider).Root : "", source.Path)}");
                            foreach (var key in provider.GetAllChildKeyNames())
                            {
                                provider.TryGet(key, out string val);
                                resultString.AppendLine($"    Config Key {++keyCounter} named <{key}>: Value <{val}>");
                            }
                        }
                        break;

                    // case var s when s is AzureKeyVaultConfigurationSource:
                    // AzureKeyVaultConfigurationSource is inaccessible due to class declaration as `internal`, so source details can't be dumped here
                        // break;

                    default:
                        {
                            IConfigurationProvider provider = oneSource.Build(appConfigurationBuilder);
                            provider.Load();
                            resultString.AppendLine($"Generic (for logging purposes) configuration source of type {oneSource.GetType().Name}");
                            foreach (var key in provider.GetAllChildKeyNames())
                            {
                                provider.TryGet(key, out string val);
                                resultString.AppendLine($"    Config Key {++keyCounter} named <{key}>: Value <{val}>");
                            }
                        }
                        break;
                }
                resultString.AppendLine();
            }

            if (config != null)
            {
                foreach (var kvp in config.AsEnumerable())
                {
                    resultString.AppendLine($"    Current configuration in effect from combined providers/sources, config Key <{kvp.Key}>: Value <{kvp.Value}>");
                }
            }

            return resultString.ToString();
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
