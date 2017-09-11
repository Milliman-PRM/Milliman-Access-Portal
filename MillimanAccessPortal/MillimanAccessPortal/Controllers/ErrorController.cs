using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MillimanAccessPortal.Models.ErrorViewModels;

namespace MillimanAccessPortal.Controllers
{
    public class ErrorController : Controller
    {
        //public IActionResult Index()
        //{
        //    return View();
        //}

        public IActionResult NotAuthorized(string RequestedId, string ReturnToController, string ReturnToAction)
        {
            var x = HttpContext.GetRouteData();
            var y = HttpContext.GetRouteValue("RequestedId");
            NotAuthorizedViewModel Model = new NotAuthorizedViewModel
            {
                Message = $"You are not authorized to view the requested content (#{RequestedId})",
                StackTrace = null,
                ReturnToController = ReturnToController,
                ReturnToAction = ReturnToAction,
            };

            return View(Model);
        }
    }
}