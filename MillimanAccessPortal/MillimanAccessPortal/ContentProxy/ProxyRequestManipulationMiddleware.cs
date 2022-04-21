using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;

namespace MillimanAccessPortal.ContentProxy
{
    public class ProxyRequestManipulationMiddleware
    {
        private RequestDelegate _next { get; init; }
        private MapProxyConfigProvider _proxyConfigProvider { get; init; }
        private IConfiguration _appConfig { get; init; }

        public ProxyRequestManipulationMiddleware(RequestDelegate next, IProxyConfigProvider configProviderArg, IConfiguration appConfig)
        {
            _next = next;
            _proxyConfigProvider = (MapProxyConfigProvider)configProviderArg;
            _appConfig = appConfig;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string contentTokenName = _appConfig.GetValue<string>("ReverseProxyContentTokenHeaderName");

            // if (this request is NOT for a content container)
            if (!context.Request.Query.ContainsKey(contentTokenName)) // TODO a better check may be required (e.g. for Shiny websocket request)
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.ContainsKey(contentTokenName))
            {
                IProxyConfig? proxyCfg = _proxyConfigProvider.GetConfig();
                List<string> allConfiguredTokens = proxyCfg.Routes.Aggregate(new List<string>(),
                                                                             (list, val) =>
                                                                             {
                                                                                 if (val.Metadata!.TryGetValue(contentTokenName, out string? token))
                                                                                 {
                                                                                     list.Add(token);
                                                                                 }
                                                                                 return list;
                                                                             });

                string reverseProxyBaseUrl = _appConfig.GetValue<string>("ReverseProxyBaseUrl");
                string? foundToken = HttpRequestUtils.GetContentTokenForRequest(context.Request, reverseProxyBaseUrl, contentTokenName, allConfiguredTokens);

                // If there is a route that is likely to match:
                if (foundToken is not null && proxyCfg.Routes.Any(r => r.Match.Headers?.Any(h => h.Name == contentTokenName) ?? false))
                {
                    context.Request.Headers.Add(contentTokenName, foundToken);
                    string removeFromPath = $"/{foundToken}";
                    if (context.Request.Path.StartsWithSegments(removeFromPath, out PathString remaining))
                    {
                        context.Request.Path = remaining;
                    }
                }
            }

            if (context.WebSockets.IsWebSocketRequest ||
                (context.Request.Path.HasValue && context.Request.Path.Value.Contains("/websocket")))
            {
                await _next(context);
                return;
            }

            // bool addBaseTag = true;

            using (var tempStream = new MemoryStream())
            {
                // Set the response body to a temporary MemoryStream so we can read from it after other middlewares have returned.
                Stream originalBodyStream = context.Response.Body;
                context.Response.Body = tempStream;

                // Run through the chain of subsequent middlewares
                await _next(context);

                // originalContent will contain the existing response body from tempBody.
                tempStream.Seek(0, SeekOrigin.Begin);
                string newBody = string.Empty;
                using (StreamReader originalContentReader = new StreamReader(tempStream))
                {
                    newBody = originalContentReader.ReadToEnd();
                }

                // Reset the response body to its original stream object.
                context.Response.Body = originalBodyStream;

                if (!string.IsNullOrEmpty(context.Response.ContentType) &&
                    context.Response.ContentType.Contains("text/html", StringComparison.InvariantCultureIgnoreCase) &&
                    !newBody.Contains("<base "))
                {
                    string contentTokenPath = $"/{(context.Request.Headers.ContainsKey("content-token") ? context.Request.Headers["content-token"] : "")}";
                    string basePath = context.Request.Path.StartsWithSegments(contentTokenPath) ? context.Request.Path : contentTokenPath + context.Request.Path;

                    // Write modified content to the response body.
                    UriBuilder baseUri = new UriBuilder(context.Request.Scheme, context.Request.Host.Host, context.Request.Host.Port.HasValue ? context.Request.Host.Port.Value : -1, basePath);
                    newBody = newBody.Replace("<head>", $"<head>\n  <base href=\"{baseUri.Uri.AbsoluteUri}\" />");
                }

                await context.Response.WriteAsync(newBody);
            }
        }
    }

    public static class ProxyRequestManipulationMiddlewareExtensions
    {
        public static IApplicationBuilder UseProxyRequestManipulationMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProxyRequestManipulationMiddleware>();
        }
    }
}
