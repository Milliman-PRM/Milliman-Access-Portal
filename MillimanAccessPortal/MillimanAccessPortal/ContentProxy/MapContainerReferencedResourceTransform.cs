using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Yarp.ReverseProxy.Transforms;

namespace MillimanAccessPortal.ContentProxy
{
    public class MapContainerReferencedResourceTransform : RequestTransform
    {
        private string _reverseProxyPathBaseSegment { get; init; }
        private string _tokenName { get; init; }

        public MapContainerReferencedResourceTransform(string reverseProxyPathBaseSegment, string tokenName)
        {
            _reverseProxyPathBaseSegment = reverseProxyPathBaseSegment;
            _tokenName = tokenName;
        }

        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
            Log.Information("Request: {@Method} {@Scheme}://{Host}{Path}{Query} (entering MapContainerReferencedResourceTransform)",
                                      context.HttpContext.Request.Method,
                                      context.HttpContext.Request.Scheme,
                                      context.HttpContext.Request.Host,
                                      context.HttpContext.Request.Path,
                                      context.HttpContext.Request.QueryString);

            try
            {
                if (context.HttpContext.Request.Path.StartsWithSegments($"/{_tokenName}"))
                {
                    string[] pathElements = context.HttpContext.Request.Path.Value.Split('/');
                    string newPath = string.Join('/', pathElements.Skip(2));
                    context.HttpContext.Request.Path = new Microsoft.AspNetCore.Http.PathString();
                }
                /*
                string? referer = context.HttpContext.Request.Headers.Referer.SingleOrDefault();
                string? origin = context.HttpContext.Request.Headers.Origin.SingleOrDefault();

                if (referer is null && origin is null)
                {
                    Log.Information("No <referer> or <origin> header in request");
                    //return ValueTask.CompletedTask;
                }

#warning need better logic here.  Referer and orginal are sometimes not included at all
                UriBuilder newUri = new UriBuilder(referer ?? origin!);

                //string[] pathSegments = newUri.Uri.Segments;
                //if (pathSegments.Length != 3 ||
                //    !pathSegments[1].Substring(0, pathSegments[1].Length-1).Equals(_reverseProxyPathBaseSegment))
                //{
                //    throw new ApplicationException($"MapContainerReferencedResourceTransform failed processing malformed request path: <{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}{context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}> with referer header <{referer}>");
                //}

                string[] queryParams = newUri.Query.Substring(newUri.Query.IndexOf('?') + 1).Split('&');
                if (!queryParams.Any(q => q.StartsWith("contentToken=", StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new ApplicationException($"MapContainerReferencedResourceTransform failed processing due to query parameter contentToken missing from referer: <{referer}>");
                }

                newUri.Path = context.Path.Value!.StartsWith(newUri.Path)
                    ? context.Path
                    : newUri.Path + context.Path;
                newUri.Path = newUri.Path.Replace("//", "/");

                context.ProxyRequest.RequestUri = newUri.Uri;

                string msg = $"MapContainerReferencedResourceTransform transformed request: <{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}{context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}> to <{context.ProxyRequest.RequestUri}>";
                Log.Information(msg);
                */

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
