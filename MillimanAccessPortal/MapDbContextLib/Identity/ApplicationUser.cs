/*
 * CODE OWNERS: Ben Wyatt, Tom Puckett
 * OBJECTIVE: Provide extensions to the base IdentityUser class
 * DEVELOPER NOTES: 
 */

using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using MapDbContextLib.Models;

namespace MapDbContextLib.Identity
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser<Guid>
    {
        public ApplicationUser() : base()
        {}

        public ApplicationUser(string userName) : base(userName)
        {}

        /// <summary>
        /// This must be declared to support mocking (All foreign keys are expected to reference a property with [KeyAttribute], which the base class does not have). 
        /// </summary>
        [Key]
        public override Guid Id { get; set; }

        //
        // Summary:
        //     Gets or sets the user's most recent password change date
        public virtual DateTime LastPasswordChangeDateTimeUtc { get; set; }

        //
        // Summary:
        //     Gets or sets the user's LastName.
        public virtual string LastName { get; set; }

        //
        // Summary:
        //     Gets or sets the user's FirstName.
        public virtual string FirstName { get; set; }

        //
        // Summary:
        //     Gets or sets the user's Employer.
        public virtual string Employer { get; set; }

        /// <summary>
        /// Store a history of previously-used passwords
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string PreviousPasswords { get; set; } = "[]";

        /// <summary>
        /// Access a list of all password history
        /// </summary>
        [NotMapped]
        public List<PreviousPassword> PasswordHistoryObj
        {
            get
            {
                return string.IsNullOrWhiteSpace(PreviousPasswords) ? new List<PreviousPassword>() : JsonConvert.DeserializeObject<List<PreviousPassword>>(PreviousPasswords);
            }
            set
            {
                PreviousPasswords = JsonConvert.SerializeObject(value == null ? new List<PreviousPassword>() : value);
            }
        }

        //
        // Summary:
        //     Gets or sets the user's IsSuspended status.
        public virtual bool IsSuspended { get; set; }
    }
}
