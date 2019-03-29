/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace TestResourcesLib
{
    class MockAuthenticationService
    {
        public static Mock<AuthenticationService> New(Mock<ApplicationDbContext> Context)
        {
            Mock<AuthenticationService> ReturnService = new Mock<AuthenticationService>();

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

    class MockAuthenticationSchemeProvider
    {
        public static Mock<IAuthenticationSchemeProvider> New(ApplicationDbContext context)
        {
            Mock<AuthenticationSchemeProvider> ReturnProvider = new Mock<AuthenticationSchemeProvider>();

            ReturnProvider.Setup(s => s.AddScheme(It.IsAny<Microsoft.AspNetCore.Authentication.AuthenticationScheme>())).Callback<Microsoft.AspNetCore.Authentication.AuthenticationScheme>(scheme => 
            {
                MapDbContextLib.Context.AuthenticationScheme newScheme = new MapDbContextLib.Context.AuthenticationScheme
                {
                    Name = scheme.Name,
                    DisplayName = scheme.DisplayName,
                    Id = Guid.NewGuid(),
                    Type = AuthenticationType.Default,
                    //SchemePropertiesObj = ?,
                    //DomainList = new string[0],
                };
                context.AuthenticationScheme.Add(newScheme);
            });
            ReturnProvider.Setup(s => s.GetAllSchemesAsync()).ReturnsAsync(context.AuthenticationScheme.Select(s => new Microsoft.AspNetCore.Authentication.AuthenticationScheme(s.Name, s.DisplayName, null)).AsEnumerable());
            ReturnProvider.Setup(s => s.GetDefaultAuthenticateSchemeAsync()).ReturnsAsync(new Microsoft.AspNetCore.Authentication.AuthenticationScheme(IdentityConstants.ApplicationScheme, IdentityConstants.ApplicationScheme, typeof(string)));
            ReturnProvider.Setup(s => s.GetDefaultChallengeSchemeAsync()).ReturnsAsync((Microsoft.AspNetCore.Authentication.AuthenticationScheme)null);
            ReturnProvider.Setup(s => s.GetDefaultForbidSchemeAsync()).ReturnsAsync((Microsoft.AspNetCore.Authentication.AuthenticationScheme)null);
            ReturnProvider.Setup(s => s.GetDefaultSignInSchemeAsync()).ReturnsAsync((Microsoft.AspNetCore.Authentication.AuthenticationScheme)null);
            ReturnProvider.Setup(s => s.GetDefaultSignOutSchemeAsync()).ReturnsAsync((Microsoft.AspNetCore.Authentication.AuthenticationScheme)null);
            ReturnProvider.Setup(s => s.GetRequestHandlerSchemesAsync()).ReturnsAsync(context.AuthenticationScheme.Select(s => new Microsoft.AspNetCore.Authentication.AuthenticationScheme(s.Name, s.DisplayName, null)).AsEnumerable());
            ReturnProvider.Setup(s => s.GetSchemeAsync(It.IsAny<string>())).ReturnsAsync<string, AuthenticationSchemeProvider, Microsoft.AspNetCore.Authentication.AuthenticationScheme>(name => 
            {
                var dbScheme = context.AuthenticationScheme.SingleOrDefault(s => s.Name == name);
                return new Microsoft.AspNetCore.Authentication.AuthenticationScheme(dbScheme.Name, dbScheme.DisplayName, null);
            });
            //
            // Summary:
            //     Returns the Microsoft.AspNetCore.Authentication.AuthenticationScheme matching
            //     the name, or null.
            //
            // Parameters:
            //   name:
            //     The name of the authenticationScheme.
            //
            // Returns:
            //     The scheme or null if not found.
            Task<AuthenticationScheme> GetSchemeAsync(string name);
            //
            // Summary:
            //     Removes a scheme, preventing it from being used by Microsoft.AspNetCore.Authentication.IAuthenticationService.
            //
            // Parameters:
            //   name:
            //     The name of the authenticationScheme being removed.
            void RemoveScheme(string name);
        }

    }
}
