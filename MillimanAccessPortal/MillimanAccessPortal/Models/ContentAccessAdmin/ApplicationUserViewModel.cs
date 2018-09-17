/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    /// <summary>
    /// A POCO class representing a MAP user without the sensitive information from Identity
    /// </summary>
    public class ApplicationUserViewModel
    {
        public long Id { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        public string Employer { get; set; }

        public Guid MemberOfClientId { get; set; }

    }
}
