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
    }
}
