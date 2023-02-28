using System;
using System.Collections.Generic;
using System.Linq;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms.Builder;

namespace MillimanAccessPortal.ContentProxy
{
    public class MapContainerContentTransformProvider : ITransformProvider
    {
        private Uri _targetUri;

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
            try
            {
                DestinationConfig targetDestination = (context.Cluster.Destinations?.Select(d => d.Value) ?? new List<DestinationConfig>()).Single();
                _targetUri = new Uri(targetDestination.Address);
            }
            catch (Exception ex)
            {
                // Adding to context.Errors prevents the call to Apply() below
                context.Errors.Add(new AggregateException($"Cluster {context.Cluster.ClusterId} is null or is not configured with a single destination host", ex));
                return;
            }
        }

        /// <summary>
        /// Inspect the given route and conditionally add transforms.
        /// This is called for every route, each time that route is built.
        /// </summary>
        /// <param name="context">The context to add any generated transforms to.</param>
        public void Apply(TransformBuilderContext context)
        {
            context.RequestTransforms.Add(new MapContainerContentRequestTransform(context.Cluster));
            context.ResponseTransforms.Add(new MapContainerContentResponseTransform(_targetUri!));
        }
    }
}
