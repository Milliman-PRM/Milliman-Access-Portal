/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Log diagnostic information about an action request
 * DEVELOPER NOTES: 
 */

using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System;
using System.Linq;

namespace MapCommonLib.ActionFilters
{
    /// <summary>
    /// The Order parameter (of the base class) can optionally be set e.g. [ActionDiagnostics(Order = 3)]
    /// </summary>
    public class ActionDiagnosticsAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionArguments.ContainsKey("Order"))
            {
                Order = int.Parse(context.ActionArguments["Order"].ToString());
            }

            Log.Information($"Action diagnostic logging with order {Order}: Action {context.ActionDescriptor.DisplayName}, verb {context.HttpContext.Request.Method}{Environment.NewLine}"
                + $"\tCookies: {string.Join("", context.HttpContext.Request.Cookies.Select(c => $"{Environment.NewLine}\t\t" + c.Key + " : " + c.Value))}{Environment.NewLine}"
                + $"\tForm data: {string.Join("", context.HttpContext.Request.Form.Select(f => $"{Environment.NewLine}\t\t" + f.Key + " : " + f.Value))}{Environment.NewLine}"
                + $"\tHeaders: {string.Join("", context.HttpContext.Request.Headers.Select(h => $"{Environment.NewLine}\t\t" + h.Key + " : " + h.Value))}{Environment.NewLine}"
                + $"\tUser: {{@User}}{Environment.NewLine}"
                , context.HttpContext.User.Identity
                );
        }
    }
}
