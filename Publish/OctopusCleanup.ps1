# Code Owners: Ben Wyatt, Steve Gredell

### OBJECTIVE:
#  Deploy Milliman Access Portal CI builds to Azure

### DEVELOPER NOTES:
#  The version number in this package could be derived from MAP's version number, but that would require a build. 
#       I (BW) decided to leave that out of the first version, to keep the script as fast as possible.

Param(
    [Parameter(Mandatory=$true)]
    [string]$SiteName,
    [Parameter(Mandatory=$true)]
    [string]$AppName,
    [Parameter(Mandatory=$true)]
    [string]$PoolName,
    [Parameter(Mandatory=$true)]
    [string]$FolderToRemove
)

Import-Module WebAdministration

# Remove web app

$webApp = Get-WebApplication -Name "$AppName" -site "$SiteName"

if ($webApp)
{
    write-output "Removing web application"
    Remove-WebApplication -Name "$AppName" -Site "$SiteName"
}

# Delete deployed files

if (test-path $FolderToRemove)
{
    write-output "Deleting files"
    Get-ChildItem -Path $FolderToRemove -Recurse | Remove-Item -Force

    Remove-Item $FolderToRemove -Force
}

# Remove app pool

Remove-WebAppPool -name $PoolName
