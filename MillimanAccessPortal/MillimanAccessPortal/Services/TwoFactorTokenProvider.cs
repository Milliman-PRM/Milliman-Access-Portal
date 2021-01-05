/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A token provider intended to provide tokens for 2FA authentication using separate configuration from the default Identity provider
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MillimanAccessPortal.Services
{
    public class TwoFactorTokenProvider<TUser> : EmailTokenProvider<TUser> where TUser : class {}

    public class TwoFactorTokenProviderOptions : DataProtectionTokenProviderOptions { }

}
