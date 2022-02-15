using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace ContainerReverseProxy.ProxyConfiguration
{
    public class MapProxyConfiguration : IProxyConfig
    {
        private readonly CancellationTokenSource _cts = new();

        public MapProxyConfiguration(IReadOnlyList<RouteConfig>? routes, IReadOnlyList<ClusterConfig>? clusters)
        {
            Routes = routes ?? new List<RouteConfig>();
            Clusters = clusters ?? new List<ClusterConfig>();
            ChangeToken = new CancellationChangeToken(_cts.Token);
        }

        public IReadOnlyList<RouteConfig> Routes { get; private init; }

        public IReadOnlyList<ClusterConfig> Clusters { get; private init; }

        public IChangeToken ChangeToken { get; }

        internal void SignalChange()
        {
            _cts.Cancel();
        }
    }
}
