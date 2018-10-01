using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class PasswordResetSecurityTokenProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
    {
        public PasswordResetSecurityTokenProvider(IDataProtectionProvider dataProtectionProvider, IOptions<PasswordResetSecurityTokenProviderOptions> options) : base(dataProtectionProvider, options)
        {
        }
    }

    public class PasswordResetSecurityTokenProviderOptions : DataProtectionTokenProviderOptions { }

}
