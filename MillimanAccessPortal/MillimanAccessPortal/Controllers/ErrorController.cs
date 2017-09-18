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

        public IActionResult NotAuthorized()
        {
            NotAuthorizedViewModel ErrorModel = new NotAuthorizedViewModel
            {
                Message = TempData["Message"] as string,
                ReturnToController = TempData["ReturnToController"] as string,
                ReturnToAction = TempData["ReturnToAction"] as string,
            };
            TempData.Remove("Message");
            TempData.Remove("ReturnToController");
            TempData.Remove("ReturnToAction");

            return View(ErrorModel);
        }
    }
}