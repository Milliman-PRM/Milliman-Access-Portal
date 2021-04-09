/*
* CODE OWNERS: Ben Wyatt
* OBJECTIVE: Execute the User Stats ETL script using Azure Functions
* DEVELOPER NOTES: 
*/
using System;
using System.Reflection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
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
        [FunctionName("RunStatsHttp")]
        public static void RunHttp([HttpTrigger]ExecutionContext context)
        {
            #if DEBUG // Since this trigger is intended only for local debugging, don't allow it to run in production
                ILoggerFactory loggerFactory = new LoggerFactory().AddConsole();
                ILogger log = loggerFactory.CreateLogger("MAP Stats");
                Run(log, context);
            #else
                throw new NotImplementedException();
            #endif
        }

        [FunctionName("RunStatsTimer")]
        public static void RunTimer([TimerTrigger("0 0 * * * *")] TimerInfo thisTimer, ILogger log, ExecutionContext context)
        {
            Run(log, context);
        }
        
        private static void Run(ILogger log, ExecutionContext context)
        {
            if (NpgsqlLogManager.Provider == null)
            {
                NpgsqlLogManager.Provider = new ConsoleLoggingProvider(NpgsqlLogLevel.Trace, printLevel: true);
            }

            log.LogInformation($"RunStatsETL executed at: {DateTime.Now} UTC");
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToLower();
            
            var config = new ConfigurationBuilder()
                #if DEBUG // Variable set automatically by build configuration (Debug vs. Release)
                    .SetBasePath(Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).ToString())
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("local.secrets.json", optional: false) // This file is not in the git repo. See readme.md for details.
                #else 
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile($"{environmentName}.settings.json", optional: false)
                #endif
                .AddEnvironmentVariables()
                .Build();

            #if DEBUG
                log.LogInformation("Retrieving locally configured connection string");
                var connectionString = config.GetConnectionString("UserStatsConnection");
            #else
                log.LogInformation("Retrieving connection string from Key Vault");
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                var connectionString = keyVaultClient.GetSecretAsync(config["ConnectionStringVaultUrl"])
                    .Result.Value;
                
                string dbServerOverride = Environment.GetEnvironmentVariable("MAP_DATABASE_SERVER");
                if (!string.IsNullOrEmpty(dbServerOverride)) {
                    NpgsqlConnectionStringBuilder statsDbConnectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
                    statsDbConnectionStringBuilder.Host = dbServerOverride;
                    connectionString = statsDbConnectionStringBuilder.ConnectionString;
                }
            #endif

            log.LogInformation($"Retrieved connection string. Connecting to database.");
            
            // Create database connection
            using (var conn = new NpgsqlConnection(connectionString))
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
                            log.LogWarning($"ExecuteNonQuery() returned unexpected value {rows} rows affected. That could indicate a problem.");
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
