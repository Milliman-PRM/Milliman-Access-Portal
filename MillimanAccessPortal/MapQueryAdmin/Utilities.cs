using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapQueryAdmin
{
    /// <summary>
    /// A catch-all class for helper functions that don't really belong anywhere else
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Helper function to get the current environment name. 
        /// You may need to set ASPNETCORE_ENVIRONMENT manually for yourself for this to work in local debug mode.
        /// </summary>
        /// <returns>The name of the current environment</returns>
        public static string getEnvironmentName()
        {
            string envName;

            envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.IsNullOrWhiteSpace(envName))
            {
                envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", EnvironmentVariableTarget.User);
            }

            // Return a default value if we haven't found anything yet
            if (string.IsNullOrWhiteSpace(envName))
            {
                envName = "Development";
            }

            return envName;
        }

        /// <summary>
        /// Returns the application database's connection string, with the username and password replaced with the credentials provided by the user.
        /// </summary>
        /// <param name="username">The username to be used in the connection string</param>
        /// <param name="password">The user's postgresql password</param>
        /// <returns></returns>
        public static string getAppDbConnectionString(string username, string password)
        {
            throw new NotImplementedException();
        }
    }
}
