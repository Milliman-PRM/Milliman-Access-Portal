using ContainerReverseProxy;
using ContainerReverseProxy.Transforms;
using Prm.SerilogCustomization;
using Serilog;
using Serilog.Settings.Configuration;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.With<UtcTimestampEnricher>()
    .CreateLogger();
Log.Information("ContainerReverseProxy logger started");

builder.Services.AddReverseProxy()
                .AddMapProxyConfigProvider(false)  // TODO true here is only for development purposes
                .AddTransforms<MapContainerContentTransformProvider>();
//.LoadFromMapConfig(builder.Configuration.GetSection("ReverseProxy"));
// or maybe builder.Services.AddHttpForwarder...?

builder.Services.AddSingleton<MapHubClient>();

var app = builder.Build();

// Run the MapHubClient constructor now to make the SignalR connection
app.Services.GetRequiredService<MapHubClient>();  

app.MapReverseProxy();

app.Run();
