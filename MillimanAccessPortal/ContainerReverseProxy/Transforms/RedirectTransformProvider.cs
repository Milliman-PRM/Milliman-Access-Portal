using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms.Builder;

namespace ContainerReverseProxy.Transforms
{
    public class RedirectTransformProvider : ITransformProvider
    {
        private List<Uri> Hosts = new();

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
            List<DestinationConfig> targetDestinations = (context.Cluster.Destinations?.Select(d => d.Value) ?? new List<DestinationConfig>()).ToList();

            // Add an exception to context.Errors if there is a problem with the cluster
            if (targetDestinations.Count == 0 ||
                targetDestinations.Any(d => string.IsNullOrEmpty(d.Address)))
            {
                context.Errors.Add(new Exception($"Cluster {context.Cluster} is not configured with a host destination"));
                return;
            }

            Hosts = targetDestinations.Select(d => new Uri(d.Address)).ToList();
        }

        /// <summary>
        /// Inspect the given route and conditionally add transforms.
        /// This is called for every route, each time that route is built.
        /// </summary>
        /// <param name="context">The context to add any generated transforms to.</param>
        public void Apply(TransformBuilderContext context)
        {
            context.ResponseTransforms.Add(new RedirectTransform(Hosts));
            context.RequestTransforms.Add(new TestRequestTransform());
        }
    }
}
