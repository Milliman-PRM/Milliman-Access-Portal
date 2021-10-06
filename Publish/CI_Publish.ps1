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
    [ValidateSet("AzureCI","CI","Production","Staging","Development","Internal")]
    [string]$deployEnvironment="AzureCI",
    [ValidateSet("AzureCI","CI","Production","Staging","Development")]
    [string]$testEnvironment="CI"
)

import-module az.accounts, az.keyvault

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
$BranchName = $env:GITHUB_PR_SOURCE_BRANCH # Will be used in the version string of the octopus package & appended to database names

$buildType = if($BranchName -eq 'develop' -or $BranchName -eq 'master' -or $BranchName.ToLower() -like 'pre-release*' -or $BranchName.ToLower() -like "*hotfix*") {"Release"} Else {"Debug"}
log_statement "Building configuration: $buildType"

$gitExePath = "git"
$TrimmedBranch = $BranchName.Replace("_","").Replace("-","").Replace(".","").ToLower()
log_statement "$BranchName trimmed to $TrimmedBranch"

$jUnitOutputJest = "../../_test_results/jest-test-results.xml"

$core2="C:\Program Files\dotnet\sdk\2.2.105\Sdks"
$core3="C:\Program Files\dotnet\sdk\3.1.409\Sdks"
$env:MSBuildSDKsPath=$core3
$env:APP_DATABASE_NAME=$appDbName
$env:AUDIT_LOG_DATABASE_NAME=$logDbName
$env:ASPNETCORE_ENVIRONMENT=$testEnvironment
$env:PATH = $env:PATH+";C:\Program Files (x86)\OctopusCLI\;$env:appdata\npm\"
$rootPath = (get-location).Path
$webBuildTarget = "$rootPath\WebDeploy"
$serviceBuildTarget = "$rootPath\ContentPublishingServer\ContentPublishingService\bin\$buildType"
$queryAppBuildTarget = "$rootPath\MillimanAccessPortal\MapQueryAdminWeb\bin\$buildType\netcoreapp2.1"
$nugetDestination = "$rootPath\nugetPackages"
$octopusURL = "https://indy-prmdeploy.milliman.com"
$octopusAPIKey = $env:octopus_api_key
$runTests = $env:RunTests -ne "False"

mkdir -p ${rootPath}\_test_results
#endregion

#region Exit if only notes have changed within the current branch (comparing against develop)
# if we're not building in "Release" mode
if ($buildType -ne "Release")
{
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
    if ($diff -like '*/*' -and $diff -notlike 'Notes/*' -and $diff -notlike '.github/*' -and $diff -notlike 'UtilityScripts/*')
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
}
#endregion

#region Run unit tests and exit if any fail

log_statement "Restoring packages and building MAP"

# Switch to the correct version of Node.js using NVM
$command = "nvm use 14.18.0"
Invoke-Expression $command

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Switching to Node.js v14.18.0 failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

Set-Location $rootpath\MillimanAccessPortal\MillimanAccessPortal

$command = "yarn install --frozen-lockfile"
invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: yarn package restore failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

Set-Location $rootpath\MillimanAccessPortal\

MSBuild /restore:true /verbosity:minimal /p:Configuration=$buildType

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Initial build of MAP solution failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}


Set-Location $rootpath\MillimanAccessPortal\MillimanAccessPortal

log_statement "Building yarn packages"

yarn build-prod

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: yarn build-prod failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

log_statement "Building documentation"

Set-Location "$rootpath\Documentation\"
cmd /c "compileUserDocs.bat"

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: failed to build documentation"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

Set-Location $rootpath\ContentPublishingServer

log_statement "Building content publishing server"

MSBuild /restore:true /verbosity:quiet /p:Configuration=$buildType

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Test build or package restore failed for content publishing server solution"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

$env:MSBuildSDKsPath=$core2
Set-Location "$rootPath\User Stats\MAPStatsLoader"

log_statement "Building MAP User Stats loader"

msbuild /restore /t:Publish /p:Configuration=$buildType /p:Platform=x64

if ($LASTEXITCODE -ne 0)
{
    log_statement "ERROR: Build failed for MAP User Stats Loader project"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

$env:MSBuildSDKsPath=$core3
Set-Location "$rootPath\SftpServer"

log_statement "Building SFTP Server"

Get-ChildItem -Recurse "$rootpath\SftpServer\out" | remove-item
mkdir "out"

MSBuild /restore:true /verbosity:minimal /p:Configuration=$buildType /p:outdir="$rootPath\SftpServer\out"

if ($LASTEXITCODE -ne 0)
{
    log_statement "ERROR: Build failed for MAP SFTP Server project"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

$sFTPVersion = get-childitem "$rootpath\SftpServer\out\SftpServer.dll" -Recurse | Select-Object -expandproperty VersionInfo -First 1 | Select-Object -expandproperty ProductVersion
$sFTPVersion = "$sFTPVersion-$TrimmedBranch"

if($runTests) {
    log_statement "Performing MAP unit tests"

    Set-Location $rootPath\MillimanAccessPortal\MapTests

    dotnet test --no-build --configuration $buildType "--logger:trx;LogFileName=${rootPath}\_test_results\MAP-tests.trx"

    if ($LASTEXITCODE -ne 0) {
        log_statement "ERROR: One or more MAP xUnit tests failed"
        log_statement "errorlevel was $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    log_statement "Peforming Jest tests"

    Set-Location $rootPath\MillimanAccessPortal\MillimanAccessPortal

    $env:JEST_JUNIT_OUTPUT = $jUnitOutputJest

    $command = "yarn test --ci --reporters='jest-junit'"
    invoke-expression "&$command"

    if ($LASTEXITCODE -ne 0) {
        log_statement "ERROR: One or more Jest tests failed"
        log_statement "errorlevel was $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    log_statement "Performing content publishing unit tests"

    Set-Location $rootPath\ContentPublishingServer\ContentPublishingServiceTests

    if ($buildType -eq "Release") {
        dotnet test --no-build --configuration $buildType "--logger:trx;LogFileName=${rootPath}\_test_results\CPS-tests.trx"

        if ($LASTEXITCODE -ne 0) {
            log_statement "ERROR: One or more content publishing xUnit tests failed"
            log_statement "errorlevel was $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    }
}
#endregion

log_statement "Publishing and packaging web application"

#region Publish web application to a folder

Set-Location $rootpath\MillimanAccessPortal\MillimanAccessPortal

msbuild /t:publish /p:PublishDir=$webBuildTarget /verbosity:quiet /p:Configuration=$buildType

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to publish web application"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Copying Deployment scripts to target folder"

Get-ChildItem -path "$rootPath\Publish\*" -include *.ps1 | Copy-Item -Destination "$webBuildTarget"
Get-ChildItem -path "$rootPath\Publish\*" -include *.template | Copy-Item -Destination "$webBuildTarget"


#endregion

#region package the web application for nuget

Set-Location $webBuildTarget


$webVersion = get-childitem "MillimanAccessPortal.dll" -Recurse | Select-Object -expandproperty VersionInfo -First 1 | Select-Object -expandproperty ProductVersion
$webVersion = "$webVersion-$TrimmedBranch"

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

Set-Location $serviceBuildTarget

$serviceVersion = get-childitem "ContentPublishingService.exe" -Recurse | Select-Object -expandproperty VersionInfo -first 1 | Select-Object -expandproperty ProductVersion
$serviceVersion = "$serviceVersion-$TrimmedBranch"

octo pack --id ContentPublishingServer --version $serviceVersion --outfolder $nugetDestination\service

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to package publication server for nuget"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

#endregion

#region Package MAP User Stats Loader for nuget
log_statement "Packaging MAP User Stats Loader"

Set-Location "$rootPath\User Stats\MAPStatsLoader\"
Set-Location (Get-ChildItem -Directory "publish" -Recurse | Select-Object -First 1)

octo pack --id UserStatsLoader --version $webVersion --outfolder $nugetDestination\UserStatsLoader

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to package user stats loader for nuget"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}
#endregion

#region Publish MAP Query Admin to a folder
log_statement "Publishing MAP Query Admin to a folder"

Set-Location $rootpath\MillimanAccessPortal\MapQueryAdminWeb

msbuild /t:publish /p:PublishDir=$queryAppBuildTarget /verbosity:quiet /p:Configuration=$buildType

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to publish query admin app application"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

#endregion

#region Package MAP Query Admin for nuget
log_statement "Packaging MAP Query Admin"

Set-Location $queryAppBuildTarget

$queryVersion = get-childitem "MapQueryAdminWeb.dll" -Recurse | Select-Object -expandproperty VersionInfo -first 1 | Select-Object -expandproperty ProductVersion
$queryVersion = "$queryVersion-$TrimmedBranch"

octo pack --id MapQueryAdmin --version $queryVersion --outfolder $nugetDestination\QueryApp

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to package MAP Query Admin for nuget"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

#endregion

#region Configure releases in Octopus

log_statement "Pushing nuget packages to Octopus"

Set-Location $nugetDestination

octo push --package "UserStatsLoader\UserStatsLoader.$webVersion.nupkg" --space "Spaces-2" --package "web\MillimanAccessPortal.$webVersion.nupkg" --package "service\ContentPublishingServer.$serviceVersion.nupkg" --package "QueryApp\MapQueryAdmin.$queryVersion.nupkg" --replace-existing --server $octopusURL --apiKey "$octopusAPIKey"

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to push packages to Octopus"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Creating web app release"
# Determine appropriate release channel (applies only at the time the release is created)
if ($BranchName.ToLower() -like "*pre-release*" -or $BranchName.ToLower() -like "*hotfix*")
{
    $channelName = "Pre-Release"
}
else
{
    $channelName = "Pre-Release" # TODO: Set this to "Dev" once the Dev Azure environment is up and running
}

octo create-release --project "Web App" --space "Spaces-2" --channel $channelName --version $webVersion --packageVersion $webVersion --ignoreexisting --apiKey "$octopusAPIKey" --server $octopusURL

if ($LASTEXITCODE -eq 0) {
    log_statement "Web application release created successfully"
}
else {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to create Octopus release for the web application"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Creating Content Publishing Service release"

octo create-release --project "Content Publishing Service" --space "Spaces-2" --version $serviceVersion --packageVersion $serviceVersion --ignoreexisting --apiKey "$octopusAPIKey" --server $octopusURL

if ($LASTEXITCODE -eq 0) {
    log_statement "Publishing service application release created successfully"
}
else {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to create Octopus release for the publishing service application"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Creating MAP Query Admin release"

octo create-release --project "Query Admin" --space "Spaces-2" --version $queryVersion --packageVersion $queryVersion --ignoreexisting --apiKey "$octopusAPIKey" --server $octopusURL

if ($LASTEXITCODE -eq 0) {
    log_statement "MAP Query Admin release created successfully"
}
else {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to create Octopus release for MAP Query Admin"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Creating Database Migrations project release"

octo create-release --project "Database Migrations" --space "Spaces-2" --channel $channelName --version $webVersion --packageVersion $webVersion --ignoreexisting --apiKey "$octopusAPIKey" --server $octopusURL

if ($LASTEXITCODE -eq 0) {
    log_statement "Database Migrations release created successfully"
}
else {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to create Octopus release for the Database Migrations project"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Creating SFTP Server project release"

octo create-release --project "SFTP Server" --space "Spaces-2" --channel $channelName --version $webVersion --packageVersion $webVersion --ignoreexisting --apiKey "$octopusAPIKey" --server $octopusURL

if ($LASTEXITCODE -eq 0) {
    log_statement "SFTP Server release created successfully"
}
else {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to create Octopus release for the SFTP Server project"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

log_statement "Creating Full Stack project release"

octo create-release --project "Full Stack" --space "Spaces-2" --channel $channelName --version $webVersion --packageVersion $webVersion --ignoreexisting --apiKey "$octopusAPIKey" --server $octopusURL

if ($LASTEXITCODE -eq 0) {
    log_statement "Full Stack release created successfully"
}
else {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to create Octopus release for the Full Stack project"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $error_code
}

#endregion
