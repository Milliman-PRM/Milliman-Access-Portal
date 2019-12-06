/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Actions that may be invoked from various areas of the application
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MillimanAccessPortal.Models.SharedModels;
using System.Collections.Generic;

namespace MillimanAccessPortal.Controllers
{
    public class SharedController : Controller
    {
        [AllowAnonymous]
        public IActionResult UserMessage(string Msg)
        {
            return View("UserMessage", new UserMessageModel(Msg));
        }
    }
}