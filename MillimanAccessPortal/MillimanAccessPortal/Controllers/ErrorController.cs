using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MillimanAccessPortal.Models.ErrorViewModels;

namespace MillimanAccessPortal.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult NotAuthorized()
        {
            return View(CreateGeneralErrorViewModel());
        }

        public IActionResult Error()
        {
            return View(CreateGeneralErrorViewModel());
        }

        [NonAction]
        private GeneralErrorViewModel CreateGeneralErrorViewModel()
        {
            return new GeneralErrorViewModel
            {
                Message = (TempData["Message"] as string).Split(new string[] { "\r\n", "<br>" }, System.StringSplitOptions.RemoveEmptyEntries),
                ReturnToController = TempData["ReturnToController"] as string,
                ReturnToAction = TempData["ReturnToAction"] as string,
            };
        }
    }
}