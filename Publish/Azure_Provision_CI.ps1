<#
.SYNOPSIS
    Provision resources for the MAP CI environment in Azure
.DESCRIPTION
    Creates all needed resources for MAP CI if they do not already exist

    Must be run interactively by a user with rights to perform all the actions in the script

    Some actions will need to be done manually in the portal after running the script:
        * Change PostgreSQL database user password
        * Add key vault certificate's .pfx file to the web application
.NOTES
    File Name      : Azure_Provision_CI.ps1
    Code Owner     : Ben Wyatt, Steve Gredell
    Prerequisite   : Azure PowerShell module, Azure CLI
#>

<#
    General workflow for provisioning a resource:

    * Check to see if it already exists
        * If yes, do nothing
        * If no, create it
    * Set configuration values
    * Save configuration updates
#>

#region Script setup
# Configure general Azure properties

$ResourceGroupName = "map-ci"
$Location = "northcentralus"
$SubscriptionId = "8f047950-269e-43c7-94e0-ff90d22bf013"

# Configure environment properties

$WebAppName = "map-ci-app"
$AppServicePlanName = "map-ci"
$StorageName = "mapcistore"

$DatabaseServerName = "map-ci-db"
$AppDatabaseName = "appdbdevelop"
$LoggingDatabaseName = "logdbdevelop"
$DatabaseUserName = "prmdb"
$DatabaseUserPassword = "P@ssw0rd" # Password should be reset manually in the Azure Portal after the script finishes

$HotwarePath = "l:\hotware\postgresql\v9.6.2"

$VmName = "map-ci-qv"
$VirtualNetworkName = "map-ci-vn"
$SubnetName = "map-ci-subnet"
$SecurityGroupName = "map-ci-vm-group"
$PublicIpAddressName = "ciqv1address"

$KeyVaultName = "mapcikeyvault"

# Authenticate to Azure

Login-AzureRmAccount -Subscription $SubscriptionId

#endregion

#region Create core resources

# Create resource group, if it doesn't already exist

$ResourceGroup = Get-AzureRmResourceGroup $ResourceGroupName -ErrorAction SilentlyContinue

if ($ResourceGroup -eq $null)
{
    New-AzureRmResourceGroup -Name $ResourceGroupName -Location $Location
}

# Create and configure storage

$StorageAccount = Get-AzureRmStorageAccount -Name $StorageName -ResourceGroupName $ResourceGroupName  -ErrorAction SilentlyContinue

if ($StorageAccount -eq $null)
{
    New-AzureRmStorageAccount -Name $StorageName -Location $Location -ResourceGroupName $ResourceGroupName -SkuName "Standard_LRS" -Kind Storage -EnableEncryptionService File
}

#endregion

#region Create and configure Azure Key Vault

$kv = get-azurermkeyvault -VaultName $KeyVaultName -ResourceGroupName $ResourceGroupName

if ($kv -eq $null)
{
    write-output "Creating Azure Key Vault"
    New-AzureRmKeyVault -VaultName $KeyVaultName -ResourceGroupName $ResourceGroupName -Location $Location -Sku Premium
}

#endregion

#region Create and configure web application

$AppServicePlan = Get-AzureRmAppServicePlan -Name $AppServicePlanName -ResourceGroupName $ResourceGroupName -ErrorAction SilentlyContinue

if ($AppServicePlan -eq $null)
{
    New-AzureRmAppServicePlan -Name $AppServicePlanName -Location $Location -ResourceGroupName $ResourceGroupName -Tier Basic
}

$WebApp = Get-azurermwebapp -Name $WebAppName -ResourceGroupName $ResourceGroupName -ErrorAction SilentlyContinue

if ($WebApp -eq $null)
{
    New-AzureRmWebApp -Name $WebAppName -ResourceGroupName $ResourceGroupName -Location $Location -AppServicePlan $AppServicePlanName
}

# Configure web app settings


#endregion

#region Create and configure databases

# This requires use of the Azure CLI, since Azure Database for PostgreSQL controls haven't been implemented in powershell yet

az login
az account set --subscription $SubscriptionId

# Server

$servers = az postgres server list | convertfrom-json
$servers = $servers | select name

if ($servers.name -contains $DatabaseServerName)
{
    write-output "Database server already exists."
}
else
{
    write-output "Creating database server"
    az postgres server create --resource-group $ResourceGroupName --name $DatabaseServerName  --location $Location --admin-user "$DatabaseUserName" --admin-password "$DatabaseUserPassword" --performance-tier Basic --compute-units 50 --version 9.6    
}

$servers = az postgres server list | convertfrom-json | where {$_.name -eq $DatabaseServerName}
$DatabaseServerFQDN = $servers[0].fullyQualifiedDomainName

# Configure Firewall Rule(s)

$firewallRules = az postgres server firewall-rule list --server-name $DatabaseServerName --resource-group $ResourceGroupName | ConvertFrom-Json
$firewallRules = $firewallRules | select name

if ($ruleNames.name -contains "AllowMilliman")
{
    write-output "AllowMilliman firewall rule already set."
    $AllowMillimanRuleFound = $true
}
else
{
    az postgres server firewall-rule create --resource-group $ResourceGroupName --server $DatabaseServerName --name AllowMilliman --start-ip-address 74.116.173.3 --end-ip-address 74.116.173.3
}

# Databases

$databases = az postgres db list --server-name $DatabaseServerName --resource-group $ResourceGroupName | convertfrom-json 
$databases = $databases | select name


if ($databases.name -contains $AppDatabaseName)
{
    write-output "Application database already exists."
}
else
{
    write-output "Creating application database"
    $command = $HotwarePath + "psql.exe --host=$DatabaseServerFQDN --username=$DatabaseUserName@$DatabaseServerName --dbname=postgres -e -q --command='create database $AppDatabaseName;'"
    invoke-expression $command
}
if ($databases.name -contains $LoggingDatabaseName)
{
    write-output "Logging database already exists."
}
else
{
    write-output "Creating logging database"
    $command = $HotwarePath + "psql.exe --host=$DatabaseServerFQDN --username=$DatabaseUserName@$DatabaseServerName --dbname=postgres -e -q --command='create database $LoggingDatabaseName;'"
    invoke-expression $command
}

#endregion

#region Create and configure VM

$ExistingVM = Get-AzureRmVM -Name $VmName -ResourceGroupName $ResourceGroupName 

if ($ExistingVM -ne $null)
{
    # Create virtual network
    $subnetConfig = New-AzureRmVirtualNetworkSubnetConfig -Name MapCiSubnet -AddressPrefix 192.168.1.0/24
    $vnet = new-azurermvirtualnetwork -ResourceGroupName $ResourceGroupName -Location $Location -Name MapCiVnet -AddressPrefix 192.168.0.0/16 -Subnet $subnetConfig
    $pip = New-AzureRmPublicIpAddress -ResourceGroupName $ResourceGroupName -Location $Location -AllocationMethod Static -Name mapciqv1ip
    $nic = New-AzureRmNetworkInterface -ResourceGroupName $ResourceGroupName -Location $Location -name mapciqv1nic -SubnetId $vnet.Subnets[0].Id -PublicIpAddressId $pip.Id

    $nsgRule = New-AzureRmNetworkSecurityRuleConfig `
      -Name myNSGRule `
      -Protocol Tcp `
      -Direction Inbound `
      -Priority 1000 `
      -SourceAddressPrefix '74.116.173.3' `
      -SourcePortRange * `
      -DestinationAddressPrefix * `
      -DestinationPortRange 3389 `
      -Access Allow

    $nsg = New-AzureRmNetworkSecurityGroup `
        -ResourceGroupName $ResourceGroupName `
        -Location $Location `
        -Name myNetworkSecurityGroup `
        -SecurityRules $nsgRule

    Set-AzureRmVirtualNetworkSubnetConfig `
        -Name MapCiSubnet `
        -VirtualNetwork $vnet `
        -NetworkSecurityGroup $nsg `
        -AddressPrefix 192.168.1.0/24

    Set-AzureRmVirtualNetwork -VirtualNetwork $vnet

    # Prompt user for credentials
    $cred = Get-Credential

    $vm = New-AzureRmVMConfig -VMName $VmName -VMSize Standard_B2s
    $vm = Set-AzureRmVMOperatingSystem -VM $vm -Windows -ComputerName $VmName -Credential $cred -ProvisionVMAgent -EnableAutoUpdate
    $vm = set-azurermvmsourceimage -VM $vm -PublisherName MicrosoftWindowsServer -Offer WindowsServer -Skus 2016-Datacenter -Version latest
    $vm = Set-AzureRmVMOSDisk -VM $vm -Name ciQvOsDisk -DiskSizeInGB 128 -CreateOption FromImage -Caching ReadWrite

    $vm = Add-AzureRmVMNetworkInterface -VM $vm -Id $nic.Id

    New-AzureRmVM -ResourceGroupName $ResourceGroupName -Location $Location -VM $vm
}

#endregion