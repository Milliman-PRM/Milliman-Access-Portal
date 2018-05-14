using Microsoft.AspNetCore.Mvc.Filters;

namespace MapCommonLib.ActionFilters
{
    public class DisableAuthRefresh : ResultFilterAttribute
    {
        public override void OnResultExecuted(ResultExecutedContext context)
        {
            // FIXME: get cookie name from configuration
            context.HttpContext.Response.Cookies.Delete(".AspNetCore.Identity.Application");
        }
    }
}
