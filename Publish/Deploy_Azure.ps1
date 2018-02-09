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
    log_statement "Dump of local variables for troubleshooting"
    variable

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
$projectPath = $basePath+"\MillimanAccessPortal\MillimanAccessPortal"
$solutionPath = $basePath+"\MillimanAccessPortal"

$Artifacts = $baseParent+"\artifacts"

$loopRetries = 10
$loopWaitSeconds = 10

$DeploymentSource = $basePath
$DeploymentTarget = $Artifacts+"\wwwroot"

$DeploymentTemp = "$env:temp\__deployTemp"+(get-random).ToString()

#region MSBuild 15
$VersionFolder = get-childitem -Path "d:\Program Files (x86)" -Name "MSBuild-15*" | select -first 1
$MSbuild15Path = "D:\Program Files (x86)\$VersionFolder\MSBuild\15.0\bin\msbuild.exe"

if ((test-path $MSbuild15Path) -eq $false)
{
    log_statement "Failed to locate MSBuild Version 15"
    log_statement "These other locations may be matches:"
    get-childitem -Path "d:\Program Files (x86)" -Name "MSBuild*"
    fail_statement ""
}
#endregion

#region Prepare Kudu Sync
invoke-expression "npm install kudusync -g --silent"
if ($LASTEXITCODE -ne 0) {
    fail_statement "Failed to install KuduSync"
}

$KuduSyncPath = $env:APPDATA+"\npm\kuduSync.cmd"
#endregion
#endregion

log_statement "Deploying application"

#region Prepare packages
cd $SolutionPath
if ((get-location).Path -ne $SolutionPath) {
    fail_statement "Failed to open project directory"
}

try {
    log_statement "Restoring nuget packages"
    Invoke-Expression "dotnet restore"
} 
catch {
        fail_statement "Failed to restore nuget packages"
}

cd $ProjectPath
if ((get-location).Path -ne $projectPath) {
    fail_statement "Failed to open project directory"
}

log_statement "Restoring bower packages"
Invoke-Expression "bower install -V -f"
if ($LASTEXITCODE -ne 0) {
    fail_statement "Failed to restore bower packages"
}

$command = "`"$msbuild15path`" /verbosity:minimal"
invoke-expression "&$command"
if ($LASTEXITCODE -ne 0) {
    fail_statement "Failed initial test build"
}

#region Web Compiler setup
log_statement "Looking for Web Compiler"
$tries = 0
while ((test-path "$env:temp\webcompiler*") -eq $false -and $tries -lt $loopRetries)
{
    $tries = $tries + 1
    log_statement "Web Compiler directory not found. Waiting for $loopWaitSeconds before trying again."
    log_statement "Attempt $tries of $loopRetries"
    start-sleep -seconds $loopWaitSeconds
}

if (test-path "$env:temp\webcompiler*")
{
    log_statement "Looking for Web Compiler packages or prepare.cmd"

    $WebCompilerPath = get-childitem -Path $env:temp | where {$_.name -match 'WebComp'} | sort-object LastWriteTime | select -first 1
    $WebCompilerPath = $WebCompilerPath.FullName

    # Wait for Web compiler contents to exist
    $tries = 0
    while ((test-path "$WebCompilerPath\node_modules") -eq $false -and (test-path "$WebCompilerPath\prepare.cmd") -eq $false -and $tries -lt $loopRetries) 
    {
        $tries = $tries + 1
        log_statement "Web Compiler components not found. Waiting for $loopWaitSeconds before trying again."
        log_statement "Attempt $tries of $loopRetries"
        start-sleep -seconds $loopWaitSeconds
    }
    
    $command = "cd $WebCompilerPath"
    Invoke-Expression $command
    if ((get-location).Path -ne $WebCompilerPath) {
        fail_statement "Failed to change to WebCompiler directory"
    }

    if (test-path "$WebCompilerPath\node_modules")
    {
        log_statement "Web Compiler packages were found"
    }
    elseif (test-path "$WebCompilerPath\prepare.cmd")
    {
        log_statement "Executing prepare.cmd"
        $command = "prepare.cmd"
        invoke-expression $command
        if ($LASTEXITCODE -ne 0) {
            fail_statement "Web Compiler's prepare.cmd returned an error"
        }
    }
    elseif ($tries -ge $loopRetries)
    {
        fail_statement "Web Compiler components were not found after $loopRetries attempts."
    }
}
else 
{
    fail_statement "Web compiler directory was not found"
}
#endregion
#endregion

#region Build and publish to temporary folder

log_statement "Build and publish application files to temporary folder"

invoke-expression "mkdir $DeploymentTemp"
if ($LASTEXITCODE -ne 0) {
    fail_statement "Failed to create deployment target directory"
}

$command = "`"$MsBuild15Path`" `"$ProjectPath\MillimanAccessPortal.csproj`" /t:Restore /t:publish /p:PublishDir=$branchFolder /verbosity:minimal /nowarn:MSB3884"
invoke-expression "%$command"
if ($LASTEXITCODE -ne 0) {
    fail_statement "Failed to build application"
}
#endregion

#region Use KuduSync to complete the publication process

log_statement "Finalizing deployment with KuduSync"

$command = "$kuduSyncPath -v 50 -f `"$DeploymentTemp`" -t `"$DeploymentTarget`" -n `"$nextManifestPath`" -p `"$previousManifestPath`"  -i `".git;.hg;.deployment;deploy.cmd`""
Invoke-Expression "%$command"
if ($LASTEXITCODE -ne 0){
    fail_statement "KuduSync returned an error."
}
#endregion

# Write success method expected by CI script
write-output "Finished successfully."