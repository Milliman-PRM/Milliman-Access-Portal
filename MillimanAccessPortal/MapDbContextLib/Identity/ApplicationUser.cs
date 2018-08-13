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


        /// <summary>
        /// Return a list of recent passwords (a specified number of most recent passwords)
        /// </summary>
        /// <param name="daysArg"></param>
        /// <returns></returns>
        public List<PreviousPassword> GetRecentPasswords(int countArg)
        {
            int passwordCount = PasswordHistoryObj.Count;

            if (passwordCount <= countArg)
            {
                return PasswordHistoryObj;
            }

            return PasswordHistoryObj.GetRange(passwordCount - countArg, countArg);
        }

        /// <summary>
        /// Return a list of recent passwords (since a provided date)
        /// </summary>
        /// <param name="timeArg"></param>
        /// <returns></returns>
        public List<PreviousPassword> RecentPasswords(DateTime timeArg)
        {
            return PasswordHistoryObj.FindAll(s => s.dateSet > timeArg);
        }

        /// <summary>
        /// Convenience method to determine if a given string was ever used as a password by the user
        /// </summary>
        /// <param name="passwordArg"></param>
        /// <returns></returns>
        public bool PasswordIsInHistory(string passwordArg)
        {
            return SearchPasswordHistory(PasswordHistoryObj, passwordArg);
        }

        /// <summary>
        /// Search for a match in history (specified number of recent passwords)
        /// </summary>
        /// <param name="passwordArg"></param>
        /// <param name="countArg"></param>
        /// <returns></returns>
        public bool PasswordRecentlyUsed(string passwordArg, int countArg)
        {
            List<PreviousPassword> history = GetRecentPasswords(countArg);

            if (history == null)
            {
                return false;
            }

            return SearchPasswordHistory(history, passwordArg);
        }

        /// <summary>
        /// Search for a match in history (since a given date/time)
        /// </summary>
        /// <param name="passwordArg"></param>
        /// <param name="timeArg"></param>
        /// <returns></returns>
        public bool PasswordRecentlyUsed(string passwordArg, DateTime timeArg)
        {
            List<PreviousPassword> history = RecentPasswords(timeArg);

            if (history == null)
            {
                return false;
            }

            return SearchPasswordHistory(history, passwordArg);
        }

        /// <summary>
        /// Search for a password within a provided history list
        /// </summary>
        /// <param name="historyArg"></param>
        /// <param name="passwordArg"></param>
        /// <returns></returns>
        public bool SearchPasswordHistory(List<PreviousPassword> historyArg, string passwordArg)
        {
            bool matchFound = false;

            // Iterate over history and return if a match is found
            foreach (PreviousPassword history in PasswordHistoryObj)
            {
                matchFound = history.PasswordMatches(passwordArg);

                if (matchFound)
                {
                    return true;
                }
            }

            return matchFound;
        }
    }
}
