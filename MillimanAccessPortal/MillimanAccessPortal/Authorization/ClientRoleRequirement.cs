using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Authorization;

namespace MillimanAccessPortal.Authorization
{
    public class ClientRoleRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Unset or &lt;= 0 to require test for authorization to any client.
        /// </summary>
        public long ClientId { get; set; } = -1;
        public RoleEnum RoleEnum { get; set; }
    }
}
