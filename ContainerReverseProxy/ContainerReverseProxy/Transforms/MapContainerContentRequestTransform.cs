using Serilog;
using Yarp.ReverseProxy.Transforms;

namespace ContainerReverseProxy.Transforms
{
    public class MapContainerContentRequestTransform : RequestTransform
    {
        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
            // Temporary
            Log.Information("Request: {@Method} {@Scheme}://{Host}{Path}{Query}", 
                                      context.HttpContext.Request.Method, 
                                      context.HttpContext.Request.Scheme, 
                                      context.HttpContext.Request.Host, 
                                      context.HttpContext.Request.Path,
                                      context.HttpContext.Request.QueryString);

            context.HttpContext.Request.Query = new QueryCollection(context.HttpContext.Request.Query.Where(q => !q.Key.Equals("contentToken")).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

            Log.Information($"After removing contentToken query:{Environment.NewLine}" +
                            "Request: {@Method} {@Scheme}://{Host}{Path}{Query}",
                                      context.HttpContext.Request.Method,
                                      context.HttpContext.Request.Scheme,
                                      context.HttpContext.Request.Host,
                                      context.HttpContext.Request.Path,
                                      context.HttpContext.Request.QueryString);

            return ValueTask.CompletedTask;
        }
    }
}
