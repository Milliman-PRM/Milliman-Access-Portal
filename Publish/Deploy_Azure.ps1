# Code Owners: Ben Wyatt, Steve Gredell

### OBJECTIVE:
#  Deploy Milliman Access Portal CI builds to Azure

### DEVELOPER NOTES:
#

#region Define functions
function log_statement {
    Param([string]$statement)

    $datestring = get-date -Format "yyyy-MM-dd HH:mm:ss"

    write-output $datestring"|"$statement
}


function fail_statement {
    Param([string]$statement)
    log_statement "DEPLOYMENT FAILED"
    log_statement $statement

    if ($host.name -notmatch "ISE") # Don't exit if we're running in PowerShell ISE
    {
        exit 42
    }
}
#endregion

log_statement "Validating & configuring environment"

#region Prepare environment
$basePath = (get-location).Path
$baseParent = $basePath.Substring(0, $basePath.LastIndexOf('\'))

$Artifacts = $baseParent+"\artifacts"

$loopRetries = 10
$loopWaitSeconds = 10

$DeploymentSource = $basePath
$DeploymentTarget = $Artifacts+"\wwwroot"

#region Prepare Kudu Sync
$command = "npm install kudusync -g --silent"
invoke-expression "&$command"
if ($LASTEXITCODE -ne 0) {
    fail_statement "Failed to install KuduSync"
}

$KuduSyncPath = $env:APPDATA+"\npm\kuduSync.cmd"
#endregion
#endregion

log_statement "Deploying application"

#region Prepare packages
log_statement "Restoring nuget packages"
$command = "dotnet restore "+$DeploymentSource+"MillimanAccessPortal\MillimanAccessPortal.sln"
invoke-expression "&$command"
if ($LASTEXITCODE -ne 0) {
    fail_statement "Failed to restore nuget packages"
}

log_statement "Restoring bower packages"
cd $DeploymentSource\MillimanAccessPortal\MillimanAccessPortal\
if ($LASTEXITCODE -ne 0) {
    fail_statement "Failed to open directory for bower packages"
}
$command = "bower install"
if ($LASTEXITCODE -ne 0) {
    fail_statement "Failed to restore bower packages"
}

log_statement "Preparing web compiler"
$tries = 0
while ((test-path "$env:temp\webcompiler*") -eq $false)
{
    $tries = $tries + 1
    log_statement "Web Compiler directory not found. Waiting for $loopWaitSeconds before trying again."
    log_statement "Attempt $tries of $loopRetries"
}

if (test-path "$env:temp\webcompiler*")
{
    $WebCompilerPath = get-childitem -Path $env:temp | where {$_.name -match 'WebComp'} | sort-object LastWriteTime | select -first 1
    $WebCompilerPath = $WebCompilerPath.FullName

    cd $WebCompilerPath
    if ($LASTEXITCODE -ne 0) {
        fail_statement "Failed to change to WebCompiler directory"
    }

    $command = "prepare.cmd"
    invoke-expression $command
    if ($LASTEXITCODE -ne 0) {
        fail_statement "Web Compiler's prepare.cmd returned an error"
    }
}
else 
{
    fail_statement "Web compiler directory was not found"
}

#endregion

#region Build and publish to temporary folder

#endregion

#region Use KuduSync to complete the publication process

#endregion

# Write success method expected by CI script
write-output "Finished successfully."