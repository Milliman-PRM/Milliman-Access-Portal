using MapCommonLib;
using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace MillimanAccessPortal.ContentProxy
{
    public class MapContainerContentRequestTransform : RequestTransform
    {
        private ClusterConfig _cluster { get; init; }

        public MapContainerContentRequestTransform(ClusterConfig cluster)
        {
            _cluster = cluster;
        }

        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
#warning how is this request authorized? 
            //if (!context.HttpContext.Request.Cookies.ContainsKey(".AspNetCore.Identity.Application") ||
            //    context.HttpContext.Request.Cookies[".AspNetCore.Identity.Application"] != _userIdentityToken ||
            //    context.HttpContext.Connection.RemoteIpAddress.ToString() != _requestingHost)
            //{
            //    string shortMsg = $"Failed to authorize the container request."
            //        + $"{Environment.NewLine}  received IP: {context.HttpContext.Connection.RemoteIpAddress}"
            //        + $"{Environment.NewLine}  expected IP: {_requestingHost}";
            //    string longMsg = shortMsg
            //        + $"{Environment.NewLine}  received identity: {context.HttpContext.Request.Cookies[".AspNetCore.Identity.Application"]}"
            //        + $"{Environment.NewLine}  expected identity: {_userIdentityToken}";

            //    Log.Information(longMsg);
            //    throw new ApplicationException(shortMsg);
            //}

            Log.Information($"Request for URI {context.HttpContext.Request.Host}{context.HttpContext.Request.Path}");
            GlobalFunctions.ContainerLastActivity[_cluster.ClusterId] = DateTime.UtcNow;

            return ValueTask.CompletedTask;
        }
    }
}
