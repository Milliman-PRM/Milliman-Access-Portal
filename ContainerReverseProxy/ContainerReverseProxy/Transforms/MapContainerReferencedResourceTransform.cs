using Serilog;
using Yarp.ReverseProxy.Transforms;

namespace ContainerReverseProxy.Transforms
{
    public class MapContainerReferencedResourceTransform : RequestTransform
    {
        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
            try
            {
                string referer = context.HttpContext.Request.Headers.Referer.Single();

                UriBuilder newUri = new UriBuilder(referer);
                newUri.Path = $"{newUri.Path}{context.Path}".Replace("//", "/");

                context.ProxyRequest.RequestUri = newUri.Uri;

                return ValueTask.CompletedTask;
            }
            finally { }
        }
    }
}
