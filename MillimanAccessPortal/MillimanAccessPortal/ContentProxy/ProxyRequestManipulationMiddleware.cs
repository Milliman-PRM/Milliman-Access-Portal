using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Serilog;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;

namespace MillimanAccessPortal.ContentProxy
{
    /// <summary>
    /// This middleware identifies and manipulates requests that should be routed to the reverse proxy functionality of the YARP library instead of a MAP controller
    /// </summary>
    public class ProxyRequestManipulationMiddleware
    {
        private RequestDelegate _next { get; init; }
        private MapProxyConfigProvider _proxyConfigProvider { get; init; }
        private string _contentTokenName { get; init; }
        private string _reverseProxyBaseUrl { get; init; }


        public ProxyRequestManipulationMiddleware(RequestDelegate next, IProxyConfigProvider proxyConfigProviderArg, IConfiguration appConfig)
        {
            _next = next;
            _proxyConfigProvider = (MapProxyConfigProvider)proxyConfigProviderArg;
            _contentTokenName = appConfig.GetValue<string>("ReverseProxyContentTokenHeaderName");
            _reverseProxyBaseUrl = appConfig.GetValue<string>("ReverseProxyBaseUrl");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // This middleware ensures that each container content request will match the appropriate configured RouteConfig object

            // if (request is the original redirect to the container)
            if (context.Request.Query.TryGetValue(_contentTokenName, out StringValues contentToken))
            {
                Log.Information($"Proxy middleware detected request with content token query element {context.Request.Path.Value}{context.Request.QueryString}");

                FormatRequestForRouteMatching(context.Request, contentToken[0]);
                // _next is called below
            }
            // if (request is for linked from the original container page as indicated by the "referrer" header)
            else if (context.Request.Headers.TryGetValue("Referer", out StringValues refererValues) && 
                     refererValues.Any(s => s.Contains($"{_contentTokenName}=", StringComparison.InvariantCultureIgnoreCase)))
            {
                string referer = refererValues.Single(s => s.Contains($"{_contentTokenName}=", StringComparison.InvariantCultureIgnoreCase));
                string refererQuery = new Uri(referer).Query;
                string tokenQueryParam = refererQuery.Split('?', '&').SingleOrDefault(q => q.StartsWith($"{_contentTokenName}="));
                contentToken = tokenQueryParam.Split('=')[1];

                FormatRequestForRouteMatching(context.Request, contentToken);
                // _next is called below
            }
            else if (context.Request.Path.StartsWithSegments($"/{_contentTokenName}", out PathString remainingPath))
            {
                var x = remainingPath.StartsWithSegments("/", out PathString matchedSegment, out PathString remainPath);
                Log.Information($"Proxy middleware detected request with container path elements, remaining path is {remainingPath}");
                // FormatRequestForRouteMatching(context.Request);
                await _next(context);
                return;
            }
            else if (context.WebSockets.IsWebSocketRequest ||
                    (context.Request.Path.HasValue && context.Request.Path.Value.Contains("/websocket")))
            {
                // Detect/handle Shiny websocket (if needed)
                ;
            }
            else
            {
                // This is a MAP request, not intended for the container proxy. Do nothing.
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
                    // context.Request.Path.StartsWithSegments($"/{_contentTokenName}/{contentToken}") &&
                    !newBody.Contains("<base "))
                {
                    string basePath = $"/{_contentTokenName}/{contentToken}/"; ;

                    // Write modified content to the response body.
                    UriBuilder baseUri = new UriBuilder(context.Request.Scheme, context.Request.Host.Host, context.Request.Host.Port.HasValue ? context.Request.Host.Port.Value : -1, basePath);
                    newBody = newBody.Replace("<head>", $"<head>\n  <base href=\"{baseUri.Uri.AbsoluteUri}\" />");
                }

                await context.Response.WriteAsync(newBody);
            }
        }

        private void FormatRequestForRouteMatching(HttpRequest request, string contentToken)
        {
            bool tokenExistsInQuery = request.Query.TryGetValue(_contentTokenName, out _);

            if (tokenExistsInQuery)
            {
                Dictionary<string, StringValues> queryWithoutContentToken = request.Query.Where(q => q.Key != _contentTokenName).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                request.Query = new QueryCollection(queryWithoutContentToken);
            }

            if (!request.Path.StartsWithSegments($"/{_contentTokenName}"))
            {
                PathString newPathString = $"/{_contentTokenName}/{contentToken}";
                request.Path = newPathString;
            }

            if (!request.Path.StartsWithSegments($"/{_contentTokenName}"))
            {
                throw new ApplicationException("Failed to format the request path for container proxy route matching");
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
