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
Log.Information("Reverse Proxy launched");

builder.Services.AddReverseProxy()
                        .Initialize(true)  // TODO true here is only for development purposes
                        .AddTransforms<MapContainerContentTransformProvider>()
                        ;
//.LoadFromMapConfig(builder.Configuration.GetSection("ReverseProxy"));
// or maybe builder.Services.AddHttpForwarder...?

builder.Services.AddSingleton<MapHubClient>();

var app = builder.Build();

app.MapReverseProxy();

MapHubClient hubClient = app.Services.GetRequiredService<MapHubClient>();
await hubClient.InitializeAsync();

app.Run();
