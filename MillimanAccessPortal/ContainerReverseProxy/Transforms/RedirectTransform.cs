using Serilog;
using Yarp.ReverseProxy.Transforms;

namespace ContainerReverseProxy.Transforms
{
    public class RedirectTransform : ResponseTransform
    {
        private Uri _targetUri { get; init; }
        
        public RedirectTransform(Uri targetUri)
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

            // Temporary
            Log.Information("Response: ProxyResponse.StatusCode {Status}, context.HttpContext.Response.StatusCode {Status}", (int)context.ProxyResponse.StatusCode, context.HttpContext.Response.StatusCode);

            // Handle a response with redirect to the internal host
            if (context.HttpContext.Response.StatusCode >= 300 && context.HttpContext.Response.StatusCode < 400)
            {
                if (context.ProxyResponse.Headers.Location is not null &&
                    context.ProxyResponse.Headers.Location.IsAbsoluteUri &&
                    context.ProxyResponse.Headers.Location.Host == _targetUri.Host &&
                    context.ProxyResponse.Headers.Location.Port == _targetUri.Port)
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
