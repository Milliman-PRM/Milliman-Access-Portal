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
        public string hash { get; set; } = null;

        public DateTime dateSet { get; set; }

        /// <summary>
        /// Parameterless constructor, required due to json deserialization to this type in ApplicationUser entity class
        /// </summary>
        public PreviousPassword()
        {}

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="passwordArg"></param>
        public PreviousPassword(string passwordArg)
        {
            Set(passwordArg);
        }

        /// <summary>
        /// Full population of instance properties
        /// </summary>
        /// <param name="passwordArg"></param>
        public void Set(string passwordArg)
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
