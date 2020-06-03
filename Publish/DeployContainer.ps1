
Param(
    [Parameter()]
    [string]$azTenantId=$env:azTenantId,
    [Parameter()]
    [PSCredential]$SPCredential,
    [Parameter()]
    [string]$azSubscriptionId=$env:azSubscriptionId,
    [Parameter()]
    [string]$FDRG="filedropsftp-$env:ASPNETCORE_ENVIRONMENT",
    [Parameter()]
    [string]$FDConName="filedropsftp-cont",
    [Parameter()]
    [string]$FDImageName,
    [Parameter()]
    [PSCredential]$FDACRCred,
    [Parameter()]
    [string]$FDFileName="filedropsftpstagingshare",
    [Parameter()]
    [PSCredential]$FDFileCred,
    [Parameter()]
    [string]$azCertPass,
    [Parameter()]
    [string]$thumbprint
    [Parameter()]
    [string]$FDLocation = "eastus2"
)


Connect-AzAccount -ServicePrincipal -Credential $SPCredential -Tenant $azTenantId -Subscription $azSubscriptionId

Remove-AzContainerGroup `
      -ResourceGroupName $FDRG `
      -Name $FDConName

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
    DnsNameLabel                        = "filedrop-$env:ASPNETCORE_ENVIRONMENT"
}

New-AzContainerGroup @params
