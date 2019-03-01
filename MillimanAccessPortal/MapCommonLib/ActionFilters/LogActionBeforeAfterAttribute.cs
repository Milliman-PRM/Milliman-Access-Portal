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

            var methodAttributes = controllerActionDescriptor.MethodInfo.CustomAttributes;
            var actionParameters = controllerActionDescriptor.MethodInfo.GetParameters();

            // Log parameters for GET requests
            if (methodAttributes.Select(a => a.AttributeType).Contains(typeof(HttpGetAttribute)))
            {
                var logObject = new Dictionary<string, object>();

                foreach (var paramInfo in actionParameters)
                {
                    var argument = context.ActionArguments.Single(a => a.Key == paramInfo.Name);
                    if (paramInfo.CustomAttributes.Select(a => a.AttributeType).Contains(
                        typeof(EmitBeforeAfterLogAttribute)))
                    {
                        // add parameter to the log object
                        logObject.Add(argument.Key, argument.Value);
                    }
                    else
                    {
                        logObject.Add(argument.Key, Activator.CreateInstance(argument.Value.GetType()));
                    }
                }

                Log.Verbose($"Executing action {context.ActionDescriptor.DisplayName} "
                           + "with arguments {@logObject}", logObject);
            }
            // Log JSON body top-level properties for POST requests
            else if (methodAttributes.Select(a => a.AttributeType).Contains(typeof(HttpPostAttribute)))
            {
                if (actionParameters.Length != 1)
                {
                    return;
                }

                var paramInfo = actionParameters.Single();
                var singleArgument = context.ActionArguments.Single().Value;

                // suppress top-level properties provided by SuppressLogAttribute
                var logObject = Activator.CreateInstance(singleArgument.GetType());

                foreach (var prop in paramInfo.ParameterType.GetProperties())
                {
                    if (prop.CustomAttributes.Select(a => a.AttributeType).Contains(
                        typeof(EmitBeforeAfterLogAttribute)))
                    {
                        // add property to the log object
                        prop.SetValue(logObject, prop.GetValue(singleArgument));
                    }
                }

                Log.Verbose($"Executing action {context.ActionDescriptor.DisplayName} "
                           + "with request object {@logObject}", logObject);
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
