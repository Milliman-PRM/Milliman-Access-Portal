using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapCommonLib.ActionFilters
{
    /// <summary>
    /// This filter should only be used with actions that accept JSON data bound to an object
    /// </summary>
    public class LogActionBeforeAfterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!(context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor))
            {
                return;
            }

            var actionParameters = controllerActionDescriptor.MethodInfo.GetParameters();

            // Log parameters for GET requests
            if (context.HttpContext.Request.Method == "GET")
            {
                var logObject = new
                {
                    Arguments = new Dictionary<string, object>(),
                    Suppressed = new List<string>(),
                };

                foreach (var paramInfo in actionParameters)
                {
                    var argument = context.ActionArguments.Single(a => a.Key == paramInfo.Name);
                    if (paramInfo.CustomAttributes.Select(a => a.AttributeType).Contains(
                        typeof(EmitBeforeAfterLogAttribute)))
                    {
                        // add parameter to the log object
                        logObject.Arguments.Add(argument.Key, argument.Value);
                    }
                    else
                    {
                        logObject.Suppressed.Add(argument.Key);
                    }
                }

                Log.Verbose($"Executing action {context.ActionDescriptor.DisplayName} "
                           + "with arguments {@logObject}"
                           + (logObject.Suppressed.Any()
                             ? ", suppressed arguments {@suppressed}"
                             : ""), logObject.Arguments, logObject.Suppressed);
            }
            // Log JSON body top-level properties for POST requests
            else if (context.HttpContext.Request.Method == "POST" && actionParameters.Length == 1)
            {
                var paramInfo = actionParameters.Single();
                var singleArgument = context.ActionArguments.Single().Value;

                var logObject = new
                {
                    Properties = new Dictionary<string, object>(),
                    Suppressed = new List<string>(),
                };

                foreach (var prop in paramInfo.ParameterType.GetProperties())
                {
                    if (prop.CustomAttributes.Select(a => a.AttributeType).Contains(
                        typeof(EmitBeforeAfterLogAttribute)))
                    {
                        // add property to the log object
                        logObject.Properties.Add(prop.Name, prop.GetValue(singleArgument));
                    }
                    else
                    {
                        logObject.Suppressed.Add(prop.Name);
                    }
                }

                Log.Verbose($"Executing action {context.ActionDescriptor.DisplayName} "
                           + "with request object {@logObject}"
                           + (logObject.Suppressed.Any()
                             ? ", suppressed properties {@suppressed}"
                             : ""), logObject.Properties, logObject.Suppressed);
            }
            else
            {
                Log.Verbose($"Executing action {context.ActionDescriptor.DisplayName}");
            }
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            Log.Verbose($"Executed result {context.ActionDescriptor.DisplayName} "
                      + $"with status code {context.HttpContext.Response.StatusCode}");
        }
    }

    /// <summary>
    /// Instruct LogActionBeforeAfterAttribute to log specific properties in a request or response model.
    ///
    /// This attribute should be applied to top-level properties of objects that are used as the single request
    /// or response value for an action that has the LogBeforeAfter attribute.
    /// </summary>
    public class EmitBeforeAfterLogAttribute : Attribute { }
}
