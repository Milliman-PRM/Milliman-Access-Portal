using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MapDbContextLib.Identity
{
    public class ApplicationRole : IdentityRole<long>
    {
        public ApplicationRole() : base() { }

        public ApplicationRole(string RoleName) : base(RoleName) { }
    }
}
