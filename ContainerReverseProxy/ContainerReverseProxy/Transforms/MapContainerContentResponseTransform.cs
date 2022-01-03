using Serilog;
using Yarp.ReverseProxy.Transforms;

namespace ContainerReverseProxy.Transforms
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
                //throw new ArgumentException();
            }

            // A redirect response with absolute location to the internal host/port updates to the external host/port
            if (context.HttpContext.Response.StatusCode >= 300 && context.HttpContext.Response.StatusCode < 400)
            {
                if (context.ProxyResponse.Headers.Location is not null &&
                    context.ProxyResponse.Headers.Location.IsAbsoluteUri &&
                    context.ProxyResponse.Headers.Location.Host == _targetUri.Host &&
                    context.ProxyResponse.Headers.Location.Port == _targetUri.Port)
                {
                    string beforeTransform = context.HttpContext.Response.Headers.Location;  // Temporary
                    UriBuilder newLocationUri = new UriBuilder(context.HttpContext.Response.Headers.Location);
                    newLocationUri.Host = context.HttpContext.Request.Host.Host;
                    if (context.HttpContext.Request.Host.Port > 0)
                    {
                        newLocationUri.Port = context.HttpContext.Request.Host.Port ?? -1;
                    }

                    context.HttpContext.Response.Headers.Location = newLocationUri.Uri.AbsoluteUri;
                    Log.Information("Redirect response location header transformed from {Before} to {After}", beforeTransform, context.HttpContext.Response.Headers.Location);  // Temporary
                }
            }

            return ValueTask.CompletedTask;
        }
    }
}
