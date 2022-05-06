using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Transforms;

namespace MillimanAccessPortal.ContentProxy
{
    public class MapContainerContentRequestTransform : RequestTransform
    {
        private string _contentToken { get; init; }
        private string _requestingHost { get; init; }
        private string _userIdentityToken { get; init; }

        public MapContainerContentRequestTransform(IReadOnlyDictionary<string, string> metadata)
        {
            _contentToken = metadata["ContentToken"];
            _requestingHost = metadata["RequestingHost"];
            _userIdentityToken = metadata["UserIdentityToken"];
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

            return ValueTask.CompletedTask;
        }
    }
}
