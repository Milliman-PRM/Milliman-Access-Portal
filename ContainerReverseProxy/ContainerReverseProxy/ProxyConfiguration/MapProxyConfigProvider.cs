using System.Diagnostics;
using Yarp.ReverseProxy.Configuration;

namespace ContainerReverseProxy.ProxyConfiguration
{
    public class MapProxyConfigProvider : IProxyConfigProvider
    {
        private volatile MapProxyConfiguration _config;

        public IProxyConfig GetConfig() => _config;

        public MapProxyConfigProvider(IReadOnlyList<RouteConfig>? routes, IReadOnlyList<ClusterConfig>? clusters)
        {
            _config = new MapProxyConfiguration(routes, clusters);
        }

        public void UpdateConfiguration(IReadOnlyList<RouteConfig>? routes, IReadOnlyList<ClusterConfig>? clusters)
        {
            MapProxyConfiguration? oldConfig = _config;
            _config = new MapProxyConfiguration(routes, clusters);
            oldConfig?.SignalChange();
        }

        public void OpenNewSession(RouteConfig route, ClusterConfig? cluster)
        {
            List<RouteConfig> newRoutes = _config.Routes.ToList();

            if (!newRoutes.Any(r => r.RouteId.Equals(route.RouteId, StringComparison.OrdinalIgnoreCase)))
            {
                newRoutes.Add(route);
            }
            else
            {
                Debug.WriteLine($"From ProxyConfigProvider.OpenSession, trying to open a session with already existing route id {route.RouteId}");
            }

            List<ClusterConfig> newClusters = _config.Clusters.ToList();

            if (cluster is null &&
                !_config.Clusters.Any(c => c.ClusterId.Equals(route.ClusterId, StringComparison.OrdinalIgnoreCase)))
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
            List<RouteConfig> newRoutes = _config.Routes.Except(new[] { route }).ToList();
            List<ClusterConfig> newClusters = cluster is null
                ? _config.Clusters.ToList()
                : _config.Clusters.Except(new[] { cluster }).ToList();

            IEnumerable<ClusterConfig> unreferencedClusters = newClusters.Where(c => c.ClusterId == route.ClusterId);
            newClusters = newClusters.Except(unreferencedClusters).ToList();

            UpdateConfiguration(newRoutes, newClusters);
        }
    }
}
