# Code Owners: Ben Wyatt, Steve Gredell

### OBJECTIVE:
#  Deploy Milliman Access Portal CI builds to Azure

### DEVELOPER NOTES:
#

#region Define Functions
function log_statement {
    Param([string]$statement)

    $datestring = get-date -Format "yyyy-MM-dd HH:mm:ss"

    write-output $datestring"|"$statement
}
function log_output {
    log_statement "Dumping captured command output"
    write-output ""
    write-output "===="
    log_statement "content of output.txt"
    write-output ""
    write-output (get-content "$env:temp\output.txt")
    write-output ""
    write-output "===="
    log_statement "content of error.txt"
    write-output ""
    write-output (get-content "$env:temp\error.txt")
}
function create_db { # Attempt to create a database by copying another one; retry up to $maxRetries before returning
    Param([string]$server,
            [string]$user,
            [string]$newDbName,
            [string]$templateDbName,
            [string]$maxRetries,
            [string]$exePath,
            [string]$dbOwner)

    $attempts = 0
    $waitSeconds = 60
    $success = $false
    $commandText = "create database $newDbName with template $templateDbName owner $dbOwner;"

    $command = "$exePath -h $server -d postgres -U $user -c `"$commandText`" -w"

    log_statement "Attempting to create database $newDbName with the command `"$command`""

    while ($attempts -lt $maxRetries -and $success -eq $false) {
        $attempts = $attempts + 1
        invoke-expression "&$command"
        if ($LASTEXITCODE -eq 0) {
            $success = $true
            log_statement "$newDbName was created successfully"
        }
        else {
            log_statement "Creation of $newDbName failed with exit code $LASTEXITCODE; Attempt #$attempts of $maxRetries"
            if ($attempts -lt ($maxRetries + 1)) {
                log_statement "Waiting $waitSeconds before re-trying..."
                start-sleep $waitSeconds
            }
        }
    }

    if ($success -eq $false)
    {
        exit -42
    }
}
#endregion

#region Configure environment properties
$ResourceGroupName = "map-ci"
$SubscriptionId = "8f047950-269e-43c7-94e0-ff90d22bf013"
$TenantId = "15dfebdf-8eb6-49ea-b9c7-f4b275f6b4b4"
$BranchName = $env:git_branch.Replace("_","").Replace("-","").ToLower() # Will be used as the name of the deployment slot & appended to database names

$gitExePath = "git"
$credManagerPath = "L:\Hotware\Powershell_Plugins\CredMan.ps1"
$psqlExePath = "L:\Hotware\Postgresql\v9.6.2\psql.exe"

$dbServerHostname = "map-ci-db"
$dbServer = "map-ci-db.postgres.database.azure.com"
$dbUser = $env:db_deploy_user
$dbPassword = $env:db_deploy_password
$appDbName = "appdb_$BranchName"
$appDbTemplateName = "appdb_ci_template"
$appDbOwner = "appdb_admin"
$logDbName = "auditlogdb_$BranchName"
$logDbTemplateName = "auditlogdb_ci_template"
$logDbOwner = "logdb_admin"
$dbCreationRetries = 5 # The number of times the script will attempt to create a new database before throwing an error

$jUnitOutputJest = "../../_test_results/jest-test-results.xml"

$env:PATH = $env:PATH+";C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\;$env:appdata\npm\"
$rootPath = (get-location).Path

#endregion


#region Exit if only notes have changed within the current branch (comparing against develop)

$command = "$gitExePath diff --name-only origin/develop 2>&1"
$diffOutput = Invoke-Expression "$command" | out-string

log_statement "git diff Output:"
write-output $diffOutput

if ($diffOutput -like "git*:*fatal:*")
{
  exit 42
}

$diffOutput = $diffOutput.Split([Environment]::NewLine)

$codeChangeFound = $false

foreach ($diff in $diffOutput)
{
  # If both of these are true, the line being examined is likely a change to the software that needs testing
  if ($diff -like '*/*' -and $diff -notlike 'Notes/*' -and $diff -notlike '.github/*')
  {
    log_statement "Code change found in $diff"
    $codeChangeFound = $true
    break
  }
}

# If no code changes were found, we don't have to run the rest of this script
if ($codeChangeFound -eq $false)
{
  log_statement "Code changes were not found. No build or deployment is needed."
  exit 0
}

#endregion

#region Run unit tests and exit if any fail

cd MillimanAccessPortal\

log_statement "Restoring packages and building MAP"

$command = "npm install -g yarn@1.5.1"
invoke-expression $command

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Failed to install yarn"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

$command = "yarn install --frozen-lockfile"
invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: yarn package restore failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

MSBuild /restore:true /verbosity:quiet

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Initial build of MAP solution failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

cd $rootpath\ContentPublishingServer

log_statement "Building content publishing server"

MSBuild /restore:true /verbosity:quiet /nowarn:CS1998

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Test build or package restore failed for content publishing server solution"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

log_statement "Performing MAP unit tests"

dotnet test --no-build

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: One or more xUnit tests failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

cd $rootPath\MillimanAccessPortal\MillimanAccessPortal

log_statement "Peforming Jest tests"

$env:JEST_JUNIT_OUTPUT = $jUnitOutputJest

$command = "yarn test --testResultsProcessor='jest-junit'"
invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: One or more Jest tests failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

log_statement "Performing Content publishing server unit tests"
 
 dotnet test --no-build
 
 if ($LASTEXITCODE -ne 0) {
     log_statement "ERROR: One or more Content publishing server xUnit tests failed"
     log_statement "errorlevel was $LASTEXITCODE"
     exit $LASTEXITCODE
}

cd $rootPath

#endregion

#region Clone databases

log_statement "Preparing branch databases"

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
        $appDbFound = 1
    }
    elseif ($db.trim() -eq $logDbName) {
        log_statement "Logging database found for this branch."
        $logDbFound = 1
    }
}

# Create app db if necessary
if ($appDbFound -eq $false)
{
    create_db -server $dbServer -user $dbUser -exePath $psqlExePath -maxRetries $dbCreationRetries -newDbName $appDbName -templateDbName $appDbTemplateName -dbOwner $appDbOwner
}

# Create log db if necessary
if ($logDbFound -eq $false)
{
    create_db -server $dbServer -user $dbUser -exePath $psqlExePath -maxRetries $dbCreationRetries -newDbName $logDbName -templateDbName $logDbTemplateName -dbOwner $logDbOwner
}

remove-item env:PGPASSWORD

#endregion
