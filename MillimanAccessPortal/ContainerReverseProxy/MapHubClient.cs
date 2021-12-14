using ContainerReverseProxy.ProxyConfiguration;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Yarp.ReverseProxy.Configuration;

namespace ContainerReverseProxy
{
    public class MapHubClient
    {
        private readonly MapProxyConfigProvider MapProxyConfigProvider;
        private readonly IConfiguration AppConfiguration;

        private HubConnection connection;

        public MapHubClient(IProxyConfigProvider proxyConfigProviderArg,
                            IConfiguration configurationArg)
        {
            MapProxyConfigProvider = (MapProxyConfigProvider)proxyConfigProviderArg;
            AppConfiguration = configurationArg;

            Task task = InitializeAsync();
        }


        public async Task InitializeAsync()
        {
            var url = AppConfiguration.GetValue<string>("MapHubUrl");

            connection = new HubConnectionBuilder().WithAutomaticReconnect()
                                                   //.WithUrl("https://localhost:7099/abc")
                                                   .WithUrl(AppConfiguration.GetValue<string>("MapHubUrl"))
                                                   .Build();
            connection.On<Uri, string>("NewSessionAuthorized", (uri, token) =>
            {
                Debug.WriteLine($"Proxy opening new session, uri is {uri.AbsoluteUri}, token is {token}");
                string newRouteId = string.Empty;    // TODO build a more meaningful route id
                string newClusterId = string.Empty;    // TODO build a more meaningful cluster id

                RouteConfig newRoute = new()
                {
                    RouteId = newRouteId,
                    ClusterId = newClusterId,
                    Match = new RouteMatch
                    {
                        // TODO: Do something more specific to the session being opened
                        Path = "{**catch-all}",
                        Hosts = default,
                        Headers = default,
                        Methods = default,
                        QueryParameters = default,
                    },
                    AuthorizationPolicy = default, // TODO Look into how to use this effectively
                };

                ClusterConfig newCluster = new()
                {
                    ClusterId = newClusterId,
                    Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "destination1", new DestinationConfig() { Address = uri.AbsoluteUri } }
                    }
                };
                MapProxyConfigProvider.OpenNewSession(newRoute, newCluster);

                connection.SendAsync("ProxyConfigurationReport", connection.ConnectionId, MapProxyConfigProvider.GetConfig());
            });

            try
            {
                await connection.StartAsync();
                Debug.WriteLine("Connection started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }
    }
}
