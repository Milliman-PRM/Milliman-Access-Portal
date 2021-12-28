using Serilog;
using Yarp.ReverseProxy.Transforms;

namespace ContainerReverseProxy.Transforms
{
    public class TestRequestTransform : RequestTransform
    {
        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
            // Temporary
            Log.Information("Request: {@Method} {@Scheme}://{Host}{Path}", context.HttpContext.Request.Method, context.HttpContext.Request.Scheme, context.HttpContext.Request.Host, context.HttpContext.Request.Path);

            return ValueTask.CompletedTask;
        }
    }
}
