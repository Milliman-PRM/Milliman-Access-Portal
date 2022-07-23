using Microsoft.AspNetCore.Mvc.Filters;
using System;
using Serilog;
using System.Text;

namespace MapCommonLib.ActionFilters
{
    /// <summary>
    /// <see cref="https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-5.0#exception-filters"/>
    /// </summary>
    public class LogAnyUnhandledExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            Log.Error(context.Exception, $"Uncaught exception while processing a controller action");
        }
    }
}
