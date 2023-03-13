using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Transforms;

namespace MillimanAccessPortal.ContentProxy
{
    public class MapContainerContentResponseTransform : ResponseTransform
    {
        private Uri _targetUri { get; init; }

        public MapContainerContentResponseTransform(Uri targetUri)
        {
            _targetUri = targetUri;
        }

        //
        // Summary:
        //     Transforms the given response. The status and headers will have (optionally)
        //     already been copied to the Microsoft.AspNetCore.Http.HttpResponse and any changes
        //     should be made there.
        public override ValueTask ApplyAsync(ResponseTransformContext context)
        {
            if (context.ProxyResponse is null)
            {
                return ValueTask.CompletedTask;
            }

            switch (context.HttpContext.Response.StatusCode)
            {
                case (>= 100 and < 200) or 204:
                    // Content-Length header is prohibited by spec for these statuses. R-Shiny non-complies for websocket upgrade response
                    // https://datatracker.ietf.org/doc/html/rfc7230#section-3.3.2
                    if (context.HttpContext.Response.Headers.ContentLength.HasValue)
                    {
                        context.HttpContext.Response.Headers.ContentLength = null;
                    }
                    break;

                case >= 300 and < 400:
                    // A redirect response with absolute location to the internal host/port updates to the external host/port
                    if (context.ProxyResponse.Headers.Location is not null &&
                        context.ProxyResponse.Headers.Location.IsAbsoluteUri &&
                        context.ProxyResponse.Headers.Location.Host == _targetUri.Host &&
                        context.ProxyResponse.Headers.Location.Port == _targetUri.Port)
                    {
                        var beforeTransform = context.HttpContext.Response.Headers["Location"];
                        // Note In .NET 6 there are more useful properties of context.HttpContext.Response.Headers
                            UriBuilder newLocationUri = new UriBuilder(beforeTransform);
                            newLocationUri.Host = context.HttpContext.Request.Host.Host;
                            if (context.HttpContext.Request.Host.Port > 0)
                            {
                                newLocationUri.Port = context.HttpContext.Request.Host.Port ?? -1;
                            }

                        context.HttpContext.Response.Headers["Location"] = newLocationUri.Uri.AbsoluteUri;
                        Log.Information("Redirect response location header transformed from {Before} to {After}", beforeTransform, context.HttpContext.Response.Headers["Location"]);  // Temporary
                    }
                    break;
            }

            return ValueTask.CompletedTask;
        }
    }
}
