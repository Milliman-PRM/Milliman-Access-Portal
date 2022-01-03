using ContainerizedAppLib.ProxySupport;
using ContainerReverseProxy.ProxyConfiguration;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Serilog;
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

            string hubUrl = AppConfiguration.GetValue<string>("MapHubUrl");  // "https://localhost:44336/contentsessionhub" in Development environment
            connection = new HubConnectionBuilder().WithAutomaticReconnect(new SignalRHubRetryForeverPolicy(TimeSpan.FromSeconds(10)))
                                                   .WithUrl(hubUrl)
                                                   .Build();

            connection.On<OpenSessionRequest>("NewSessionAuthorized", async request =>
            {
                Log.Information($"Proxy client has received new session opening message with argument: {Environment.NewLine}{JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented=true })}");
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
                        //Hosts = new List<string> { requestedUri.Host },
                        //Headers = new List<RouteHeader> { new RouteHeader { Name = "Host", Values = new List<string> { request.RequestingHost } } },
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

                var cfg = MapProxyConfigProvider.GetConfig();
                await connection.SendAsync("ProxyConfigurationReport", connection.ConnectionId, cfg);
            });

            connection.On<OpenSessionRequest>("SessionActivity", request =>
            {
                Log.Information($"Proxy client has received new session opening message with argument: {Environment.NewLine}{JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented=true })}");
            });

            Task task = InitializeAsync();
        }

        ~MapHubClient()
        {
            connection.DisposeAsync();
        }


        private async Task InitializeAsync()
        {
            try
            {
                Log.Information("MapHubClient.InitializeAsync starting connection to remote hub");
                await connection.StartAsync();
                Log.Information("MapHubClient.InitializeAsync connected to remote hub");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MapHubClient.InitializeAsync failed connecting to remote hub");
            }

        }
    }
}
