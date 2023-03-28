using MapCommonLib;
using Serilog;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace MillimanAccessPortal.ContentProxy
{
    public class MapContainerContentRequestTransform : RequestTransform
    {
        private ClusterConfig _cluster { get; init; }

        public MapContainerContentRequestTransform(ClusterConfig cluster)
        {
            _cluster = cluster;
        }

        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
            context.ProxyRequest.Headers.Add("map-user-id", context.HttpContext.User.Identity.Name);
            GlobalFunctions.ContainerLastActivity.AddOrUpdate(_cluster.Metadata["ContentToken"], DateTime.UtcNow, (_, _) => DateTime.UtcNow);

            Log.Verbose($"Proxy forwarding request {context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}{context.HttpContext.Request.Path} " +
                        $"to {context.DestinationPrefix}{context.Path.Value.TrimStart('/')}, " +
                        $"content token is {_cluster.Metadata["ContentToken"]}, " +
                        $"LastActivity collection is {JsonSerializer.Serialize(GlobalFunctions.ContainerLastActivity)}");

            return ValueTask.CompletedTask;
        }
    }
}
