using Serilog;
using Yarp.ReverseProxy.Transforms;

namespace ContainerReverseProxy.Transforms
{
    public class MapContainerReferencedResourceTransform : RequestTransform
    {
        private string _reverseProxyPathBaseSegment { get; init; }

        public MapContainerReferencedResourceTransform (string reverseProxyPathBaseSegment)
        {
            _reverseProxyPathBaseSegment = reverseProxyPathBaseSegment;
        }

        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
            try
            {
                string referer = context.HttpContext.Request.Headers.Referer.Single();
                UriBuilder newUri = new UriBuilder(referer);

                string[] pathSegments = newUri.Uri.Segments;

                if (pathSegments.Length != 3 ||
                    !pathSegments[1].Substring(0, pathSegments[1].Length-1).Equals(_reverseProxyPathBaseSegment))
                {
                    throw new ApplicationException($"MapContainerReferencedResourceTransform failed processing malformed request path: <{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}{context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}> with referer header <{referer}>");
                }

                string[] queryParams = newUri.Query.Substring(newUri.Query.IndexOf('?') + 1).Split('&');
                if (!queryParams.Any(q => q.StartsWith("contentToken=", StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new ApplicationException($"MapContainerReferencedResourceTransform failed processing due to query parameter contentToken missing from referer: <{referer}>");
                }

                newUri.Path = $"{newUri.Path}{context.Path}".Replace("//", "/");
                context.ProxyRequest.RequestUri = newUri.Uri;

                string msg = $"MapContainerReferencedResourceTransform transformed request: <{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}{context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}> to <{context.ProxyRequest.RequestUri}>";
                Log.Information(msg);

                return ValueTask.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Information(ex.Message);
                return ValueTask.FromException(ex);  // causes 5xx response
            }
        }
    }
}
