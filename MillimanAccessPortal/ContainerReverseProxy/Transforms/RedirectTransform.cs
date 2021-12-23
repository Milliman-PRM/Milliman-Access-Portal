using System.Net;
using Yarp.ReverseProxy.Transforms;

namespace ContainerReverseProxy.Transforms
{
    public class RedirectTransform : ResponseTransform
    {
        //
        // Summary:
        //     Transforms the given response. The status and headers will have (optionally)
        //     already been copied to the Microsoft.AspNetCore.Http.HttpResponse and any changes
        //     should be made there.
        public override ValueTask ApplyAsync(ResponseTransformContext context)
        {
            if (context.ProxyResponse is null)
            {
                throw new ArgumentException();
            }

            if (context.HttpContext.Response.StatusCode >= 300 && context.HttpContext.Response.StatusCode < 400)
            { // This response is a redirect of some type
                if (context.ProxyResponse.Headers.Location?.Host is not null && 
                    context.ProxyResponse.Headers.Location.Host.Equals(context.HttpContext.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
                {
                    Uri requestUri = new Uri(context.HttpContext.Request.Host.Host);
                    UriBuilder newLocationUri = new UriBuilder(context.HttpContext.Response.Headers.Location);
                    newLocationUri.Host = requestUri.Host;
                    
                    context.HttpContext.Response.Headers.Location = newLocationUri.Uri.AbsoluteUri;
                }
            }

            return ValueTask.CompletedTask;
        }
    }
}
