<#
    .SYNOPSIS
        Run unit tests and deploy MAP

    .DESCRIPTION
        This script assumes the repository has already been cloned to $rootPath

    .PARAMETER targetFolder
        The fully-qualified path to a folder where the MAP repository has been cloned

    .PARAMETER deployEnvironment
        The ASPNETCORE_ENVIRONMENT value for the environment being targeted for deployment
        This environment will be used to perform database migrations

    .PARAMETER testEnvironment
        The ASPNETCORE_ENVIRONMENT value for the environment where unit tests are being run

    .NOTES
        AUTHORS - Ben Wyatt, Steve Gredell
#>


Param(
    [ValidateSet("AzureCI","CI","Production","Staging","Development")]
    [string]$deployEnvironment="AzureCI",
    [ValidateSet("AzureCI","CI","Production","Staging","Development")]
    [string]$testEnvironment="CI"
)


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
$BranchName = $env:git_branch # Will be used in the version string of the octopus package & appended to database names

$gitExePath = "git"
$psqlExePath = "L:\Hotware\Postgresql\v9.6.2\psql.exe"

$dbServer = "map-ci-db.postgres.database.azure.com"
$dbUser = $env:db_deploy_user
$dbPassword = $env:db_deploy_password
$appDbName = "appdb_$BranchName".Replace("_","").Replace("-","").ToLower()
$appDbTemplateName = "appdb_ci_template"
$appDbOwner = "appdb_admin"
$logDbName = "auditlogdb_$BranchName".Replace("_","").Replace("-","").ToLower()
$logDbTemplateName = "auditlogdb_ci_template"
$logDbOwner = "logdb_admin"
$dbCreationRetries = 5 # The number of times the script will attempt to create a new database before throwing an error

$jUnitOutputJest = "../../_test_results/jest-test-results.xml"

$env:APP_DATABASE_NAME=$appDbName
$env:AUDIT_LOG_DATABASE_NAME=$logDbName
$env:ASPNETCORE_ENVIRONMENT=$testEnvironment
$env:PATH = $env:PATH+";C:\Program Files (x86)\OctopusCLI\;$env:appdata\npm\"
$rootPath = (get-location).Path
$webBuildTarget = "$rootPath\WebDeploy"
$serviceBuildTarget = "$rootPath\ContentPublishingServer\ContentPublishingService\bin\debug"
$nugetDestination = "$rootPath\nugetPackages"
$octopusURL = "https://indy-prmdeploy.milliman.com"
$octopusAPIKey = $env:octopus_api_key

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
    if ($BranchName -in "master", "develop")
    {
        log_statement "Branch name $BranchName is always built and deployed"
    }
    else {
        log_statement "Code changes were not found. No build or deployment is needed."
        exit 0
    }
}

#endregion

#region Run unit tests and exit if any fail

log_statement "Restoring packages and building MAP"

$command = "npm install -g yarn@1.5.1"
invoke-expression $command

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Failed to install yarn"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

cd $rootpath\MillimanAccessPortal\MillimanAccessPortal

$command = "yarn install --frozen-lockfile"
invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: yarn package restore failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

cd $rootpath\MillimanAccessPortal\

MSBuild /restore:true /verbosity:quiet

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Initial build of MAP solution failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}


cd $rootpath\MillimanAccessPortal\MillimanAccessPortal

log_statement "Building yarn packages"

yarn build

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: yarn build failed"
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

 cd $rootPath\MillimanAccessPortal\MapTests

 dotnet test --no-build

 if ($LASTEXITCODE -ne 0) {
     log_statement "ERROR: One or more MAP xUnit tests failed"
     log_statement "errorlevel was $LASTEXITCODE"
     exit $LASTEXITCODE
}

log_statement "Peforming Jest tests"

cd $rootPath\MillimanAccessPortal\MillimanAccessPortal

$env:JEST_JUNIT_OUTPUT = $jUnitOutputJest

$command = "yarn test --testResultsProcessor='jest-junit'"
invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: One or more Jest tests failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

log_statement "Performing content publishing unit tests"

cd $rootPath\ContentPublishingServer\ContentPublishingServiceTests

dotnet test --no-build

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: One or more content publishing xUnit tests failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

#endregion

#region Create and update databases

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

log_statement "Performing database migrations"

$env:ASPNETCORE_ENVIRONMENT = $deployEnvironment

cd $rootpath\MillimanAccessPortal\MillimanAccessPortal

dotnet ef database update

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to apply application database migrations"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}


dotnet ef database update --project "..\AuditLogLib\AuditLogLib.csproj" --startup-project ".\MillimanAccessPortal.csproj"  --context "AuditLogDbContext"

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to apply audit log database migrations"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

#endregion

log_statement "Publishing and packaging web application"

#region Publish web application to a folder

cd $rootpath\MillimanAccessPortal\MillimanAccessPortal

msbuild /t:publish /p:PublishDir=$webBuildTarget /verbosity:quiet

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to publish web application"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Copying Deployment scripts to target folder"

Get-ChildItem -path "$rootPath\Publish\*" -include *.ps1 | Copy-Item -Destination "$webBuildTarget"

#endregion

#region package the web application for nuget

cd $webBuildTarget

$webVersion = get-childitem "MillimanAccessPortal.dll" | select -expandproperty VersionInfo | select -expandproperty ProductVersion
$webVersion = "$webVersion-$branchName"

octo pack --id MillimanAccessPortal --version $webVersion --basepath $webBuildTarget --outfolder $nugetDestination\web

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to package web application for nuget"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

#endregion

#region Package publication server for nuget

log_statement "Packaging publication server"

cd $serviceBuildTarget

$serviceVersion = get-childitem "ContentPublishingService.exe" | select -expandproperty VersionInfo | select -expandproperty ProductVersion
$serviceVersion = "$serviceVersion-$branchName"

octo pack --id ContentPublishingServer --version $serviceVersion --outfolder $nugetDestination\service

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to package publication server for nuget"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

#endregion

#region Deploy releases to Octopus

log_statement "Deploying packages to Octopus"

cd $nugetDestination

octo push --package "web\MillimanAccessPortal.$webVersion.nupkg" --package "service\ContentPublishingServer.$serviceVersion.nupkg" --replace-existing --server $octopusURL --apiKey "$octopusAPIKey"

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to push packages to Octopus"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Creating web app release"

octo create-release --project "Milliman Access Portal" --version $webVersion --packageVersion $webVersion --ignoreexisting --apiKey "$octopusAPIKey" --channel "Development" --server $octopusURL

if ($LASTEXITCODE -eq 0) {
    log_statement "Web application release created successfully"
}
else {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to create Octopus release for the web application"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Deploying web app release"

octo deploy-release --project "Milliman Access Portal" --deployto "Development" --channel "Development" --version $webVersion --apiKey "$octopusAPIKey" --channel "Development" --server $octopusURL --waitfordeployment --cancelontimeout --progress

if ($LASTEXITCODE -eq 0) {
    log_statement "Web application release deployed successfully"
}
else {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to deploy the web application"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Creating Content Publishing Server release"

octo create-release --project "Content Publication Server" --version $serviceVersion --packageVersion $serviceVersion --ignoreexisting --apiKey "$octopusAPIKey" --channel "Development" --server $octopusURL

if ($LASTEXITCODE -eq 0) {
    log_statement "Publishing service application release created successfully"
}
else {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to create Octopus release for the publishing service application"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

#endregion
