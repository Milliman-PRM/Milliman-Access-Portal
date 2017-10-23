# Code Owners: Ben Wyatt, Steve Gredell

### OBJECTIVE:
#  Run cleanup steps for CI builds of Milliman Access Portal

### DEVELOPER NOTES:
#

function log_statement {
    Param([string]$statement)

    $datestring = get-date -Format "yyyy-MM-dd HH:mm:ss"

    write-output $datestring"|"$statement
}

$branchName = $env:git_branch.ToLower()
$branchFolder = "D:\installedapplications\map_ci\$branchName\"
$AppPool = "MAP_CI_$branchName"
$MAPDBNAME = "millimanaccessportal_ci_$branchName"
$MAPDBNAME_DEVELOP = "millimanaccessportal_ci_develop"
$LOGDBNAME = "mapauditlog_ci_$branchName"
$LOGDBNAME_DEVELOP = "mapauditlog_ci_develop"
$ASPNETCORE_ENVIRONMENT = "CI"
$errorCount = 0 # Tally errors as we go, rather than failing the script immediately

log_statement "Deleting web application"
$requestURL = "http://localhost:8044/iis_delete_app?app_name=$name&action=delete"
$requestResult = Invoke-WebRequest -Uri $requestURL | ConvertFrom-Json
# Return code 50 indicates the app doesn't currently exist. That's fine in this case.
if ($requestResult.returncode -ne 0 -and $requestResult.returncode -ne 50) {
    log_statement "ERROR: Failed to create the web application"
    log_statement $requestResult.stdout
    $errorCount += 1
}

log_statement "Deleting IIS application pool"
$requestURL = "http://localhost:8044/iis_pool_action?pool_name=$appPool&action=delete"
$requestResult = Invoke-WebRequest -Uri $requestURL | ConvertFrom-Json

if ($requestResult.returncode -ne 0) {
    log_statement "ERROR: Failed to stop application pool"
    log_statement $requestResult.stdout
    $errorCount += 1
}

log_statement "Deleting application database"
$command = "'c:\program` files\postgresql\9.6\bin\psql.exe' -d postgres -h localhost -e -q --command=`"drop database $MAPDBNAME`""
invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to drop application database"
    log_statement "errorlevel was $LASTEXITCODE"
    $error_count += 1
}

log_statement "Deleting logging database"
$command = "'c:\program` files\postgresql\9.6\bin\psql.exe' -d postgres -h localhost -e -q --command=`"drop database $LOGDBNAME`""
invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to drop logging database"
    log_statement "errorlevel was $LASTEXITCODE"
    $error_count += 1
}

log_statement "Deleting deployed files"
remove-item $branchFolder -Recurse
if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Failure during file deletion cleanup"
    $error_count += 1
}

# Check for errors and exit with an error if any occurred
if ($errorCount -gt 0) {
    log_statement "FAILURE: One or more errors occurred during branch cleanup"
    log_statement "Review previous log statements to diagnose errors"
    log_statement "Manual cleanup of some items may be required"
    exit -1
}
