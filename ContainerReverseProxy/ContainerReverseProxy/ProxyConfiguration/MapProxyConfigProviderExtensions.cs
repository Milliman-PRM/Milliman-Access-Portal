using ContainerReverseProxy;
using ContainerReverseProxy.ProxyConfiguration;
using Yarp.ReverseProxy.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MapProxyConfigProviderExtensions
    {
        public static IReverseProxyBuilder AddMapProxyConfigProvider(this IReverseProxyBuilder builder, bool addSampleData = false)
        {
            return AddMapProxyConfigProvider(builder, new List<RouteConfig>(), new List<ClusterConfig>(), addSampleData);
        }

        public static IReverseProxyBuilder AddMapProxyConfigProvider(this IReverseProxyBuilder builder, IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters, bool addSampleData = false)
        {
            if (addSampleData)
            {
                routes = routes.Append(new RouteConfig 
                { 
                    RouteId = "SampleRoute", 
                    ClusterId = "SampleCluster", 
                    Match = new RouteMatch
                    {
                        Path = "{**catch-all}"
                    } 
                }).ToList();

                clusters = clusters.Append(new ClusterConfig 
                    { 
                        ClusterId = "SampleCluster" ,
                        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                            {
                                // { "destination1", new DestinationConfig() { Address = "https://example.com" } },
                                { "destination1", new DestinationConfig() { Address = "https://localhost:44336",  } },
                            }
                }).ToList();
            }

            builder.Services.AddSingleton<IProxyConfigProvider>(new MapProxyConfigProvider(routes, clusters));
            return builder;
        }

    }
}
