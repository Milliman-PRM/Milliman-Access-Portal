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
$WebAppName = "map-ci-app"
$AppServicePlanName = "map-ci"
$BranchName = $env:git_branch.Replace("_","").Replace("-","").ToLower() # Will be used as the name of the deployment slot & appended to database names

$deployUser = $env:app_deploy_user
$deployPassword = $env:app_deploy_password

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

$env:PATH = $env:PATH+";C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\;$env:appdata\npm\"
$rootPath = (get-location).Path

#endregion


#region Exit if only notes have changed within the current branch (comparing against develop)

$command = "$gitExePath diff --name-only develop 2>&1"
$diffOutput = Invoke-Expression "&$command" | out-string

log_statement "git diff Output:"
write-output $diffOutput

$diffOutput = $diffOutput.Split([Environment]::NewLine)

$codeChangeFound = $false

foreach ($diff in $diffOutput)
{
  # If both of these are true, the line being examined is likely a change to the software that needs testing
  if ($diff -like '*/*' -and $diff -notlike 'Notes/*')
  {
    $codeChangeFound = $true
    break
  }
}

# If no code changes were found, we don't have to run the rest of this script
if ($codeChangeFound -eq $false)
{
  exit 0
}

#endregion

#region Run unit tests and exit if any fail

cd MillimanAccessPortal\MillimanAccessPortal

log_statement "Restoring packages before unit tests"

MSBuild /t:Restore /verbosity:quiet

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Initial nuget package restore failed for MAP solution"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

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


cd $rootpath\ReductionServer

log_statement "Performing test build of reduction server"

MSBuild /restore:true /verbosity:quiet /nowarn:CS1998

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: Test build or package restore failed for reduction server solution"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}


log_statement "Building unit tests"

cd $rootPath\MillimanAccessPortal\MapTests

MSBuild /t:Restore /verbosity:quiet

if ( $LASTEXITCODE -ne 0 ) {
    log_statement "ERROR: Unit test dependency restore failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

MSBuild /verbosity:quiet /nowarn:CS1998

if ( $LASTEXITCODE -ne 0 ) {
    log_statement "ERROR: Unit test build failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

log_statement "Performing unit tests"

dotnet test --no-build -v q

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: One or more xUnit tests failed"
    log_statement "errorlevel was $LASTEXITCODE"
    exit $LASTEXITCODE
}

cd $rootPath\MillimanAccessPortal\MillimanAccessPortal

$command = "yarn test"
invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    log_statement "ERROR: One or more Jest tests failed"
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
    scmType = "LocalGit"
}
Set-AzureRmResource -PropertyObject $PropertiesObject -ResourceGroupName $ResourceGroupName -ResourceType Microsoft.Web/sites/slots/config -ResourceName "$WebAppName/$BranchName/web" -ApiVersion 2016-08-01 -Force

if ($? -eq $false)
{
    log_statement "Failed to configure App Settings"
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
$slot = Get-AzureRmResource -ResourceGroupName map-ci -ResourceType Microsoft.Web/sites/slots -ResourceName "$WebAppName/$BranchName" -ApiVersion 2016-08-01
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

#region Configure database firewall rules

$command = "az login --service-principal -u $deployUser -p $deployPassword --tenant $tenantId"
Invoke-Expression "&$command"
if ($? -eq $false)
{
    log_statement "Failed to authenticate for creation of firewall rules"
    exit -9000
}

log_statement "Defining database firewall rules"

# Retrieve list of IP addresses the web app may use
$properties = Get-AzureRmResource -ResourceGroupName map-ci -ResourceType Microsoft.Web/sites/slots -ResourceName "map-ci-app/$BranchName" -ApiVersion 2016-08-01
$outboundList = $properties.Properties.possibleOutboundIpAddresses.Split(',')

# Retrieve the current list of firewall rules
# Will be compared against the app's IP addresses to see which rules need to be created
$command = "az postgres server firewall-rule list --server-name `"$dbServerHostname`" --resource-group `"$ResourceGroupName`""
$firewallRules = invoke-expression "&$command" | out-string | ConvertFrom-Json
if ($LASTEXITCODE -ne 0)
{
    log_statement "Failed retrieving list of existing firewall rules"
}
$firewallFailures = 0

foreach ( $ip in $outboundList)
{
    if ($ip -notin $firewallRules.startIpAddress -and $ip -notin $firewallRules.endIpAddress)
    {
        $ruleName = "Allow_"+$BranchName+"_"+$ip.replace(".","")
        $command = "az postgres server firewall-rule create --resource-group `"$ResourceGroupName`" --server `"$DbServerHostname`" --name `"$ruleName`" --start-ip-address $ip --end-ip-address $ip"
        invoke-expression "&$command"
        if ($LASTEXITCODE -ne 0)
        {
            log_statement "Failed to create firewall rule named $ruleName"
            $firewallFailures = $firewallFailures + 1
        }
    }
}

if ($firewallFailures -gt 0)
{
    log_statement "Failed creating one or more firewall rules. Deployment canceled."
    exit -9000
}
else
{
    log_statement "Finished creating database firewall rules"
}
#endregion

#region Create Windows credential store object for deployment

$command = "$credManagerPath -DelCred -Target `"git:$RemoteUrl`""
start-process "powershell.exe" -ArgumentList "-Command `"$command`"" -wait -RedirectStandardOutput "$env:temp\output.txt" -redirectstandarderror "$env:temp\error.txt"
log_statement "Attempted to delete an existing credential, if one exists. Return code was $LASTEXITCODE."
log_output

.$credManagerPath -AddCred -Target "git:$RemoteUrl" -User "$gitUser" -pass "$gitPassword"
if ($LASTEXITCODE -ne 0)
{
    log_statement "Failed to add git credential"
    exit 42
}
#endregion

#region Push to git remote

$command = "$gitExePath remote remove ci_push 2>&1"
$silent = Invoke-Expression "&$command" | out-string
if ($LASTEXITCODE -ne 0)
{
    if ($silent -notlike "*fatal: No such remote:*")
    {
        log_statement "Remote cleanup failed with error:"
        log_statement $silent
        exit -250
    }
}

$command = "$gitExePath remote add ci_push $RemoteUrl"
Invoke-Expression "$command"
if ($LASTEXITCODE -ne 0)
{
    log_statement "Failed to add git remote."
    exit -200
}



log_statement "Checking if git credential exists"
$Attempts = 1
$NumberRetries = 5
$WaitSeconds = 10
$CredentialFound = $False

while ($attempts -lt $NumberRetries -and $credentialFound -eq $false)
{
    $command = "powershell -command `"$credManagerPath -GetCred -Target `"git:$RemoteUrl`" *>&1`""
    $output = invoke-expression "&$command" | out-string
    if ($output -match "Found credentials as:")
    {
        $credentialFound = $true
        log_statement "Credential was found; ready to push to Azure to finalize deployment"
        log_statement "Local script complete. Console output will be delayed until the remote deployment script is finished."
    }
    else
    {
        log_statement "Credential not found. Waiting $WaitSeconds seconds before trying again."
        $Attempts = $Attempts + 1
        start-sleep -Seconds $WaitSeconds
    }
}

if ($CredentialFound)
{
    start-process "activate" -argumentlist "prod2016_11" -Wait
    if ($LASTEXITCODE -ne 0) {
        log_statement "ERROR: Failed to initialize environment"
        exit $LASTEXITCODE
    }

    # "Unset" the git credential helper, so that its cache will be cleared
    $command = "$gitexepath config --global --unset credential.helper wincred"
    Invoke-Expression "$command"
    if ($LASTEXITCODE -ne 0)
    {
        log_statement "Failed to unset git credential manager."
        exit -800
    }

    $command = "$gitexepath config --global credential.helper wincred"
    Invoke-Expression "$command"
    if ($LASTEXITCODE -ne 0)
    {
        log_statement "Failed to set git credential manager."
        exit -800
    }

    $command = "$gitExePath push ci_push `"HEAD:refs/heads/master`" --force 2>&1"
    $pushOutput = Invoke-Expression "&$command" | out-string

    log_statement "Push Output:"
    write-output $pushOutput

    if ($pushOutput -notlike "*remote: Finished successfully.*")
    {
        log_statement "Deployment failed"
        exit -300
    }
    else
    {
        log_statement "Deployment succeeded to $publicURL"
    }
}
else
{
    log_statement "Git credential was not found"
    exit -200
}
#endregion

#region Check login page to confirm deployment

try
{
    $resp = Invoke-WebRequest "$publicURL/Account/Login"
}
catch
{
    log_statement "Failed to get login page: $publicURL/Account/Login"
    exit -404
}

if ($resp.StatusCode -ne 200)
{
    log_statement "ERROR: Login page failed with code $($resp.StatusCode)"
    exit $resp.StatusCode
}


#endregion
