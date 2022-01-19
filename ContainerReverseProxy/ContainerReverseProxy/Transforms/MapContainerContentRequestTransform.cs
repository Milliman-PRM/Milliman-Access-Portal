using Serilog;
using Yarp.ReverseProxy.Transforms;

namespace ContainerReverseProxy.Transforms
{
    public class MapContainerContentRequestTransform : RequestTransform
    {
        public string ExternalPathRoot { get; init; }

        public MapContainerContentRequestTransform(IReadOnlyDictionary<string, string> metadata)
        {
            ExternalPathRoot = metadata["ExternalPathRoot"];
        }

        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
            Log.Information("Request: {@Method} {@Scheme}://{Host}{Path}{Query} (before request transform)", 
                                      context.HttpContext.Request.Method, 
                                      context.HttpContext.Request.Scheme, 
                                      context.HttpContext.Request.Host, 
                                      context.HttpContext.Request.Path,
                                      context.HttpContext.Request.QueryString);

            context.Path = context.Path.Value?.Replace(ExternalPathRoot, "/");
            context.Query.Collection.Remove("contentToken");

            return ValueTask.CompletedTask;
        }
    }
}
