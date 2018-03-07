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
function fail_statement {
    Param([string]$statement)
    log_statement "DEPLOYMENT FAILED"
    log_statement "Working directory was $pwd"
    log_statement $statement
    write-output ""
    write-output "===="
    log_statement "Dump of local variables for troubleshooting"
    write-output ""
    variable

    log_output

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
$projectPath = "$basePath\MillimanAccessPortal\MillimanAccessPortal"
$solutionPath = "$basePath\MillimanAccessPortal"

$Artifacts = "$baseParent\artifacts"

$nextManifestPath = "$Artifacts\manifest"
$previousManifestPath = $nextManifestPath

$loopRetries = 10
$loopWaitSeconds = 10

$DeploymentSource = $basePath
$DeploymentTarget = "D:\home\site\wwwroot"

$DeploymentTempFolder = "__deployTemp"+(get-random).ToString()
$DeploymentTemp = "$env:temp\$deploymentTempFolder"

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
start-process "npm" -ArgumentList "install","kudusync","-g","--silent" -wait -RedirectStandardOutput "$env:temp\output.txt" -redirectstandarderror "$env:temp\error.txt"
if ($? -eq $false) {
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

log_statement "Restoring nuget packages"
start-process "dotnet" -ArgumentList "restore" -wait
if ($? -eq $false) {
        fail_statement "Failed to restore nuget packages"
}

cd $ProjectPath
if ((get-location).Path -ne $projectPath) {
    fail_statement "Failed to open project directory"
}

log_statement "Restoring yarn packages"
$command = "npm install -g yarn@1.5.1"
invoke-expression $command

if ($LASTEXITCODE -ne 0) {
    fail_statement "ERROR: Failed to install yarn"
}

log_statement "Getting yarn path"
$potentialYarnPath = Join-Path (get-item $baseParent).Parent.FullName "node_modules\yarn\bin\yarn"
if (test-path $potentialYarnPath) {
    log_statement "Found yarn executable at $(potentialYarnPath)"
    $yarnPath = $potentialYarnPath
} else {
    fail_statement "Failed to find the yarn executable"
}

$command = "$(yarnPath) install"
invoke-expression "&$command"

if ($LASTEXITCODE -ne 0) {
    fail_statement "ERROR: yarn package restore failed"
}

#region Web Compiler setup
# If the WebCompiler folder isn't present, do a build that will fail, which triggers it to be created
if ((test-path "$env:temp\webcomp*") -eq $false)
{
    "Running false build to generate the WebCompiler folder."
    $command = "`"$msbuild15path`" /verbosity:minimal /nowarn:MSB3884"
    Invoke-Expression "&$command"

}

log_statement "Looking for Web Compiler"
$tries = 0
while ((test-path "$env:temp\webcompiler*" -PathType Container) -eq $false -and $tries -lt $loopRetries)
{
    $tries = $tries + 1
    log_statement "Web Compiler directory not found. Waiting for $loopWaitSeconds seconds before trying again."
    log_statement "Attempt $tries of $loopRetries"
    start-sleep -seconds $loopWaitSeconds
}

if (test-path "$env:temp\webcompiler*" -PathType Container)
{
    log_statement "Web Compiler directory located"
    log_statement "Looking for Web Compiler packages or prepare.cmd"

    $WebCompilerPath = get-childitem -Path $env:temp | where {$_.name -match 'WebComp'} | sort-object LastWriteTime | select -first 1
    $WebCompilerPath = $WebCompilerPath.FullName

    # Wait for Web compiler contents to exist
    $tries = 0
    while ((test-path "$WebCompilerPath\node_modules") -eq $false -and (test-path "$WebCompilerPath\prepare.cmd") -eq $false -and $tries -lt $loopRetries)
    {
        $tries = $tries + 1
        log_statement "Web Compiler components not found. Waiting for $loopWaitSeconds seconds before trying again."
        log_statement "Attempt $tries of $loopRetries"
        start-sleep -seconds $loopWaitSeconds
    }

    cd $WebCompilerPath
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
        start-process "$pwd\prepare.cmd" -wait -RedirectStandardOutput "$env:temp\output.txt" -redirectstandarderror "$env:temp\error.txt"
        if ($? -eq $false) {
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

new-item -Path $env:temp -Name $DeploymentTempFolder -ItemType "directory"
if ($? -eq $false) {
    fail_statement "Failed to create deployment target directory"
}

start-process "$MSbuild15Path" -ArgumentList "`"$ProjectPath\MillimanAccessPortal.csproj`"","/t:publish","/p:PublishDir=$DeploymentTemp","/verbosity:minimal","/nowarn:MSB3884" -wait -RedirectStandardOutput "$env:temp\output.txt" -redirectstandarderror "$env:temp\error.txt"
if ($? -eq $false) {
    fail_statement "Failed to build application"
}
log_output
#endregion

#region Use KuduSync to complete the publication process

log_statement "Finalizing deployment with KuduSync"

start-process "$kuduSyncPath" -ArgumentList "-v 50","-f `"$DeploymentTemp`"","-t `"$DeploymentTarget`"","-n `"$nextManifestPath`"","-p `"$previousManifestPath`"","-i `".git;.hg;.deployment;deploy.cmd`"" -RedirectStandardOutput "$env:temp\output.txt" -redirectstandarderror "$env:temp\error.txt"
if ($? -eq $false){
    fail_statement "KuduSync returned an error."
}
log_output
#endregion

# Write success method expected by CI script
write-output "Finished successfully."
