/*
 * OBJECTIVE: Log high level timing for an action
 * DEVELOPER NOTES: For an action, OnAction... methods and both called before either OnResult... methods.
 */

using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System.Diagnostics;

namespace MapCommonLib.ActionFilters
{
    public class LogTimingAttribute : ActionFilterAttribute
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        // time the action method
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _stopwatch.Reset();
            _stopwatch.Start();
            Log.Information("Timing action method for {action}", context.HttpContext.Request.Path);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            _stopwatch.Stop();
            Log.Information("Action method took {elapsed}ms {action}",
                _stopwatch.Elapsed.TotalMilliseconds,
                context.HttpContext.Request.Path);
        }

        // time the action result
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            _stopwatch.Reset();
            _stopwatch.Start();
            Log.Information("Timing action result for {action}", context.HttpContext.Request.Path);
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            _stopwatch.Stop();
            Log.Information("Action result took {elapsed}ms {action}",
                _stopwatch.Elapsed.TotalMilliseconds,
                context.HttpContext.Request.Path);
        }
    }
}
