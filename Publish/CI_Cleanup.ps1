# Code Owners: Ben Wyatt, Steve Gredell

### OBJECTIVE:
#  Run cleanup steps for CI builds of Milliman Access Portal

### DEVELOPER NOTES:
#


#region Define Functions
function log_statement {
    Param([string]$statement)

    $datestring = get-date -Format "yyyy-MM-dd HH:mm:ss"

    write-output $datestring"|"$statement
}
#endregion

#region Configure environment properties
$BranchName = $env:git_branch.Replace("_","").Replace("-","").ToLower() # Will be used as the name of the deployment slot & appended to database names

$psqlExePath = "L:\Hotware\Postgresql\v9.6.2\psql.exe"

$dbServer = "map-ci-db.postgres.database.azure.com"
$dbUser = $env:db_deploy_user
$dbPassword = $env:db_deploy_password
$appDbName = "appdb_$BranchName"
$logDbName = "auditlogdb_$BranchName"

#region Drop Databases

$env:PGPASSWORD = $dbPassword

# Check if databases already exist
$appDbFound = $false
$logDbFound = $false

$command = "$psqlExePath --dbname=postgres  -h $dbServer -U $dbUser --tuples-only --command=`"select datname from Pg_database`" --echo-errors"
$output = invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to query for existing databases"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

foreach ($db in $output) {
    if ($db.trim() -eq $appDbName) {
        log_statement "MAP application database found for this branch."
        $appDbFound = $true
    }
    elseif ($db.trim() -eq $logDbName) {
        log_statement "Logging database found for this branch."
        $logDbFound = $true
    }
}

if ($appDbFound)
{
    log_statement "Deleting application database"
    $command = "'c:\program` files\postgresql\9.6\bin\psql.exe' -d postgres -U $dbUser -h $dbServer -e -q --command=`"drop database $appDbName`""
    invoke-expression "&$command"

    if ($LASTEXITCODE -ne 0) {
        $error_code = $LASTEXITCODE
        log_statement "ERROR: Failed to drop application database"
        log_statement $requestResult.stdout
        exit 42
    }
}
else 
{
    log_statement "Application database was not found for this branch"
}

if ($logDbFound)
{
    log_statement "Deleting logging database"
    $command = "'c:\program` files\postgresql\9.6\bin\psql.exe' -d postgres -U $dbUser -h $dbServer -e -q --command=`"drop database $logDbName`""
    invoke-expression "&$command"

    if ($LASTEXITCODE -ne 0) {
        $error_code = $LASTEXITCODE
        log_statement "ERROR: Failed to drop audit log database"
        log_statement $requestResult.stdout
        exit 42
    }
}
else
{
    log_statement "Audit Log database was not found for this branch"
}
#endregion
