/*
 * CODE OWNERS: Ben Wyatt, Tom Puckett
 * OBJECTIVE: Provide extensions to the base IdentityUser class
 * DEVELOPER NOTES: 
 */

using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using MapDbContextLib.Models;

namespace MapDbContextLib.Identity
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser<long>
    {
        public ApplicationUser() : base()
        {
            PreviousUserPasswords = new List<PreviousPassword>();
        }

        public ApplicationUser(string userName) // : base(userName)
        {
            // The base class does not have a constructor taking an argument.  
            base.UserName = userName;
        }

        /// This overide is here only to apply the explicit [Key] attribute, required in unit tests
        [Key]
        public override long Id { get; set; }

        //
        // Summary:
        //     Gets or sets the user's password reset date
        public virtual DateTime PasswordChangeDate { get; set; }

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
        public string PreviousPasswords { get; set; }

        /// <summary>
        /// Access a list of all password history
        /// </summary>
        [NotMapped]
        public List<PasswordHistory> PasswordHistoryObj
        {
            get
            {
                return JsonConvert.DeserializeObject<List<PasswordHistory>>(PreviousPasswords);
            }
            set
            {
                PreviousPasswords = JsonConvert.SerializeObject(value);
            }
        }
    }
}
