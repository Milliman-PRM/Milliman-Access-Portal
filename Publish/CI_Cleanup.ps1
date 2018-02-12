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
$logDbName = "logdb_$BranchName"
$dbCreationRetries = 5 # The number of times the script will attempt to create a new database before throwing an error

$env:PATH = $env:PATH+";C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\"


#region Authenticate to Azure with a service principal

$DeployCredential = new-object -typename System.Management.Automation.PSCredential -argumentlist $deployUser,($deployPassword | ConvertTo-SecureString -AsPlainText -Force)
$silent = Add-AzureRmAccount -ServicePrincipal -Credential $DeployCredential -TenantId $TenantId  -Subscription $SubscriptionId

if ($? -eq $false)
{
    log_statement "Failed to authenticate to Azure. Unable to clean up."
    exit -1000
}

#endregion

log_statement "Removing deployment slot"

$silent = Get-AzureRmWebAppSlot -ResourceGroupName $ResourceGroupName -Name $WebAppName -Slot $Branchname
if ($? -eq $false)
{
    if ($? -eq $false)
    {
        log_statement "Deployment slot $BranchName was not found"
    }
}
else
{
    # TODO: Delete the slot here
}


#region Remove database firewall rules

$command = "az login --service-principal -u $deployUser -p $deployPassword --tenant $tenantId"
Invoke-Expression "&$command"
if ($? -eq $false)
{
    log_statement "Failed to authenticate for removal of firewall rules"
    exit -9000
}

# Retrieve the current list of firewall rules
# Will be compared against the app's IP addresses to see which rules need to be created
$command = "az postgres server firewall-rule list --server-name `"$dbServerHostname`" --resource-group `"$ResourceGroupName`"" 
$firewallRules = invoke-expression "&$command" | out-string | ConvertFrom-Json
if ($LASTEXITCODE -ne 0)
{
    log_statement "Failed retrieving list of existing firewall rules"
}
$firewallFailures = 0

foreach ( $rule in $firewallRules) 
{    
    if ($rule.name -match "Allow_"+$branchname+"_")
    {
        $ruleName = $rule.name
        $command = "az postgres server firewall-rule delete --yes --resource-group `"$ResourceGroupName`" --server `"$DbServerHostname`" --name `"$ruleName`""
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
    log_statement "Failed creating one or more firewall rules. Manual cleanup may be required."
    exit -9000
}
else 
{
    log_statement "Finished removing database firewall rules"
}
#endregion

#region Drop Databases
log_statement "Deleting application database"
$command = "'c:\program` files\postgresql\9.6\bin\psql.exe' -d postgres -h $dbServer -e -q --command=`"drop database $appDbName`""
invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to drop application database"
    log_statement $requestResult.stdout
}

log_statement "Deleting logging database"
$command = "'c:\program` files\postgresql\9.6\bin\psql.exe' -d postgres -h $dbServer -e -q --command=`"drop database $logDbName`""
invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    $error_code = $LASTEXITCODE
    log_statement "ERROR: Failed to drop logging database"
    log_statement $requestResult.stdout
}
#endregion
