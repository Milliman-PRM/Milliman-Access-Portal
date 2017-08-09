/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Enable granting application users a role for a client
 * DEVELOPER NOTES: 
 */

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
        public ApplicationRole Role { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
