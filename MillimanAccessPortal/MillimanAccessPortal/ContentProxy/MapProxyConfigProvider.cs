using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Yarp.ReverseProxy.Configuration;

namespace MillimanAccessPortal.ContentProxy
{
    public class MapProxyConfigProvider : IProxyConfigProvider
    {
        // private readonly IConfiguration _appConfiguration;
        private volatile MapProxyConfiguration _proxyConfig = new MapProxyConfiguration();

        // Implementation of the base interface signature
        public IProxyConfig GetConfig() => _proxyConfig;

        // public MapProxyConfigProvider(IConfiguration appConfiguration)
        // {
        //     _appConfiguration = appConfiguration;
        // }

        public void UpdateConfiguration(IReadOnlyList<RouteConfig>? routes, IReadOnlyList<ClusterConfig>? clusters)
        {
            MapProxyConfiguration? oldConfig = _proxyConfig;
            _proxyConfig = new MapProxyConfiguration(routes, clusters);
            oldConfig?.SignalChange();

            Log.Information($"New Configuration:{Environment.NewLine}{JsonSerializer.Serialize(_proxyConfig)}");
        }

        public void OpenNewSession(string requestingHost, string contentToken, string publicUri, string internalUri, string userIdentityToken)
        {
            UriBuilder requestedUri = new UriBuilder(publicUri);

            RouteConfig newRoute = new()
            {
                RouteId = contentToken,
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
                           { "RequestingHost", requestingHost },
                           { "UserIdentityToken", userIdentityToken },
                       },
            };

            if (!_proxyConfig.Routes.Any(r => //(r.Match.Hosts is not null && r.Match.Hosts.ToHashSet().SetEquals(newRoute.Match.Hosts)) &&
                                              // (r.Match.QueryParameters is not null && r.Match.QueryParameters.ToHashSet().SetEquals(newRoute.Match.QueryParameters.ToHashSet())) &&
                                              (r.Match.Headers is not null && newRoute.Match.Headers is not null && r.Match.Headers.ToHashSet().SetEquals(newRoute.Match.Headers.ToHashSet()))
                                              ))
            {
                ClusterConfig newCluster = new ClusterConfig
                {
                    ClusterId = newRoute.ClusterId,
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
                           { "RequestingHost", requestingHost },
                           { "UserIdentityToken", userIdentityToken },
                       },
                };
                OpenNewSession(newRoute, newCluster);

                try
                {
                    var cfg = JsonSerializer.Serialize(GetConfig(), new JsonSerializerOptions { WriteIndented=true });
                    Log.Information($"New proxy configuration is{Environment.NewLine}{cfg}");
                }
                catch (Exception ex)
                {
                    string msg = ex.Message;
                }
            }
        }

        public void OpenNewSession(RouteConfig route, ClusterConfig? cluster)
        {
            List<RouteConfig> newRoutes = _proxyConfig.Routes.ToList();

            if (!newRoutes.Any(r => r.RouteId.Equals(route.RouteId, StringComparison.OrdinalIgnoreCase)))
            {
                newRoutes.Add(route);
            }
            else
            {
                Debug.WriteLine($"From ProxyConfigProvider.OpenSession, trying to open a session with already existing route id {route.RouteId}");
            }

            List<ClusterConfig> newClusters = _proxyConfig.Clusters.ToList();

            if (cluster is null &&
                !_proxyConfig.Clusters.Any(c => c.ClusterId.Equals(route.ClusterId, StringComparison.OrdinalIgnoreCase)))
            { // error
                ;
            }
            if (cluster is not null)
            {
                newClusters.Add(cluster);
            }

            UpdateConfiguration(newRoutes, newClusters);
        }

        public void CloseSession(RouteConfig route, ClusterConfig? cluster)
        {
            // TODO Maybe route and cluster are not the right arguments.  Somehow the caller needs to identify the session to be closed
            List<RouteConfig> newRoutes = _proxyConfig.Routes.Except(new[] { route }).ToList();
            List<ClusterConfig> newClusters = cluster is null
                ? _proxyConfig.Clusters.ToList()
                : _proxyConfig.Clusters.Except(new[] { cluster }).ToList();

            IEnumerable<ClusterConfig> unreferencedClusters = newClusters.Where(c => c.ClusterId == route.ClusterId);
            newClusters = newClusters.Except(unreferencedClusters).ToList();

            UpdateConfiguration(newRoutes, newClusters);
        }
    }
}
