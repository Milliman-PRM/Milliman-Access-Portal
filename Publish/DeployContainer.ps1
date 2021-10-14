<#
    .SYNOPSIS
        Deploy FileDrop SFTP server

    .NOTES
        AUTHORS - Steve Gredell, Ben Wyatt
#>

import-module PSTokens
$version = $OctopusParameters["Octopus.Action.Package[MillimanAccessPortal].PackageVersion"].split('-')[0]
$branch = "$version-$TrimmedBranch"

$passwd = ConvertTo-SecureString $AzDeploymentPrincipalPassword -AsPlainText -Force
$SPCredential = New-Object System.Management.Automation.PSCredential($AzDeploymentPrincipalClient, $passwd)

Connect-AzAccount -ServicePrincipal -Credential $SPCredential -Tenant $AzureTenantId -Subscription $AzDeploymentPrincipalSubscriptionNumber

$FDImageName = "$ContainerRegistryUrl/filedropsftp:$branch"

$azFileShareRootPass = (Get-AzStorageAccountKey -ResourceGroupName $StorageAccountResourceGroupName -AccountName $FileStorageAccount)[0].Value

$params = @{
    AZURE_TENANT_ID                     = $AzureTenantId
    AZURE_CLIENT_ID                     = $FileDropServicePrincipalClient
    AZURE_CLIENT_SECRET                 = $FileDropServicePrincipalPassword
    ResourceGroupName                   = $FileDropResourceGroup
    container_name                      = $FileDropContainerName
    location                            = $FileDropLocation
    azCertPass                          = $azCertPass
    thumbprint                          = $thumbprint
    ASPNETCORE_ENVIRONMENT              = $EnvironmentCode.ToUpper()
    AzureVaultName                      = $AzureVaultName
    acr_url                             = $ContainerRegistryUrl
    acr_user                            = $ContainerRegistryUsername
    acr_password                        = $ContainerRegistryPassword
    Image                               = $FDImageName
    filedropdns                         = "map-$EnvironmentShortCode-sftp"
    filedroprootsharename               = $FileDropFileShareName
    filedroplogssharename               = $FileDropLogShareName
    filedroprootstoragename             = $FileStorageAccount
    filedroprootkey                     = $azFileShareRootPass
    filedroplogsstoragename             = $FileStorageAccount
    filedroplogskey                     = $azFileShareRootPass
    MAP_DATABASE_SERVER                 = $DatabaseServerPublicFqdn
}

$template = Get-Content ".\MillimanAccessPortal\SFTPServer.yaml.template" | Merge-Tokens -tokens $params

$template_path = ".\SFTPServer.yaml"
$template | Set-Content -path $template_path

az login --service-principal -u $AzDeploymentPrincipalClient -p $AzDeploymentPrincipalPassword  --tenant $AzureTenantId
az account set --subscription $AzDeploymentPrincipalSubscriptionNumber
az container create --resource-group $FileDropResourceGroup --file $template_path --registry-login-server $ContainerRegistryUrl  --registry-username $ContainerRegistryUsername  --registry-password $ContainerRegistryPassword
    