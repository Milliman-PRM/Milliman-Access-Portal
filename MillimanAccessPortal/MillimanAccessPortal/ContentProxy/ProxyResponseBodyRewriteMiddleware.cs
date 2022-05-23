using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;

namespace MillimanAccessPortal.ContentProxy
{
    public class ProxyResponseBodyRewriteMiddleware
    {
        private RequestDelegate _next { get; init; }
        private MapProxyConfigProvider _proxyConfigProvider { get; init; }
        private IConfiguration _appConfig { get; init; }

        public ProxyResponseBodyRewriteMiddleware(RequestDelegate next, IProxyConfigProvider configProviderArg, IConfiguration appConfig)
        {
            _next = next;
            _proxyConfigProvider = (MapProxyConfigProvider)configProviderArg;
            _appConfig = appConfig;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // Only process responses that have been designated by the appropriate RequestTransform
            if (context.Items.ContainsKey("ResponseBodyStream") && context.Items.ContainsKey("ContentToken"))
            {
                string tmpPath = context.Request.Path.Value;

                // originalContent will contain the existing response body from tempBody.
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                using StreamReader originalBodyReader = new StreamReader(context.Response.Body);
                string newBody = await originalBodyReader.ReadToEndAsync();
                originalBodyReader.Close();

                // Reset the response body to its original stream object.
                context.Response.Body = context.Items["ResponseBodyStream"] as Stream;

                // If appropriate, insert a <base> html element
                if (!string.IsNullOrEmpty(context.Response.ContentType) &&
                    context.Response.ContentType.Contains("text/html", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Create modified response body.
                    PathString newBasePath = new PathString($"/{context.Items["ContentToken"]}/");

                    if (newBody.Contains("<base "))
                    {
                        int baseElementStartIndex = newBody.IndexOf("<base ");
                        int baseElementCloseIndex = newBody.IndexOf('>', baseElementStartIndex);
                        string originalBaseElement = newBody.Substring(baseElementStartIndex, baseElementCloseIndex  - baseElementStartIndex + 1);
                        int hrefValueStartIndex = originalBaseElement.IndexOf("href=\"") + "href=\"".Length;
                        int hrefValueEndIndex = originalBaseElement.IndexOf('\"', hrefValueStartIndex) - 1;
                        string hrefValue = originalBaseElement.Substring(hrefValueStartIndex, hrefValueEndIndex - hrefValueStartIndex + 1);

                        hrefValue = hrefValue.TrimStart('~', '/');
                        newBasePath.Add(hrefValue);

                        string newBaseElement = originalBaseElement.Substring(0, hrefValueStartIndex) + newBasePath + originalBaseElement.Substring(hrefValueEndIndex+1);

                        newBody = newBody.Replace(originalBaseElement, newBaseElement);
                    }
                    else
                    {
                        newBody = newBody.Replace("<head>", $"<head>\n  <base href=\"{newBasePath}\" />");
                    }
                }

                // Write the correct body (modified or not) to the response object
                await context.Response.WriteAsync(newBody);
            }
        }
    }

    public static class ProxyResponseBodyRewriteMiddlewareExtensions
    {
        public static IApplicationBuilder UseProxyResponseBodyRewriteMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProxyResponseBodyRewriteMiddleware>();
        }
    }
}
