using Yarp.ReverseProxy.Transforms.Builder;

namespace ContainerReverseProxy.Transforms
{
    public class RedirectTransformProvider : ITransformProvider
    {
        /// <summary>
        /// Validates any route data needed for transforms.
        /// </summary>
        /// <param name="context">The context to add any generated errors to.</param>
        public void ValidateRoute(TransformRouteValidationContext context)
        {
            // Add an exception to context.Errors if there is a problem
        }

        /// <summary>
        /// Validates any cluster data needed for transforms.
        /// </summary>
        /// <param name="context">The context to add any generated errors to.</param>
        public void ValidateCluster(TransformClusterValidationContext context)
        {
            // Add an exception to context.Errors if there is a problem
        }

        /// <summary>
        /// Inspect the given route and conditionally add transforms.
        /// This is called for every route, each time that route is built.
        /// </summary>
        /// <param name="context">The context to add any generated transforms to.</param>
        public void Apply(TransformBuilderContext context)
        {
            context.ResponseTransforms.Add(new RedirectTransform());
        }
    }
}
