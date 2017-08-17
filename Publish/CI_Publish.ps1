# Code Owners: Ben Wyatt, Steve Gredell

### OBJECTIVE:
#  Run configuration steps for CI builds of Milliman Access Portal

### DEVELOPER NOTES:
#  

$branchName = $env:GIT_BRANCH

$branchFolder = "D:\installedapplications\map_ci\$branchName\"
$outputPath = "D:\installedapplications\map_ci\$branchName\error.log"
$urlFilePath = "D:\installedapplications\map_ci\$branchName\urls.log"
$urlBase = "https://indy-qvtest01.milliman.com/"
$errorCode = 0

import-module webadministration

if ($errorCode -eq 0)
{
    # Clear URL file, if it exists
    set-content -LiteralPath $urlFilePath "Published URL:"

    # (Re-)create applications and log deployed URLs to text file
    try 
    {
        $name = "MAP_CI_$branchName"
        $appPool = Get-ChildItem -Path IIS:\AppPools | where {$_.name -eq $name}

        $ci_username = $env:ephi_username
        $ci_password = $env:ephi_password

        # Create branch-specific app pool if it doesn't already exist
        if (-not $appPool)
        {
            $command = "C:\windows\system32\inetsrv\appcmd.exe add apppool /name:$name /managedRuntimeVersion:v4.0"
            invoke-expression $command 

            # Configuring credentials must be done separately from creating the application pool
            $command = "C:\windows\system32\inetsrv\appcmd.exe set config /section:applicationPools `"/[name='$name'].processModel.identityType:SpecificUser`" `"/[name='$name'].processModel.userName:$ci_username`" `"/[name='$name'].processModel.password:$ci_password`""
            invoke-expression $command
        }

        # Database migrations for application

        # Database migrations for logger

        # If the web application already exists, remove it
        if ((Get-WebApplication $name).Count -gt 0) { Remove-WebApplication -Name $name -Site "Default Web Site" }

        # Create web application
        New-WebApplication -Name $name -PhysicalPath $branchFolder -Site "Default Web Site" -ApplicationPool "$name"
        Set-Content -LiteralPath $urlFilePath ($urlBase + "/" + $name + "/")
    }
    catch [Exception]
    {
        $_.Exception | format-list -force
        $errorCode = 1
    }
}

# Write out the error code
Set-Content -LiteralPath $outputPath $errorCode