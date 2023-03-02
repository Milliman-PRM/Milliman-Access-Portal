using MapCommonLib;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace MillimanAccessPortal.ContentProxy
{
    public class MapProxyConfigProvider : IProxyConfigProvider
    {
        private volatile MapProxyConfiguration _proxyConfig = new MapProxyConfiguration();

        // Implementation of the base interface signature
        public IProxyConfig GetConfig() => _proxyConfig;

        private void UpdateConfiguration(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters, bool logNewConfig = true)
        {
            lock(_proxyConfig)
            {
                MapProxyConfiguration oldConfig = _proxyConfig;
                _proxyConfig = new MapProxyConfiguration(routes, clusters);
                oldConfig?.SignalChange();

                if (logNewConfig)
                {
                    Log.Information($"New proxy configuration:{Environment.NewLine}" +
                                    $"  {JsonSerializer.Serialize(_proxyConfig)}{Environment.NewLine}" +
                                    $"  Route count changed by {_proxyConfig.Routes.Count - oldConfig.Routes.Count}, Cluster count changed by {_proxyConfig.Clusters.Count - oldConfig.Clusters.Count}");
                }
            }
        }

        public void RemoveExistingRoute(string contentToken)
        {
            List<RouteConfig> routesToRemove = _proxyConfig.Routes
                                                           .Where(r => r.RouteId.StartsWith(contentToken, StringComparison.InvariantCultureIgnoreCase))
                                                           .ToList();
            List<string> clusterIds = routesToRemove.Select(r => r.ClusterId).ToList();
            List<ClusterConfig> clustersToRemove = _proxyConfig.Clusters
                                                               .Where(c => clusterIds.Contains(c.ClusterId, StringComparer.InvariantCultureIgnoreCase))
                                                               .ToList();

            UpdateConfiguration(_proxyConfig.Routes.Except(routesToRemove).ToList(), _proxyConfig.Clusters.Except(clustersToRemove).ToList());
        }

        public void AddNewRoute(string contentToken, string publicUri, string internalUri)
        {
            UriBuilder requestedUri = new UriBuilder(publicUri);

            RouteConfig newPathRoute = new()
            {
                RouteId = contentToken + "-Path",
                ClusterId = contentToken,
                Match = new RouteMatch
                {
                    Path = $"/{contentToken}/{{**actual-path}}",
                },
                AuthorizationPolicy = default, // TODO Look into how to use this effectively
                Order = 1,
                Metadata = new Dictionary<string, string>
                       {
                           { "ContentToken", contentToken },
                       },
            };

            // Add a built in transform to strip out the path prefix indicating the container token
            newPathRoute = newPathRoute.WithTransformPathRemovePrefix($"/{contentToken}");

            RouteConfig newRefererRoute = new()
            {
                RouteId = contentToken + "-Referer",
                ClusterId = contentToken,
                Match = new RouteMatch
                {
                    Path = $"/{{**actual-path}}",
                    Headers = new[]
                    {
                        new RouteHeader()
                        {
                            Name = "referer",
                            Values = new[] { contentToken },
                            Mode = HeaderMatchMode.Contains
                        }
                    }
                },
                AuthorizationPolicy = default, // TODO Look into how to use this effectively
                Order = 2,
                Metadata = new Dictionary<string, string>
                       {
                           { "ContentToken", contentToken },
                       },
            };

            ClusterConfig newCluster = new ClusterConfig
            {
                ClusterId = contentToken,
                Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                        { {
                                "destination1",
                                new DestinationConfig
                                {
                                    Address = internalUri,
                                }
                        } },
                Metadata = new Dictionary<string, string>
                       {
                           { "ContentToken", contentToken },
                       },
            };

            AddNewConfigs(new[] { newPathRoute, newRefererRoute }, newCluster);
        }

        private void AddNewConfigs(IEnumerable<RouteConfig> routes, ClusterConfig cluster)
        {
            List<RouteConfig> newRoutes = _proxyConfig.Routes.ToList();
            foreach (var route in routes)
            {
                newRoutes.RemoveAll(r => r.RouteId.Equals(route.RouteId));
                newRoutes.Add(route);
            }

            List<ClusterConfig> newClusters = _proxyConfig.Clusters.ToList();
            if (cluster is not null)
            {
                // The new cluster could be different (e.g. destination IP address) from an existing ClusterConfig with the same id
                newClusters.RemoveAll(c => c.ClusterId.Equals(cluster.ClusterId));
                newClusters.Add(cluster);
            }

            UpdateConfiguration(newRoutes, newClusters);
        }

        public void RemoveAllConfigForContentToken(string contentToken)
        {
            List<RouteConfig> newRoutes = _proxyConfig.Routes.Where(rc => !rc.RouteId.Contains(contentToken)).ToList();
            List<ClusterConfig> newClusters = _proxyConfig.Clusters.Where(cc => !cc.ClusterId.Contains(contentToken)).ToList();

            UpdateConfiguration(newRoutes, newClusters);

            GlobalFunctions.ContainerLastActivity.Remove(contentToken, out _);
        }
    }
}
