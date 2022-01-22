/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Main Program class for the MAP reverse proxy application
 * DEVELOPER NOTES: Uses the new streamlined ASP.NET format introduced with .NET 6
 */

using ContainerReverseProxy;
using ContainerReverseProxy.Transforms;
using Prm.SerilogCustomization;
using Serilog;
using System.IO;

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

// This middleware must run before routing so that the route selection process can consider changes made to the request in here
app.Use(async (context, next) => 
{
    string referer = context.Request.Headers.Referer;
    if (string.IsNullOrEmpty(referer))
    {
        Log.Warning($"Empty referer for request {context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}");
    }

    string oldHost = context.Request.Host.Value;
    string newHost = oldHost;

    if (context.Request.Query.ContainsKey("contentToken")
        // || something else
    )
    {
        newHost = context.Request.Query["contentToken"];
        context.Request.Headers.Host = newHost;
    }
    else
    {
        Log.Information("I am here");
    }
    /*else if (context.Request.Headers.Referer.) */

    bool addBaseTag = true;
    if (!addBaseTag)
    {
        await next();
    }
    else
    {
        Stream originalBodyStream = context.Response.Body;

        using (var tempStream = new MemoryStream())
        {
            // Set the response body to a temporary stream so we can read after the chain of middlewares have returned.
            context.Response.Body = tempStream;

            // Run through the chain of subsequent middlewares
            await next();

            // originalContent will contain the existing response body from tempBody.
            tempStream.Seek(0, SeekOrigin.Begin);
            string originalContent = string.Empty;
            using (StreamReader originalContentReader = new StreamReader(tempStream))
            {
                originalContent = originalContentReader.ReadToEnd();
            }

            // Reset the body to the original stream.
            context.Response.Body = originalBodyStream;

            if (context.Response.ContentType.Contains("text/html", StringComparison.InvariantCultureIgnoreCase) &&
                !originalContent.Contains("<base"))

            {
                // Write modified content to the response body.
                UriBuilder baseUri = new UriBuilder(context.Request.Scheme, context.Request.Host.Host, context.Request.Host.Port.HasValue ? context.Request.Host.Port.Value : -1, context.Request.Path);
                string newContent = originalContent.Replace("<head>", $"<head><base href=\"{baseUri.Uri.AbsoluteUri}\">");
                await context.Response.WriteAsync(newContent);
            }
        }
    }
});

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapReverseProxy();
});

//Timer configLogTimer = new Timer(config => Log.Information(JsonSerializer.Serialize(configProvider.GetConfig())), null, dueTime: TimeSpan.Zero, period: TimeSpan.FromSeconds(10));

app.Run();
