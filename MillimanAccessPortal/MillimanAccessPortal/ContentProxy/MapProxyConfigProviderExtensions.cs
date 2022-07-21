using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace MillimanAccessPortal.ContentProxy
{
    public static class MapProxyConfigProviderExtensions
    {
        public static IReverseProxyBuilder AddMapProxyConfigProvider(this IReverseProxyBuilder builder)
        {
            builder.Services.AddSingleton<IProxyConfigProvider, MapProxyConfigProvider>();
            return builder;
        }
    }
}
