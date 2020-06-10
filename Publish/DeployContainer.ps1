<#
    .SYNOPSIS
        Deploy FileDrop SFTP server

    .DESCRIPTION
        This script assumes the environment is set up in CI_Publish

    .NOTES
        AUTHORS - Steve Gredell
#>
Param(
    [Parameter()]
    [string]$envCommonName
    [Parameter()]
    [string]$azTenantId=$env:azTenantId,
    [Parameter()]
    [PSCredential]$SPCredential,
    [Parameter()]
    [string]$azSubscriptionId=$env:azSubscriptionId,
    [Parameter()]
    [string]$FDRG,
    [Parameter()]
    [string]$FDConName="filedropsftp-cont",
    [Parameter()]
    [string]$FDImageName,
    [Parameter()]
    [PSCredential]$FDACRCred,
    [Parameter()]
    [string]$FDFileName,
    [Parameter()]
    [PSCredential]$FDFileCred,
    [Parameter()]
    [string]$azCertPass,
    [Parameter()]
    [string]$thumbprint,
    [Parameter()]
    [string]$FDLocation = "eastus2"
)


Connect-AzAccount -ServicePrincipal -Credential $SPCredential -Tenant $azTenantId -Subscription $azSubscriptionId

$params = @{
    ResourceGroupName                   = $FDRG
    Name                                = $FDConName
    Image                               = $FDImageName
    RegistryCredential                  = $FDACRCred
    Location                            = $FDLocation
    OsType                              = "Linux"
    CPU                                 = 1
    MemoryInGB                          = 1.5
    IpAddressType                       = "Public"
    Port                                = 22
    Command                             = "/bin/sh /app/startsftpserver.sh $azCertPass $thumbprint" # "tail -f /dev/null"
    EnvironmentVariable                 = @{ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT}
    AzureFileVolumeShareName            = $FDFileName
    AzureFileVolumeAccountCredential    = $FDFileCred
    AzureFileVolumeMountPath            = "/mnt/filedropshare"
    DnsNameLabel                        = "filedrop-$envCommonName"
}

$containerGroup = New-AzContainerGroup @params

$TMName = switch ($($env:ASPNETCORE_ENVIRONMENT).ToUpper()) {
    "STAGING" {"filedrop-staging"}
    "PRODUCTION" {"filedrop-prod"}
    default {"filedrop-ci"}
}

$TrafficManagerEndpoint = Get-AzTrafficManagerEndpoint -Name $TMName -Type "ExternalEndpoints" -ResourceGroupName $FDRG -ProfileName "filedrop-sftp-endpoint"

$TrafficManagerEndpoint.Target = $($containerGroup).Fqdn

Set-AzTrafficManagerEndpoint -TrafficManagerEndpoint $TrafficManagerEndpoint
