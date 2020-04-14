/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Defines a Mocked version of AuthenticationService to reduced unit test dependency on external resources
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace TestResourcesLib
{
    public class MockAuthenticationService
    {
        public static Mock<AuthenticationService> New(ApplicationDbContext Context)
        {
            IOptions<AuthenticationOptions> options = new OptionsWrapper<AuthenticationOptions>(new AuthenticationOptions { });

            IAuthenticationSchemeProvider schemes = new AuthenticationSchemeProvider(options); // MockAuthenticationSchemeProvider.New(Context, options).Object;
            IAuthenticationHandlerProvider handlers = new AuthenticationHandlerProvider(schemes); // MockAuthenticationHandlerProvider.New(schemes).Object;
            IClaimsTransformation transform = new NoopClaimsTransformation();
            Mock<AuthenticationService> ReturnService = new Mock<AuthenticationService>(schemes, handlers, transform, options);

            // Provide mocked methods required by tests
            ReturnService.Setup(s => s.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>())).ReturnsAsync<HttpContext, string, AuthenticationService, AuthenticateResult>((cxt, scheme) =>
            {
                var ticket = new AuthenticationTicket(new ClaimsPrincipal(), scheme);
                return HandleRequestResult.Success(ticket);
            });
            ReturnService.Setup(s => s.ChallengeAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>())).Returns<HttpContext, string, AuthenticationProperties>((cxt, scheme, props) => Task.CompletedTask);
            ReturnService.Setup(s => s.ForbidAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>())).Returns<HttpContext, string, AuthenticationProperties>((cxt, scheme, props) => Task.CompletedTask);
            ReturnService.Setup(s => s.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>())).Returns<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>((cxt, scheme, cp, props) => Task.CompletedTask);
            ReturnService.Setup(s => s.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>())).Returns<HttpContext, string, AuthenticationProperties>((cxt, scheme, props) => Task.CompletedTask);

            return ReturnService;
        }

    }

    public class MockAuthenticationSchemeProvider
    {
        public static Mock<IAuthenticationSchemeProvider> New(ApplicationDbContext context, IOptions<AuthenticationOptions> options)
        {
            List<Microsoft.AspNetCore.Authentication.AuthenticationScheme> Store = new List<Microsoft.AspNetCore.Authentication.AuthenticationScheme>();

            Mock<IAuthenticationSchemeProvider> ReturnProvider = new Mock<IAuthenticationSchemeProvider>();

            ReturnProvider.Setup(s => s.AddScheme(It.IsAny<Microsoft.AspNetCore.Authentication.AuthenticationScheme>())).Callback<Microsoft.AspNetCore.Authentication.AuthenticationScheme>(scheme => Store.Add(scheme));
            ReturnProvider.Setup(s => s.GetAllSchemesAsync()).ReturnsAsync(Store.AsEnumerable());
            ReturnProvider.Setup(s => s.GetDefaultAuthenticateSchemeAsync()).ReturnsAsync(() => 
            {
                Microsoft.AspNetCore.Authentication.AuthenticationScheme theScheme = Store.SingleOrDefault(s => s.HandlerType == typeof(CookieAuthenticationHandler));
                return theScheme;
            });
            ReturnProvider.Setup(s => s.GetDefaultChallengeSchemeAsync()).ReturnsAsync((Microsoft.AspNetCore.Authentication.AuthenticationScheme)null);
            ReturnProvider.Setup(s => s.GetDefaultForbidSchemeAsync()).ReturnsAsync((Microsoft.AspNetCore.Authentication.AuthenticationScheme)null);
            ReturnProvider.Setup(s => s.GetDefaultSignInSchemeAsync()).ReturnsAsync((Microsoft.AspNetCore.Authentication.AuthenticationScheme)null);
            ReturnProvider.Setup(s => s.GetDefaultSignOutSchemeAsync()).ReturnsAsync((Microsoft.AspNetCore.Authentication.AuthenticationScheme)null);
            ReturnProvider.Setup(s => s.GetRequestHandlerSchemesAsync()).ReturnsAsync(Store);
            ReturnProvider.Setup(s => s.GetSchemeAsync(It.IsAny<string>())).Returns<string>(name => Task.FromResult(Store.SingleOrDefault(s => s.Name == name)));
            ReturnProvider.Setup(s => s.RemoveScheme(It.IsAny<string>())).Callback<string>(name =>
            {
                var itemToRemove = Store.SingleOrDefault(s => s.Name == name);
                Store.Remove(itemToRemove);
            });

            return ReturnProvider;
        }
    }

    /*
    public class MockAuthenticationHandlerProvider
    {
        public static Mock<AuthenticationHandlerProvider> New(IAuthenticationSchemeProvider schemes)
        {
            Mock<AuthenticationHandlerProvider> ReturnProvider = new Mock<AuthenticationHandlerProvider>(schemes);

            return ReturnProvider;
        }

    }
    */

}
