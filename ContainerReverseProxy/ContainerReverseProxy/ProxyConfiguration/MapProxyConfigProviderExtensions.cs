/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Extension methods to incorporate MAP's specialized ProxyConfigProvider implementation
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using ContainerReverseProxy;
using ContainerReverseProxy.ProxyConfiguration;
using Yarp.ReverseProxy.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MapProxyConfigProviderExtensions
    {
        public static IReverseProxyBuilder AddMapProxyConfigProvider(this IReverseProxyBuilder builder)
        {
            return AddMapProxyConfigProvider(builder, new List<RouteConfig>(), new List<ClusterConfig>());
        }

        public static IReverseProxyBuilder AddMapProxyConfigProvider(this IReverseProxyBuilder builder, IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            builder.Services.AddSingleton<IProxyConfigProvider>(new MapProxyConfigProvider(routes, clusters));
            return builder;
        }

    }
}
