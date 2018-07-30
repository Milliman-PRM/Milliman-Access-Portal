<#
    ## CODE OWNERS: Ben Wyatt, Steve Gredell

    ### OBJECTIVE:
		Set environment variables for .net core applications deployed via Octopus Deploy

    ### DEVELOPER NOTES:

#>

Param(
    [Parameter(Mandatory=$true)]
    [string]$SiteName,
    [string]$AppName,
    [Parameter(Mandatory=$true)]
    [string]$VarName,
    [Parameter(Mandatory=$true)]
    [string]$NewValue
)

$IISPath = "IIS:\Sites\$SiteName\$AppName"
$AppCmdFolder = "C:/Windows/system32/inetsrv/"

$configPath = Get-WebConfigFile -PSPath $IISPath | select FullName
write-output "Configuration file path: $($configPath.FullName)"

$ExistingVars = Get-WebConfiguration -filter 'system.webserver/aspnetcore/environmentvariables/EnvironmentVariable' -PSPath $IISPath | where {$_.name -eq $VarName}

if ($ExistingVars.value -eq $NewValue)
{
    # Notify that no change is needed
    write-output "The target value is the same as the current value. No change will be made."
    exit
}

if ($ExistingVars -eq $null)
{
    # Create a new environment variable
    write-output "Setting $VarName for the first time. The value will be $NewValue"
 
    $config = Get-WebConfiguration 'system.webserver/aspnetcore/environmentvariables' -PSPath $IISPath

    cd $AppCmdFolder
    .\appcmd set config "$SiteName/$AppName" /section:"system.webserver/aspnetcore" /+environmentvariables."[name='$VarName',value='$NewValue']"
}
else
{
    # Change an existing value
    $ExistingValue = $ExistingVars.value
    write-output "The current value of $VarName is $ExistingValue. Changing to $NewValue"
    Set-WebConfigurationProperty -Name "Value" -Value $NewValue -PSPath $IISPath -Filter "system.webserver/aspnetcore/environmentvariables/EnvironmentVariable[@name='$VarName' and @value='$ExistingValue']"
}

$ExistingVars = Get-WebConfiguration -filter 'system.webserver/aspnetcore/environmentvariables/EnvironmentVariable' -PSPath $IISPath | where {$_.name -eq $VarName}

$ExistingValue = $ExistingVars.value
write-output "The final value of $VarName is $ExistingValue"

