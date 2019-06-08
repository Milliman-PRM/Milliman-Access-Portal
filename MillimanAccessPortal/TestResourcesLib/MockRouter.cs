/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Adds mocked action routing functionality to a mocked controller to enable unit test of actions that rely on router features
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestResourcesLib
{
    public class MockRouter
    {
        public static void AddToController(ControllerBase controller, Dictionary<string, string> actionRouteValues)
        {
            var actionContext = new ActionContext()
            {
                HttpContext = controller.HttpContext,
                RouteData = new RouteData(),
            };

            RouteValueDictionary valueDictionary = new RouteValueDictionary(actionRouteValues);
            Mock<IRouter> mockRouter = new Mock<IRouter>();
            mockRouter.Setup(m => m.GetVirtualPath(It.IsAny<VirtualPathContext>())).Returns<VirtualPathContext>((r) =>
            {
                UriBuilder urlHelper = new UriBuilder
                {
                    Path = $"/{r.Values["controller"]}/{r.Values["action"]}",
                    Query = string.Join("&", r.Values.Keys.Except(new List<string> {"controller", "action" }).Select(k => $"{k}={r.Values[k]}")),
                };
                return new VirtualPathData(mockRouter.Object, urlHelper.Uri.PathAndQuery);
            });
            controller.Url = new UrlHelper(actionContext);
            controller.Url.ActionContext.RouteData.PushState(mockRouter.Object, valueDictionary, null);
        }
    }
}
