using Microsoft.AspNetCore.Mvc.Filters;

namespace MapCommonLib.ActionFilters
{
    public class PreventAuthRefreshAttribute : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.Items.Add("PreventAuthRefresh", true);
        }
    }
}
