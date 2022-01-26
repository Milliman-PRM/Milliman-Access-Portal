/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Main Program class for the MAP reverse proxy application
 * DEVELOPER NOTES: Uses the new streamlined ASP.NET format introduced with .NET 6
 */

using ContainerReverseProxy;
using ContainerReverseProxy.Transforms;
using Prm.SerilogCustomization;
using Serilog;

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
builder.Services.AddSingleton<MapHubClient>();

var app = builder.Build();

app.Services.GetRequiredService<MapHubClient>();  // This runs the MapHubClient constructor to open the SignalR connection

app.UseProxyRequestManipulationMiddleware();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapReverseProxy();
});

app.Run();
