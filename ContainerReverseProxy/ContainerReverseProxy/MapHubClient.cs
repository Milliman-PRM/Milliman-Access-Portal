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
        private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

        private HubConnection connection;

        public MapHubClient(IProxyConfigProvider proxyConfigProviderArg,
                            IConfiguration configurationArg)
        {
            MapProxyConfigProvider = (MapProxyConfigProvider)proxyConfigProviderArg;
            AppConfiguration = configurationArg;

            string hubUrl = AppConfiguration.GetValue<string>("MapHubUrl");  // "https://localhost:44336/contentsessionhub" in Development environment
            connection = new HubConnectionBuilder().WithAutomaticReconnect(new SignalRHubRetryForeverPolicy(RetryDelay))
                                                   .WithUrl(hubUrl)
                                                   .Build();

            connection.Closed += Connection_Closed;
            connection.Reconnecting += Connection_Reconnecting;
            connection.Reconnected += Connection_Reconnected;

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
                        Path = requestedUri.Path,
                        Methods = default,
                        QueryParameters = new List<RouteQueryParameter>
                        {
                            new RouteQueryParameter { Name = "contentToken", Values = new List<string> { request.ContentToken }, Mode = QueryParameterMatchMode.Exact }
                        },
                        //Headers = new List<RouteHeader> { new RouteHeader { Name = "cookie", Values = new List<string> { $".AspNetCore.Session={request.SessionToken}" }, Mode = HeaderMatchMode.Contains } },
                        //Hosts = new List<string> { requestedUri.Host },
                    },
                    AuthorizationPolicy = default, // TODO Look into how to use this effectively
                    Metadata = new Dictionary<string, string> { { "InternalPath", new Uri(request.InternalUri).AbsolutePath } },
                };

                var cfg = MapProxyConfigProvider.GetConfig();
                if (!cfg.Routes.Any(r => (r.Match.Path is not null && r.Match.Path.Equals(newRoute.Match.Path, StringComparison.InvariantCultureIgnoreCase))
                                      && (r.Match.QueryParameters is not null && r.Match.QueryParameters.ToHashSet().SetEquals(newRoute.Match.QueryParameters.ToHashSet()))
                                      && (r.Match.Headers is not null && newRoute.Match.Headers is not null && r.Match.Headers.ToHashSet().SetEquals(newRoute.Match.Headers.ToHashSet()))))
                {
                    ClusterConfig newCluster = new ClusterConfig
                    {
                        ClusterId = newRoute.ClusterId,
                        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                        { { 
                                "destination1", 
                                new DestinationConfig
                                { 
                                    Address = request.InternalUri, 
                                    Metadata = new Dictionary<string, string> { { "InternalPath", new Uri(request.InternalUri).AbsolutePath } }
                                }
                        } },
                    };
                    MapProxyConfigProvider.OpenNewSession(newRoute, newCluster);
                }

                await connection.SendAsync("ProxyConfigurationReport", connection.ConnectionId, cfg);
            });

            connection.On<OpenSessionRequest>("SessionActivity", request =>
            {
                Log.Information($"Proxy client has received new session opening message with argument: {Environment.NewLine}{JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented=true })}");
            });

            Task task = InitializeAsync();
        }

        #region Connection event handlers
        private Task Connection_Reconnecting(Exception? arg)
        {
            Log.Information($"Reconnecting to SignalR hub: {arg?.Message}");
            return Task.CompletedTask;
        }

        private Task Connection_Reconnected(string? arg)
        {
            Log.Information($"Reconnected to SignalR hub, connection ID {arg}");
            return Task.CompletedTask;
        }

        private Task Connection_Closed(Exception? arg)
        {
            Log.Information($"Connection to SignalR hub was closed: {arg?.Message}");
            return Task.CompletedTask;
        }
        #endregion

        ~MapHubClient()
        {
            connection.DisposeAsync();
        }


        private async Task InitializeAsync()
        {
            Log.Information("MapHubClient.InitializeAsync starting connection to remote hub");

            Task startTask = Task.CompletedTask;
            do
            {
                try
                {
                    startTask = connection.StartAsync();
                    await startTask;
                }
                catch (Exception ex)
                {
                    Log.Error($"MapHubClient failed connecting to SignalR hub at {AppConfiguration.GetValue<string>("MapHubUrl")}:{Environment.NewLine}{ex.Message}");
                    await Task.Delay(RetryDelay);
                }
            }
            while (startTask.IsFaulted);

            Log.Information("MapHubClient.InitializeAsync connected to remote hub");

        }
    }
}
