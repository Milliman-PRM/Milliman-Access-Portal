/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Actions that may be invoked from various areas of the application
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MillimanAccessPortal.Controllers
{
    public class SharedController : Controller
    {
        public IActionResult Message(string Msg)
        {
            return View(Msg);
        }

        public IActionResult ContentMessage(List<string> MsgList)
        {
            return View(MsgList);
        }
    }
}