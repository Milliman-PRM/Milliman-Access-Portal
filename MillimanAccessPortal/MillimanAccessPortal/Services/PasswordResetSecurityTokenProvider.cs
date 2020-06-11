/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A token provider intended to provide tokens using separate configuration from the default Identity provider
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MillimanAccessPortal.Services
{
    public class PasswordResetSecurityTokenProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
    {
        public PasswordResetSecurityTokenProvider(IDataProtectionProvider dataProtectionProvider, IOptions<PasswordResetSecurityTokenProviderOptions> options, ILogger<PasswordResetSecurityTokenProvider<TUser>> logger) : base(dataProtectionProvider, options, logger)
        {
        }
    }

    public class PasswordResetSecurityTokenProviderOptions : DataProtectionTokenProviderOptions { }

}
