/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An MVC controller to handle and present error condition(s)
 * DEVELOPER NOTES: General pattern is to pass error details in TempData and build a model as expected by the appropriate error view
 */
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MillimanAccessPortal.Models.ErrorViewModels;

namespace MillimanAccessPortal.Controllers
{
    public class ErrorController : Controller
    {
        /// <summary>
        /// Presents an error page indicating an authorization failure
        /// </summary>
        /// <returns></returns>
        public IActionResult NotAuthorized()
        {
            return View(CreateGeneralErrorViewModel());
        }

        /// <summary>
        /// A general error handler for use when a more specific view is not called for
        /// </summary>
        /// <returns></returns>
        public IActionResult Error()
        {
            return View(CreateGeneralErrorViewModel());
        }

        /// <summary>
        /// Populates a general model for the error views from TempData. If the TempData variables are not found, default route is "HostedContent/Index"
        /// </summary>
        /// <param name="MessageVarNameArg">Name of a TempData variable containing error message, default "Message"</param>
        /// <param name="ReturnToControllerArg">Name of a TempData variable containing the controller to return to, default "HostedContent"</param>
        /// <param name="ReturnToActionArg">Name of a TempData variable containing the action to return to, default "Index"</param>
        /// <returns></returns>
        [NonAction]
        private GeneralErrorViewModel CreateGeneralErrorViewModel(string MessageVarNameArg="Message", string ReturnToControllerArg = "ReturnToController", string ReturnToActionArg = "ReturnToAction")
        {
            // Defaults here are in case the TempData variables don't exist
            string[] LocalMessage = new string[] { "Error message not found" } ;
            string LocalReturnToController = "HostedContent";
            string LocalReturnToAction = "Index";

            if (TempData.Keys.Contains(MessageVarNameArg))
            {
                LocalMessage = (TempData[MessageVarNameArg] as string).Split(new string[] { Environment.NewLine, "<br>" }, System.StringSplitOptions.RemoveEmptyEntries);
                TempData.Remove(MessageVarNameArg);
            }

            if (TempData.Keys.Contains(ReturnToControllerArg))
            {
                LocalReturnToController = TempData[ReturnToControllerArg] as string;
                TempData.Remove(ReturnToControllerArg);
            }

            if (TempData.Keys.Contains(ReturnToActionArg))
            {
                LocalReturnToAction = TempData[ReturnToActionArg] as string;
                TempData.Remove(ReturnToActionArg);
            }

            return new GeneralErrorViewModel
            {
                Message = LocalMessage,
                ReturnToController = LocalReturnToController,
                ReturnToAction = LocalReturnToAction,
            };
        }
    }
}