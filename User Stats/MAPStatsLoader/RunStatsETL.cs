/*
* CODE OWNERS: Ben Wyatt
* OBJECTIVE: Execute the User Stats ETL script using Azure Functions
* DEVELOPER NOTES: 
*/
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.Logging;
using System.IO;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;

namespace MAP.UserStats
{
    public static class RunStatsETL
    {
        
        [FunctionName("RunStatsETL")]
        public static void Run([TimerTrigger("0 0 * * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"RunStatsETL executed at: {DateTime.Now} UTC");
            NpgsqlLogManager.Provider = new ConsoleLoggingProvider(NpgsqlLogLevel.Trace);
            
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                #if DEBUG // Variable set automatically by build configuration (Debug vs. Release)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("local.secrets.json", optional: false) // This file is not in the git repo. See readme.md for details.
                #else 
                    .AddJsonFile("prod.settings.json", optional: false)
                #endif
                .AddEnvironmentVariables()
                .Build();

            #if DEBUG
                log.LogInformation("Retrieving locally configured connection string");
                var secret = config.GetConnectionString("UserStatsConnection");
            #else
                log.LogInformation("Retrieving connection string from Key Vault");
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                var secret = keyVaultClient.GetSecretAsync(config["ConnectionStringVaultUrl"])
                    .Result.Value;
            #endif

            log.LogInformation($"Retrieved connection string. Connecting to database.");
            
            // Create database connection
            using (var conn = new NpgsqlConnection(secret))
            {
                conn.Open();

                // Retrieve query text from ETL script file                
                log.LogInformation($"Fetching query file");
                string[] queryText = File.ReadAllLines(config["EtlScriptPath"]);
                string fullText = string.Join("\n",queryText);

                // Execute ETL script
                using (NpgsqlCommand query = new NpgsqlCommand(fullText, conn))
                {
                    try 
                    {
                        log.LogInformation($"Executing query");
                        int rows = query.ExecuteNonQuery();

                        if (rows == -1)
                        {
                            log.LogWarning("No rows were affected by the ETL query. This may indicate that data was not loaded.");
                        }

                    }
                    catch (Exception e)
                    {
                        // Azure Portal should be configured to send an email to PRM.Security@milliman.com if an exception occurs
                        log.LogError($"ERROR: Exception while executing ETL: {e.Message}");
                        throw e;
                    }
                    
                    log.LogInformation($"RunStatsETL succeeded at: {DateTime.Now} UTC");
                }

                conn.Close();
            }
        }
    }
}
