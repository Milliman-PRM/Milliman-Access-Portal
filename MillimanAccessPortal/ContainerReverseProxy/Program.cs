using ContainerReverseProxy;
using ContainerReverseProxy.Transforms;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
                        .Initialize(true)  // TODO true here is only for development purposes
                        .AddTransforms<RedirectTransformProvider>()
                        ;
//.LoadFromMapConfig(builder.Configuration.GetSection("ReverseProxy"));
// or maybe builder.Services.AddHttpForwarder...?

builder.Services.AddSingleton<MapHubClient>();

var app = builder.Build();

app.MapReverseProxy();

MapHubClient hubClient = app.Services.GetRequiredService<MapHubClient>();
await hubClient.InitializeAsync();

app.Run();
