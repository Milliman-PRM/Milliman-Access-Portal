using System.Collections.Generic;
using Yarp.ReverseProxy.Transforms;

namespace ContainerReverseProxy.Transforms
{
    public class RedirectTransform : ResponseTransform
    {
        private List<Uri> _targetUris { get; init; }
        
        public RedirectTransform(List<Uri> targetUris)
        {
            _targetUris = targetUris;
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
                //throw new ArgumentException();
            }

            // Handle a response with redirect to the internal host
            if (context.HttpContext.Response.StatusCode >= 300 && context.HttpContext.Response.StatusCode < 400)
            {
                if (context.ProxyResponse.Headers.Location is not null &&
                    context.ProxyResponse.Headers.Location.IsAbsoluteUri &&
                    _targetUris.Any(u => u.Host == context.ProxyResponse.Headers.Location.Host &&
                                         u.Port == context.ProxyResponse.Headers.Location.Port))
                {
                    UriBuilder newLocationUri = new UriBuilder(context.HttpContext.Response.Headers.Location);
                    newLocationUri.Host = context.HttpContext.Request.Host.Host;
                    if (context.HttpContext.Request.Host.Port > 0)
                    {
                        newLocationUri.Port = context.HttpContext.Request.Host.Port ?? -1;
                    }

                    context.HttpContext.Response.Headers.Location = newLocationUri.Uri.AbsoluteUri;
                }
            }

            return ValueTask.CompletedTask;
        }
    }
}
