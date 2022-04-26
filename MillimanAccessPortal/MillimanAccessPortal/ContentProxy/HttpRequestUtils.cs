using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MillimanAccessPortal.ContentProxy
{
    public class HttpRequestUtils
    {
        internal static string? GetContentTokenForRequest(HttpRequest request, string baseUrl, string contentTokenName, List<string> configuredTokens)
        {
            string requestHostToken = ContentTokenFromHostString(new HostString(request.Host.Value).Host, baseUrl);  // Only for an environment where there is no application gateway??
            string pathRootToken = configuredTokens.SingleOrDefault(t => request.Path.StartsWithSegments($"/{t}"));
            string queryStringToken = request.Query.ContainsKey(contentTokenName) ? request.Query.Single(q => q.Key.Equals(contentTokenName)).Value.ToString() : null;
            string originHeaderToken = request.Headers.ContainsKey("Origin") ? ContentTokenFromHostString(new Uri(request.Headers["Origin"]).Host, baseUrl) : null;
            string refererHeaderToken = request.Headers.ContainsKey("Referer") ? ContentTokenFromHostString(new Uri(request.Headers["Referer"]).Host, baseUrl) : null;
            string originQueryToken = request.Headers.ContainsKey("Origin") ? ContentTokenFromUriQuery(new Uri(request.Headers["Origin"]), contentTokenName) : null;
            string refererQueryToken = request.Headers.ContainsKey("Referer") ? ContentTokenFromUriQuery(new Uri(request.Headers["Referer"]), contentTokenName) : null;

            string foundToken = requestHostToken ??
                                pathRootToken ??
                                queryStringToken ??
                                originHeaderToken ??
                                refererHeaderToken ??
                                originQueryToken ??
                                refererQueryToken;

            return foundToken;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Full host name with no port number</param>
        /// <returns></returns>
        internal static string? ContentTokenFromHostString(string requestHost, string reverseProxyBaseUrl)
        {
            if (requestHost is null)
            {
                return null;
            }

            Uri baseProxyUri = new Uri(reverseProxyBaseUrl);
            int baseHostNameIndex = requestHost.IndexOf(baseProxyUri.Host);

            if (baseHostNameIndex <= 0)
            {
                return null;
            }
            else
            {
                string contentToken = requestHost.Substring(0, baseHostNameIndex);
                return contentToken;
            }
        }

        internal static string? ContentTokenFromUriQuery(Uri uri, string tokenName)
        {
            if (uri is null)
            {
                return null;
            }

            string[] queryElements = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, StringValues> queryKvps = queryElements.Aggregate(new Dictionary<string, StringValues>(),
                                                                                 (dict, val) =>
                                                                                 {
                                                                                     var kv = val.Split('=');
                                                                                     dict.Add(kv[0], kv[1]);
                                                                                     return dict;
                                                                                 });
            QueryCollection queryCollection = new QueryCollection(queryKvps);
            if (queryCollection.TryGetValue(tokenName, out StringValues v))
            {
                return v;
            }
            else
            {
                return null;
            }
        }
    }
}
