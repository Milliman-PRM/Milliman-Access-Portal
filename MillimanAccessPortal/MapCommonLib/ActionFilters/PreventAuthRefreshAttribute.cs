/*
 * CODE OWNERS: Joseph Sweeny
 * OBJECTIVE: A method attribute that prevents an action invocation from refreshing the user session timeout. 
 * DEVELOPER NOTES: Intended for actions that are called without user activity such as periodic status calls.
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
