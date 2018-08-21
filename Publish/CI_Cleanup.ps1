# Code Owners: Ben Wyatt, Steve Gredell

### OBJECTIVE:
#  Run cleanup steps for CI builds of Milliman Access Portal, and publish if it was merged into master or develop

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
$BranchName = $env:branch_name.Replace("_","").Replace("-","").ToLower() # Will be used as the name of the deployment slot & appended to database names

$gitExePath = "git"
$psqlExePath = "L:\Hotware\Postgresql\v9.6.2\psql.exe"

$dbServer = "map-ci-db.postgres.database.azure.com"
$dbUser = $env:db_deploy_user
$dbPassword = $env:db_deploy_password
$appDbName = "appdb_$BranchName"
$logDbName = "auditlogdb_$BranchName"

$rootPath = (get-location).Path
$nugetDestination = "$rootPath\nugetPackages"
$cleanupVersion = "1.0.0"
$cleanupPackageVersion = "$cleanupVersion-$branchName"
$octopusURL = "https://indy-prmdeploy.milliman.com"
$octopusAPIKey = $env:octopus_api_key

$env:PATH = $env:PATH+";C:\Program Files (x86)\OctopusCLI\;"

$IsMerged = $env:IsMerged
$MergeBase = $env:MergeBase
$CloneURL = "https://indy-github.milliman.com/PRM/milliman-access-portal.git"

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

#region Prepare nuget package for Octopus cleanup tasks

mkdir $nugetDestination

copy-item "$rootPath\Publish\OctopusSetBranch.ps1" -Destination "$nugetDestination\OctopusSetBranch.ps1"
copy-item "$rootPath\Publish\OctopusCleanup.ps1" -Destination "$nugetDestination\OctopusCleanup.ps1"

Set-Location $nugetDestination

log_statement "Packaging cleanup scripts for deployment"

octo pack --id MAPCleanup --version $cleanupPackageVersion $nugetDestination --outfolder $nugetDestination

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to package cleanup script for nuget"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

octo push --package "MAPCleanup.$cleanupPackageVersion.nupkg" --replace-existing --server $octopusURL --apiKey "$octopusAPIKey"

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to push package to Octopus"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Creating release"

octo create-release --project "Test Branch Cleanup" --version $cleanupPackageVersion --packageVersion $cleanupPackageVersion --ignoreexisting --apiKey "$octopusAPIKey" --channel "Development" --server $octopusURL

if ($LASTEXITCODE -eq 0) {
    log_statement "Cleanup release created successfully"
}
else {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to create Octopus release for cleanup"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Deploying and executing cleanup package"

octo deploy-release --project "Test Branch Cleanup" --deployto "Development" --channel "Development" --version $cleanupPackageVersion --apiKey "$octopusAPIKey" --channel "Development" --server $octopusURL --waitfordeployment --cancelontimeout --progress

if ($LASTEXITCODE -eq 0) {
    log_statement "Cleanup release deployed successfully"
}
else {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to deploy the cleanup package"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

#endregion

#region Modify Environment to push base branch to Dev

if ($IsMerged) {

    log_statement "Deploying $MergeBase to dev infrastructure"

    Set-Location $rootPath

    Remove-Item -path $nugetDestination -Recurse -Force

    if ($MergeBase -eq 'develop') {
        $checkoutPath = $rootPath
    } elseif ($MergeBase -eq 'master') {
        $checkoutPath = "$env:TEMP\$env:repo_name\"
        Set-Location $env:TEMP
        & $gitExePath clone $CloneURL
        Set-Location $checkoutPath
    }
    $env:git_branch = $MergeBase
    & $gitExePath checkout $MergeBase
    & "$checkoutPath\Publish\CI_Publish.ps1"

    if ($LASTEXITCODE -eq 0) {
        log_statement "$MergeBase deployed successfully"
    }
    else {
        $error_code = $LASTEXITCODE
        log_statement "ERROR: Failed to deploy $MergeBase"
        log_statement "errorlevel was $LASTEXITCODE"
        exit $error_code
    }
}

#endregion
