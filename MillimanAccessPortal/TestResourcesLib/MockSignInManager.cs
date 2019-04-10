/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Mocks the SignInManager<ApplicationUser> type for use in MAP testing.
 * DEVELOPER NOTES: Only known needed methods have been mocked, and constructor argument list is incomplete
 *      SignInManager does not implement any Interface so it is necessary to Mock the concrete class. 
 */

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MapDbContextLib.Identity;
using Moq;
using System;
using System.Collections.Generic;

namespace TestResourcesLib
{
    public class MockSignInManager
    {
        public static Mock<SignInManager<ApplicationUser>> New(UserManager<ApplicationUser> userManager)
        {
            var ReturnMockSignInManager = new Mock<SignInManager<ApplicationUser>>(userManager, 
                                                                                   new HttpContextAccessor(), 
                                                                                   new UserClaimsPrincipalFactory<ApplicationUser>(userManager, new OptionsWrapper<IdentityOptions>(new IdentityOptions())), 
                                                                                   null, null, null);

            ReturnMockSignInManager
                .Setup(m => m.ConfigureExternalAuthenticationProperties(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string, string>((p, r, u) => new AuthenticationProperties(new Dictionary<string, string> { { ".redirect", r }, { "LoginProvider", p } }));

            return ReturnMockSignInManager;
        }
    }
}
