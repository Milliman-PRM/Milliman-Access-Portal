
Param(
    [Parameter()]
    [string]$azTenantId=$env:azTenantId,
    [Parameter()]
    [string]$SPCredential,
    [Parameter()]
    [string]$azSubscriptionId=$env:azSubscriptionId
    [Parameter()]
    [string]$FDRG="filedropsftp",
    [Parameter()]
    [string]$FDConName="filedropsftp-cont",
    [Parameter()]
    [string]$FDImageName,
    [Parameter()]
    [string]$FDACRCred,
    [Parameter()]
    [string]$FDFileName,
    [Parameter()]
    [string]$FDFileCred,
)

$FDLocation = "eastus2"

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
    Command                             = "echo `"Copy certificate here`""
    EnvironmentVariable                 = @{ASPNETCORE_ENVIRONMENT = "CI"}
    AzureFileVolumeShareName            = $FDFileName
    AzureFileVolumeAccountCredential    = $FDFileCred
    AzureFileVolumeMountPath            = "/mnt/filedropshare"
    IdentityType                        = "UserAssigned"
    IdentityId                          = "77da4376-975f-4609-8ad3-25740f972c02"
}

New-AzContainerGroup @params
