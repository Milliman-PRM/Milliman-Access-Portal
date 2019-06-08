/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Actions that may be invoked from various areas of the application
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace MillimanAccessPortal.Controllers
{
    public class SharedController : Controller
    {
        [AllowAnonymous]
        public IActionResult Message(string Msg)
        {
            return View("Message", Msg);
        }

        [AllowAnonymous]
        public IActionResult ContentMessage(List<string> MsgList)
        {
            return View("ContentMessage", MsgList);
        }
    }
}