using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MapCommonLib.ActionFilters
{
    public class LogActionBeforeAfterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!(context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor))
            {
                return;
            }

            var arguments = new Dictionary<string, object>(context.ActionArguments);

            // Consider log-relevant custom parameter attributes
            foreach (var paramInfo in controllerActionDescriptor.MethodInfo.GetParameters())
            {
                foreach (var attr in paramInfo.CustomAttributes)
                {
                    if (attr.AttributeType == typeof(SuppressLogAttribute))
                    {
                        var suppressed = attr.NamedArguments
                            .SingleOrDefault(a => a.MemberName == nameof(SuppressLogAttribute.Properties));

                        if (suppressed.MemberInfo == null)
                        {
                            // suppress the entire object
                            arguments.Remove(paramInfo.Name);
                        }
                        else
                        {
                            // suppress top-level properties provided by SuppressLogAttribute
                            var argument = arguments[paramInfo.Name];
                            var argumentClone = Activator.CreateInstance(argument.GetType());

                            // clone to keep from mutating original argument
                            var cloneProps = argumentClone.GetType().GetProperties();
                            foreach (var cloneProp in cloneProps)
                            {
                                cloneProp.SetValue(argumentClone, cloneProp.GetValue(argument));
                            }

                            var supressedProps = suppressed.TypedValue.Value as string;
                            foreach (var suppressedProp in supressedProps?.Split(',') ?? new string[] { })
                            {
                                var cloneProp = argumentClone.GetType().GetProperty(suppressedProp);
                                if (cloneProp != null)
                                {
                                    // setting the value to null is a simple way to obfuscate the property
                                    cloneProp.SetValue(argumentClone, null);
                                }

                                arguments[paramInfo.Name] = argumentClone;
                            }
                        }
                    }
                }
            }

            Log.Verbose($"Executing action {context.ActionDescriptor.DisplayName} "
                      + "with parameters {@actionArguments}", arguments);
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            Log.Verbose($"Executed result {context.ActionDescriptor.DisplayName} "
                      + $"with status code {context.HttpContext.Response.StatusCode}");
        }
    }

    /// <summary>
    /// Prevent LogActionBeforeAfterAttribute from logging specific parameters or top-level argument properties
    /// </summary>
    public class SuppressLogAttribute : Attribute
    {
        /// <summary>
        /// A comma-separated list of top-level property names to null out when logging the parameter
        /// </summary>
        public string Properties { get; set; } = null;
    }
}
