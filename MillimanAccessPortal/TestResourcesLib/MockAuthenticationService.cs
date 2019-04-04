/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Defines a Mocked version of AuthenticationService to reduced unit test dependency on external resources
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using Microsoft.AspNetCore.Authentication;
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
            IAuthenticationSchemeProvider schemes = MockAuthenticationSchemeProvider.New(Context).Object;
            Mock<AuthenticationService> ReturnService = new Mock<AuthenticationService>(schemes, null, null);

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
        public static Mock<AuthenticationSchemeProvider> New(ApplicationDbContext context)
        {
            IOptions<AuthenticationOptions> options = new OptionsWrapper<AuthenticationOptions>(new AuthenticationOptions { });
            Mock<AuthenticationSchemeProvider> ReturnProvider = new Mock<AuthenticationSchemeProvider>(options);

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
            ReturnProvider.Setup(s => s.GetAllSchemesAsync()).ReturnsAsync(context.AuthenticationScheme.Select(s => new Microsoft.AspNetCore.Authentication.AuthenticationScheme(s.Name, s.DisplayName, typeof(Microsoft.AspNetCore.Authentication.IAuthenticationHandler))).AsEnumerable());
            ReturnProvider.Setup(s => s.GetDefaultAuthenticateSchemeAsync()).ReturnsAsync(() => 
            {
                var theScheme = context.AuthenticationScheme.SingleOrDefault(s => s.Name == IdentityConstants.ApplicationScheme);
                return new Microsoft.AspNetCore.Authentication.AuthenticationScheme(theScheme.Name, theScheme.DisplayName, typeof(Microsoft.AspNetCore.Authentication.IAuthenticationHandler));
            });
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
            ReturnProvider.Setup(s => s.RemoveScheme(It.IsAny<string>())).Callback<string>(name =>
            {
                var dbScheme = context.AuthenticationScheme.SingleOrDefault(s => s.Name == name);
                context.AuthenticationScheme.Remove(dbScheme);
            });

            return ReturnProvider;
        }

    }
}
