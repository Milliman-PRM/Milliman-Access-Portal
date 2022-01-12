/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Main Program class for the MAP reverse proxy application
 * DEVELOPER NOTES: Uses the new streamlined ASP.NET format introduced with .NET 6
 */

using ContainerReverseProxy;
using ContainerReverseProxy.Transforms;
using Prm.SerilogCustomization;
using Serilog;
using System.Text.Json;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.With<UtcTimestampEnricher>()
    .CreateLogger();
builder.Host.UseSerilog();
Log.Information("ContainerReverseProxy logger started");

builder.Services.AddReverseProxy()
                .AddMapProxyConfigProvider()
                .AddTransforms<MapContainerContentTransformProvider>();
//.LoadFromMapConfig(builder.Configuration.GetSection("ReverseProxy"));
// or maybe builder.Services.AddHttpForwarder...?

builder.Services.AddSingleton<MapHubClient>();

var app = builder.Build();

// Run the MapHubClient constructor now to start the SignalR connection
app.Services.GetRequiredService<MapHubClient>();

app.MapReverseProxy();

Yarp.ReverseProxy.Configuration.IProxyConfigProvider? configProvider = app.Services.GetRequiredService<Yarp.ReverseProxy.Configuration.IProxyConfigProvider>();
configProvider.GetConfig();

Timer configLogTimer = new Timer(config => Log.Information(JsonSerializer.Serialize(configProvider.GetConfig())), null, dueTime: TimeSpan.Zero, period: TimeSpan.FromSeconds(10));

app.Run();
