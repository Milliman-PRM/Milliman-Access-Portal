using ContainerizedAppLib.ProxySupport;
using ContainerReverseProxy.ProxyConfiguration;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text.Json;
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

            connection = new HubConnectionBuilder().WithAutomaticReconnect(new SignalRHubRetryForeverPolicy(TimeSpan.FromSeconds(20)))
                                                   .WithUrl(AppConfiguration.GetValue<string>("MapHubUrl"))  // "https://localhost:7099/abc" in Development environment
                                                   .Build();

            var url = AppConfiguration.GetValue<string>("MapHubUrl");

            connection.On<OpenSessionRequest>("NewSessionAuthorized", request =>
            {
                Debug.WriteLine($"Proxy opening new session, argument is {JsonSerializer.Serialize(request)}");
                UriBuilder requesterUri = new UriBuilder(request.RequestingHost);
                UriBuilder requestedUri = new UriBuilder(request.PublicUri);

                RouteConfig newRoute = new()
                {
                    RouteId = Guid.NewGuid().ToString(),
                    ClusterId = Guid.NewGuid().ToString(),
                    Match = new RouteMatch
                    {
                        // TODO: Do something more specific to the session being opened
                        //Path = "{**catch-all}",
                        Path = requestedUri.Path,
                        Hosts = new List<string> { requestedUri.Host },
                        Headers = new List<RouteHeader> { new RouteHeader { Name = "Host", Values = new List<string> { requesterUri.Host } } },
                        Methods = default,
                        QueryParameters = new List<RouteQueryParameter> 
                        { 
                            new RouteQueryParameter { Name = "contentToken", Values = new List<string> { request.Token } }
                        },
                    },
                    AuthorizationPolicy = default, // TODO Look into how to use this effectively
                    
                };

                ClusterConfig newCluster = new()
                {
                    ClusterId = newRoute.ClusterId,
                    Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "destination1", new DestinationConfig() { Address = request.InternalUri } }
                    }
                };
                MapProxyConfigProvider.OpenNewSession(newRoute, newCluster);

                connection.SendAsync("ProxyConfigurationReport", connection.ConnectionId, MapProxyConfigProvider.GetConfig());
            });

            Task task = InitializeAsync();
        }


        public async Task InitializeAsync()
        {
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
