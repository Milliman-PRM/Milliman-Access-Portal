using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Authorization;

namespace MillimanAccessPortal.Authorization
{
    public class RoleRequirement : IAuthorizationRequirement
    {
        public RoleEnum RoleEnum { get; set; }
    }
}
