# Code Owners: Ben Wyatt, Steve Gredell

### OBJECTIVE:
#  Deploy Milliman Access Portal CI builds to Azure

### DEVELOPER NOTES:
#

function log_statement {
    Param([string]$statement)

    $datestring = get-date -Format "yyyy-MM-dd HH:mm:ss"

    write-output $datestring"|"$statement
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


#region Configure environment properties
$ResourceGroupName = "map-ci"
$SubscriptionId = "8f047950-269e-43c7-94e0-ff90d22bf013"
$TenantId = "15dfebdf-8eb6-49ea-b9c7-f4b275f6b4b4"
$WebAppName = "map-ci-app"
$AppServicePlanName = "map-ci"
$BranchName = "CreateAzureCI".Replace("_","").Replace("-","").ToLower() # Will be used as the name of the deployment slot & appended to database names

$deployUser = $env:app_deploy_user
$deployPassword = $env:app_deploy_password

$gitExePath = "git"
$credManagerPath = "L:\Hotware\Powershell_Plugins\CredMan.ps1"
$psqlExePath = "L:\Hotware\Postgresql\v9.6.2\psql.exe"

$dbServer = "map-ci-db.postgres.database.azure.com"
$dbUser = $env:db_deploy_user
$dbPassword = $env:db_deploy_password
$appDbName = "appdb_$BranchName"
$appDbTemplateName = "appdb_ci_template"
$appDbOwner = "appdb_admin"
$logDbName = "logdb_$BranchName"
$logDbTemplateName = "logdb_ci_template"
$logDbOwner = "logdb_admin"
$dbCreationRetries = 5 # The number of times the script will attempt to create a new database before throwing an error

#endregion

#region Run unit tests and exit if any fail

$command = "activate prod2016_11"
Invoke-Expression $command
if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Failed to initialize environment"
    exit $LASTEXITCODE
}

$rootPath = (get-location).Path

cd MillimanAccessPortal\MillimanAccessPortal

log_statement "Building unit tests"

MSBuild /t:Restore /verbosity:quiet

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Initial MAP package restore failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

$command = '"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Web\External\bower.cmd" install'
invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Bower package restore failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

cd $rootPath\MillimanAccessPortal\MapTests

MSBuild /t:Restore /verbosity:quiet

if ( $LASTEXITCODE -ne 0 ) {
    log_statement "ERROR: Unit test dependency restore failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

MSBuild /verbosity:quiet

if ( $LASTEXITCODE -ne 0 ) {
    log_statement "ERROR: Unit test build failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

log_statement "Performing unit tests"

dotnet test --no-build -v q

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: One or more tests failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

cd $rootPath

#endregion

$env:PSModulePath = $env:PSModulePath+';C:\Program Files (x86)\Microsoft SDKs\Azure\PowerShell\ResourceManager\AzureResourceManager'

#Load required PowerShell modules
$silent = import-module AzureRM.Profile, AzureRM.Resources, AzureRM.Websites, Microsoft.PowerShell.Security 

if ($? -eq $false)
{
    log_statement "Failed to load modules"
    exit -1000
}

#region Authenticate to Azure with a service principal

$DeployCredential = new-object -typename System.Management.Automation.PSCredential -argumentlist $deployUser,($deployPassword | ConvertTo-SecureString -AsPlainText -Force)
$silent = Add-AzureRmAccount -ServicePrincipal -Credential $DeployCredential -TenantId $TenantId  -Subscription $SubscriptionId

if ($? -eq $false)
{
    log_statement "Failed to authenticate to Azure. Unable to deploy."
    exit -1000
}

#endregion

#region Create and configure deployment slot

log_statement "Preparing deployment slot"

$silent = Get-AzureRmWebAppSlot -ResourceGroupName $ResourceGroupName -Name $WebAppName -Slot $Branchname
if ($? -eq $false)
{
    New-AzureRmWebAppSlot -ResourceGroupName $ResourceGroupName -AppServicePlan $AppServicePlanName -Name $WebAppName -Slot $BranchName

    if ($? -eq $false)
    {
        log_statement "Failed to create deployment slot"
        exit -1000
    }
}
else
{
    log_statement "Deployment slot $BranchName already exists"
}

# Configure local Git deployment
$PropertiesObject = @{
    scmType = "LocalGit";
}
$silent = Set-AzureRmResource -PropertyObject $PropertiesObject -ResourceGroupName $ResourceGroupName -ResourceType Microsoft.Web/sites/slots/config -ResourceName "$WebAppName/$BranchName/web" -ApiVersion 2016-08-01 -Force

if ($? -eq $false)
{
    log_statement "Failed to configure scmType"
    exit -1000
}

# Update branch name
$resource = Invoke-AzureRmResourceAction -ResourceGroupName $ResourceGroupName -ResourceType Microsoft.Web/sites/slots/config -ResourceName "$WebAppName/$BranchName/appsettings" -Action list -ApiVersion 2016-08-01 -Force
$resource.Properties.BranchName = $BranchName
$silent = New-AzureRmResource -PropertyObject $resource.properties -ResourceGroupName $ResourceGroupName -ResourceType Microsoft.Web/sites/slots/config -ResourceName "$WebAppName/$BranchName/appsettings" -ApiVersion 2016-08-01 -Force

if ($? -eq $false)
{
    log_statement "Failed to set BranchName environment variable in deployment slot"
    exit -1000
}

# Retrieve git remote URL
$deployProperties = Get-AzureRmResource -ResourceGroupName $ResourceGroupName -ResourceType Microsoft.Web/sites/slots/sourcecontrols -ResourceName "$WebAppName/$BranchName/web" -ApiVersion 2016-08-01

# Get app-level deployment credentials
$xml = [xml](Get-AzureRmWebAppSlotPublishingProfile -Name $webappname -Slot $BranchName -ResourceGroupName $ResourceGroupName -OutputFile null)
$gitUser = $xml.SelectNodes("//publishProfile[@publishMethod=`"MSDeploy`"]/@userName").value
$gitPassword = $xml.SelectNodes("//publishProfile[@publishMethod=`"MSDeploy`"]/@userPWD").value

$remoteUrl = $deployProperties.Properties.repoUrl

# Retrieve public URL of deployment slot to output later
$slot = Get-AzureRmResource -ResourceGroupName map-ci -ResourceType Microsoft.Web/sites/slots -ResourceName "$WebAppName/test-slot" -ApiVersion 2016-08-01
if ($? -eq $false)
{
    log_statement "Failed to retrieve deployment slot properties"
    exit -1000
}

$publicURL = "https://$($slot.Properties.defaultHostName)"

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

#region Create Windows credential store object for deployment

.$credManagerPath -AddCred -Target "git:$RemoteUrl" -User $gitUser -pass $gitPassword
if ($LASTEXITCODE -ne 0)
{
    log_statement "Failed to add git credential."
    #exit -100
}

$command = "$gitexepath config --global credential.helper manager"
Invoke-Expression "$command"
if ($LASTEXITCODE -ne 0)
{
    log_statement "Failed to configure git credential manager."
    exit -800
}
#endregion

#region Push to git remote

$command = "$gitExePath remote remove ci_push"
$silent = Invoke-Expression "&$command" # We don't really care if this succeeds or not, so silence the output

$command = "$gitExePath remote add ci_push $RemoteUrl"
Invoke-Expression "$command"
if ($LASTEXITCODE -ne 0)
{
    log_statement "Failed to add git remote."
    exit -200
}

log_statement "Local script complete. Pushing to Azure to finalize deployment."

$command = "$gitExePath push ci_push `"HEAD:refs/heads/master`" --force"
Invoke-Expression "&$command"
if ($LASTEXITCODE -ne 0)
{
    log_statement "Deployment failed"
    exit -300
}

log_statement "Deployment succeeded to $publicURL"

#endregion