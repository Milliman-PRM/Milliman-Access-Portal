using ContainerReverseProxy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
                        .Initialize(true);
                        //.LoadFromMapConfig(builder.Configuration.GetSection("ReverseProxy"));
// or maybe builder.Services.AddHttpForwarder...?

builder.Services.AddSingleton<MapHubClient>();

var app = builder.Build();

app.MapReverseProxy();

MapHubClient hubClient = app.Services.GetRequiredService<MapHubClient>();
await hubClient.InitializeAsync();

app.Run();
