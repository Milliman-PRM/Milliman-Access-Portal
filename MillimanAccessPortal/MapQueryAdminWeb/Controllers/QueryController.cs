using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuditLogLib.Event;
using AuditLogLib.Services;
using MapQueryAdminWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace MapQueryAdminWeb.Controllers
{
    public class QueryController : Controller
    {
        public IAuditLogger _logger;
        public IConfiguration _config;

        public QueryController (IAuditLogger loggerArg, IConfiguration configArg)
        {
            _logger = loggerArg;
            _config = configArg;
        }

        [HttpGet]
        public IActionResult RunQuery()
        {
            RunQueryModel model = new RunQueryModel();

            // Get connection string info for display
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"));
            ViewData["Server"] = builder.Host;
            ViewData["Database"] = builder.Database;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RunQuery(RunQueryModel model)
        {
            // Get connection string info for display
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"));
            ViewData["Server"] = builder.Host;
            ViewData["Database"] = builder.Database;

            // Initialize connection
            NpgsqlConnection conn = new NpgsqlConnection(getAppDbConnectionString(model.pgsqlUsername, model.pgsqlPassword));
            conn.Open();
            NpgsqlTransaction transaction = conn.BeginTransaction();
            NpgsqlCommand command = new NpgsqlCommand(model.queryText, conn);

            try
            {
                // Execute query
                int rows = command.ExecuteNonQuery();

                _logger.Log(AuditEventType.ManualDatabaseCommand.ToEvent(
                    model.pgsqlUsername,
                    model.referenceUrl,
                    model.approverName,
                    model.queryText,
                    rows
                    ));

                // Only commit the transaction after the action is logged
                transaction.Commit();

                // Give the user an indication of success
                ViewData["result"] = $"The query executed successfully. {rows} rows were affected.";
            }
            catch (Exception ex)
            {
                // Give the user an indication of failure
                transaction.Rollback();
                ViewData["result"] = $"The query failed.<br />Message: {ex.Message}<br />Stack Trace:<br />{ex.StackTrace}";
            }
            finally
            {
                conn.Close();
            }
            return View(model);
        }

        [NonAction]
        public string getAppDbConnectionString(string username, string password)
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"));

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
    }
}