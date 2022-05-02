using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Transforms;

namespace MillimanAccessPortal.ContentProxy
{
    public class MapContainerContentRequestTransform : RequestTransform
    {
        private string _externalPathRoot { get; init; }
        private string _contentTokenName { get; init; }

        public MapContainerContentRequestTransform(IReadOnlyDictionary<string, string> metadata)
        {
            _externalPathRoot = metadata["ExternalPathRoot"];
            _contentTokenName = metadata["ContentTokenName"];
        }

        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
            Log.Information("Request: {@Method} {@Scheme}://{Host}{Path}{Query} (entering MapContainerContentRequestTransform)",
                                      context.HttpContext.Request.Method,
                                      context.HttpContext.Request.Scheme,
                                      context.HttpContext.Request.Host,
                                      context.HttpContext.Request.Path,
                                      context.HttpContext.Request.QueryString);

            if (context.HttpContext.Request.Path.StartsWithSegments($"/{_contentTokenName}"))
            {
                string[] pathElements = context.HttpContext.Request.Path.Value.Split('/').Skip(3).ToArray();
                string newPath = $"/{string.Join('/', pathElements)}";
                context.Path = new Microsoft.AspNetCore.Http.PathString(newPath);
            }

            return ValueTask.CompletedTask;
        }
    }
}
