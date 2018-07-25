/*
 * CODE OWNERS: Joseph Sweeny
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

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
