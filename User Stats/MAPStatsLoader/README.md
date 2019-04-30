# MAP User Stats Loader

The MAP User Stats loader is an [Azure Function App](https://azure.microsoft.com/en-us/services/functions/) written in C#, using .NET Core. This document assumes some familiarity with serverless architectures, and all build instructions refer to Visual Studio Code. The project can be developed in Visual Studio as well, but build steps may not be identical.

This guide will focus primarily on setting up developer machines, but also on ways in which this project differs from others in this repository.

It is advised that you read through this entire guide and complete all configuration steps before attempting to make updates to this project.

## Production Environment

Azure Function apps are an abstraction layer on top of Azure App Services. Azure Functions does strip away most capabilities to configure the runtime environment, with a few exceptions. It utilizes dependency injection to pass information into function executions, most usefully the `ExecutionContext`.

The ETL load function runs on a scheduled basis. The trigger is configured directly in the `Run` method's declaration and uses CRON syntax.

The function executes the `etl.sql` script utilizing `Npgsql` and a connection string retrieved configuration.

Monitoring of the function takes place in Azure Application Insights.

## Developer Setup
### Working in Visual Studio Code

> Some aspects of development for this project (particularly publishing to Azure) will only behave properly in VS Code if the project folder is opened directly. In other words, don't open the root repository folder.

For the optimal development experience, you should install the following extensions in VS Code:

* C#
* Azure Account
* Azure Functions

If you're going to modify the deployment scripts, the Powershell extension is also highly recommended.

### Build Configuration

Build configurations are managed by `tasks.json` in the `.vscode` folder. By default, VS Code runs the task named `Publish` at the time of publication to Azure. Because we need to include an additional file (`etl.sql`), we needed to modify the Publish task.

The original (default) publish task has been renamed `publish-build` and replaced with a new task that depends on it. By utilizing dependency chaining, we can be sure the latest version of the project will be built and deployed to Azure Functions.

### Secret Management

While the project does utilize the ASP.NET Core configuration framework, it does not have a concept of User Secrets. Instead, each developer must create a file named `local.secrets.json` in their local copy of the project folder, using the following structure. This file is included in the project's `.gitignore` to make sure nobody commits their connection strings.

In order to ensure that each developer's machine is correctly configured, the function will crash during startup if the secrets file is not present.

``` json
{
    "ConnectionStrings":
    {
        "UserStatsConnection":"Server=127.0.0.1;Port=5432;Database=user_stats;User Id=postgres;Password=postgres;"
    }
}
```

Replace the connection string with one that is valid for your local PostgreSQL instance. The database name should be the one you create for user stats development in the next section.

In production, we utilize `prod.settings.json` and Azure Key Vault to manage configuration secrets.

### Azure Storage Emulator

For local debugging, develoeprs need to have the Azure Storage Emulator installed and configured. More details and a download link are available at [this page](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator). The emulator is also available in the Azure SDK, but the full SDK is not required to work on this project.

After installing, you only need to launch the emulator one time. It will automatically set up the default configuration the first time it runs, and that configuration is fine for debugging this project. Allow the configuration to run completely, then close the window when it's finished.

### Local Database Preparation

Because this function exists to load the User Stats database, you of course must have one of those to do local development.

Before going through these steps, you do need to have existing MAP application and audit log databases on your local PostgreSQL instance.

First, create a new database with whatever name you'd like to use.

Second, modify and run the following script, substituting your database credentials and database names. A user with `SUPER USER` rights on your instance will be much easier to use than a standard user. If for whatever reason you don't use a `SUPER USER` locally, this configuration may not work directly.

Keep in mind that identifiers (like database and user names) are case-sensitive in PostgreSQL 9.6, which we are currently running.

``` pgsql
-- Install extensions
CREATE EXTENSION postgres_fdw;
CREATE EXTENSION "uuid-ossp";

-- Configure foreign data wrappers
CREATE SERVER map_app
        FOREIGN DATA WRAPPER postgres_fdw
        OPTIONS (host 'localhost', port '5432', dbname 'MAP_App'); -- Substitute the name of your local MAP application database

CREATE USER MAPPING FOR postgres
        SERVER map_app
        OPTIONS (user 'postgres', password ''); -- Password must be filled in

CREATE SCHEMA map;

IMPORT FOREIGN SCHEMA public
    FROM SERVER map_app
    INTO map;

CREATE SERVER map_log
        FOREIGN DATA WRAPPER postgres_fdw
        OPTIONS (host 'localhost', port '5432', dbname 'MAP_AuditLog'); -- Substitute the name of your local MAP audit log database

CREATE USER MAPPING FOR postgres
        SERVER map_log
        OPTIONS (user 'postgres', password ''); -- Password must be filled in

CREATE SCHEMA maplog;

IMPORT FOREIGN SCHEMA public
    FROM SERVER map_log
    INTO maplog;
```

> If you encounter an error on this script, consult with the DBA.

Finally, execute everything under the "Create tables" and "Create views" sections of the `schema.sql` script under the `/UserStats/Database` folder in this repository.

After you've run these statements, you should be all set up to debug locally.

> Now that you have foreign data wrappers set up to the MAP application and audit log databases, you can refer to their tables from the user stats database. Prepend application table names with the `map` schema and audit log tables with `map_log` (i.e. `map.'AspNetUsers'` and `maplog.'AuditEvent'`). These wrappers are critical to the ETL script.

### Running Locally

To run locally, press F5 in VS Code. When the app finishes launching, the HTTP trigger's URL will be printed to the Terminal. Ctrl+Click on the URL to trigger the function

Breakpoint management in VS Code is very similar to Visual Studio.