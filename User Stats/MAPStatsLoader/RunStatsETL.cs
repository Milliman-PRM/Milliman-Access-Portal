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
using System.IO;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace MAP.UserStats
{
    public static class RunStatsETL
    {
        
        [FunctionName("RunStatsETL")]
        public static void Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"RunStatsETL executed at: {DateTime.Now}");

            // Retrieve connection string from Azure Key Vault
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

            var keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

            var secret = keyVaultClient.GetSecretAsync("https://map-prod-vault.vault.azure.net/secrets/ConnectionStrings--DefaultConnection/")
                .Result.Value;

            log.LogInformation($"Retrieved connection string. Connecting to database.");

            // Create database connection
            using (var conn = new NpgsqlConnection(secret))
            {
                conn.Open();
                
                log.LogInformation($"Fetching query file");

                // Retrieve query text from ETL script file
                string[] queryText = File.ReadAllLines("etl.sql");
                
                log.LogInformation($"Executing query");

                // Execute ETL script
                using (NpgsqlCommand query = new NpgsqlCommand(string.Join("",queryText), conn))
                {
                    try 
                    {
                        query.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        // Azure Portal should be configured to send an email to PRM.Security@milliman.com if an exception occurs
                        log.LogError($"ERROR: Exception while executing ETL: {e.Message}");
                        throw e;
                    }
                    
                    log.LogInformation($"RunStatsETL succeeded at: {DateTime.Now}");
                }

                conn.Close();
            }
        }
    }
}
