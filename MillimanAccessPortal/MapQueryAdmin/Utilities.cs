using Microsoft.Extensions.Configuration;
using Microsoft.Configuration;
using System;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

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
        /// If the database name is set in an environment variable, use that database name instead
        /// </summary>
        /// <param name="username">The username to be used in the connection string</param>
        /// <param name="password">The user's postgresql password</param>
        /// <returns></returns>
        public static string getAppDbConnectionString(string username, string password)
        {
            ConfigurationRoot config = getConfiguration();

            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection"));

            // Set database name if it exists in environment variables
            string dbNameVar = Environment.GetEnvironmentVariable("APP_DATABASE_NAME"); // Returns null if the variable is undefined
            if (!string.IsNullOrWhiteSpace(dbNameVar))
            {
                builder.Database = dbNameVar;
            }

            // Set username & password
            builder.Username = username;
            builder.Password = password;

            return builder.ConnectionString;
        }

        /// <summary>
        /// Gets a copy of the audit log database connection string
        /// If the database name is set in an environment variable, use that name
        /// </summary>
        /// <returns></returns>
        public static string getAuditLogDbConnectionString()
        {
            ConfigurationRoot config = getConfiguration();

            // Set database name if it exists in environment variables
            string dbNameVar = Environment.GetEnvironmentVariable("AUDIT_LOG_DATABASE_NAME"); // Returns null if the variable is undefined
            if (!string.IsNullOrWhiteSpace(dbNameVar))
            {
                NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(config.GetConnectionString("AuditLogConnectionString"));
                builder.Database = dbNameVar;
                return builder.ConnectionString;
            }
            else
            {
                return config.GetConnectionString("AuditLogConnectionString");
            }
        }

        /// <summary>
        /// Builds a configuration object and returns it to the caller
        /// </summary>
        /// <returns></returns>
        public static ConfigurationRoot getConfiguration()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();

            builder.AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{getEnvironmentName()}.json", true)
                .AddJsonFile($"AzureKeyVault.{getEnvironmentName()}.json", true);

            // If a key vault URL was defined, add Azure Key Vault configuration values
            IConfigurationRoot root = builder.Build();
            string vaultUrl = root["AzureVaultName"];
            if (!string.IsNullOrWhiteSpace(vaultUrl))
            {
                var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                var cert = store.Certificates.Find(X509FindType.FindByThumbprint, root["AzureCertificateThumbprint"], false);

                builder.AddAzureKeyVault(
                    root["AzureVaultName"],
                    root["AzureClientID"],
                    cert.OfType<X509Certificate2>().Single()
                    );
            }

            return builder.Build() as ConfigurationRoot;
        }
    }
}
