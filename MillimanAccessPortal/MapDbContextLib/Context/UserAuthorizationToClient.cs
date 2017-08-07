using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MapDbContextLib.Context
{
    public class UserAuthorizationToClient
    {
        public long Id { get; set; }

        [ForeignKey("Client")]
        public long ClientId { get; set; }
        public Client Client { get; set; }

        [ForeignKey("Role")]
        public long RoleId { get; set; }
        // TODO I would like to convert all Identity tables to use long key type so PK and FK values are easy to follow.  
        // Need to learn how to accomplish proper access to IdentityRole.  
        // Might need to inherit to locally declared class the same way ApplicationUser does.  
        public IdentityRole Role { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
