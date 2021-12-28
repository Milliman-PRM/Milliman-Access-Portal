using Yarp.ReverseProxy.Transforms;

namespace ContainerReverseProxy.Transforms
{
    public class TestRequestTransform : RequestTransform
    {
        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
            return ValueTask.CompletedTask;
        }
    }
}
