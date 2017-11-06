using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MapTests
{
    /// <summary>
    /// Methods to support common test initialization tasks
    /// </summary>
    internal class TestInitialization
    {
        /// <summary>
        /// Initializes a ControllerContext as needed to construct a functioning controller. 
        /// </summary>
        /// <param name="UserName">The user name to be passed to the controller</param>
        /// <returns></returns>
        internal static ControllerContext GenerateControllerContext(string UserName)
        {
            ClaimsPrincipal TestUserPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, UserName) }));

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext() { User = TestUserPrincipal }
            };
        }

    }
}
