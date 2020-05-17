
Param(
    [Parameter()]
    [string]$azTenantId=$env:azTenantId,
    [Parameter()]
    [PSCredential]$SPCredential,
    [Parameter()]
    [string]$azSubscriptionId=$env:azSubscriptionId,
    [Parameter()]
    [string]$FDRG="filedropsftp",
    [Parameter()]
    [string]$FDConName="filedropsftp-cont",
    [Parameter()]
    [string]$FDImageName,
    [Parameter()]
    [PSCredential]$FDACRCred,
    [Parameter()]
    [string]$FDFileName="filedropsftpshare",
    [Parameter()]
    [PSCredential]$FDFileCred
)

$FDLocation = "eastus2"
$FDFileName = "filedropsftpshare"

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
    IdentityId                          = "/subscriptions/8f047950-269e-43c7-94e0-ff90d22bf013/resourceGroups/filedropsftp/providers/Microsoft.ManagedIdentity/userAssignedIdentities/filedrop-sftp"
}

New-AzContainerGroup @params
