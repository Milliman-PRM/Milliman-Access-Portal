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

            context.HttpContext.Request.Path = PathString.Empty;
            context.Path = context.Path.Value?.Replace(ExternalPathRoot, "/");
            context.HttpContext.Request.Query = new QueryCollection(context.HttpContext.Request.Query.Where(q => !q.Key.Equals("contentToken")).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

            Log.Information("Request: {@Method} {@Scheme}://{Host}{Path}{Query} (after request transform)",
                                      context.HttpContext.Request.Method,
                                      context.HttpContext.Request.Scheme,
                                      context.HttpContext.Request.Host,
                                      context.HttpContext.Request.Path,
                                      context.HttpContext.Request.QueryString);

            return ValueTask.CompletedTask;
        }
    }
}
