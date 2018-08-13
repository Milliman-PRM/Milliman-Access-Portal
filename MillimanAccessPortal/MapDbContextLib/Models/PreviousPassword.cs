/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Represents a single instance of a prior password used by a user
 *              Stored and accessed as a list on ApplicationUser objects
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using CryptoHelper;

namespace MapDbContextLib.Models
{
    public class PreviousPassword
    {

        public string algorithmUsed { get; set; }

        public string hash { get; set; }

        public DateTime dateSet {get; set;}

        /// <summary>
        /// When a password is provided to the constructor, 
        /// automatically build the other properties
        /// </summary>
        /// <param name="passwordArg"></param>
        public PreviousPassword(string passwordArg)
        {
            dateSet = DateTime.UtcNow;

            hash = Crypto.HashPassword(passwordArg);
        }

        public bool PasswordMatches(string passwordArg)
        {
            return Crypto.VerifyHashedPassword(hash, passwordArg);
        }
    }
}
