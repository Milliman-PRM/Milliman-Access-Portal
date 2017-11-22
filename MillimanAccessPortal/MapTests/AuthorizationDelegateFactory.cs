using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MapTests
{
    public static class AuthorizationDelegateFactory
    {
        public static Func<ClaimsPrincipal, object, IAuthorizationRequirement, Task<AuthorizationResult>> AuthorizeAsync =
            (c, o, r) =>
            {
                return AuthorizationServiceExtensions.AuthorizeAsync(null, null, "");
            };

    }
}
