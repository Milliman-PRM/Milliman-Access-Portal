using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace MapCommonLib.ActionFilters
{
    public class LogVerboseAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            Log.Verbose($"Executing action {context.ActionDescriptor.DisplayName} "
                      + "with parameters {actionArguments}", context.ActionArguments);
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            Log.Verbose($"Executed result {context.ActionDescriptor.DisplayName} "
                      + $"with status code {context.HttpContext.Response.StatusCode}");
        }
    }
}
