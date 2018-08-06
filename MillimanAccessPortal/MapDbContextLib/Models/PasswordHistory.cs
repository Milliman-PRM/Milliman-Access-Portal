using System;
using System.Collections.Generic;
using System.Text;

namespace MapDbContextLib.Models
{
    public class PasswordHistory
    {

        public string uniqueSalt { get; set; }

        public string algorithmUsed { get; set; }

        public string hash { get; set; }

        public DateTimeOffset dateSet {get; set;}

        /// <summary>
        /// When a password is provided to the constructor, 
        /// automatically build the other properties
        /// </summary>
        /// <param name="passwordArg"></param>
        public PasswordHistory(string passwordArg)
        {
            dateSet = DateTimeOffset.UtcNow;

            uniqueSalt = generateSalt();

            hash = calculateHash(uniqueSalt, passwordArg);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string generateSalt()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string calculateHash (string saltArg, string passwordArg)
        {
            throw new NotImplementedException();
        }
    }
}
