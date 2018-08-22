<#
    .SYNOPSIS
        Deploy MAP migrations
 
    .DESCRIPTION
        This script assumes the repository has already been cloned to $targetFolder

        This script must be run from a PC that has the cert installed for the 
            relevant Azure Key Vault instance (based on the $environmentName)
        
        This is mostly intended to be run from Octopus, but can be run manually as well.

        The $branchName parameter is only required if deploying to the dev/test environment (AzureCI)
    
    .PARAMETER targetFolder
        The fully-qualified path to a folder where the MAP repository has been cloned
    
    .PARAMETER environmentName
        The value to be set in ASPNETCORE_ENVIRONMENT during migrations, to aid in configuration loading

    .PARAMETER branchName
        Should only be set when deploying migrations to the AzureCI environment; ensures migrations are applied 
        against the correct branch database. If another environment is specified, this parameter is ignored.

    .NOTES
        AUTHORS - Ben Wyatt, Steve Gredell
#>

Param(
    [Parameter(Mandatory=$true)]
    [string]$targetFolder,
    [Parameter(Mandatory=$true)]
    [ValidateSet("AzureCI","CI","Production","Staging","Development")]
    [string]$environmentName,
    [Parameter]
    [string]$branchName
)

# Set the database name variables if a branch name was provided
if ($environmentName -eq "AzureCI" -and $branchName)
{
    $env:APP_DATABASE_NAME="appdb_$branchName"
    $env:AUDIT_LOG_DATABASE_NAME="auditlogdb_$branchName"
}
elseif ($environmentName -eq "AzureCI")
{
    write-error "A branch name must be provided if AzureCI is specified as the environment"
    return -42
}

# Find the csproj file and navigate to its directory
cd $targetFolder
$proj = Get-ChildItem -Recurse -Include MillimanAccessPortal.csproj

if (-not $proj)
{
    Write-Error "MillimanAccessPortal.csproj was not found under $targetFolder. The folder path provided must be a folder where the MAP repository was cloned."
    return -42
}

cd $proj.Directory.FullName

$env:ASPNETCORE_ENVIRONMENT = $environmentName
$env:MIGRATIONS_RUNNING = "true" # Flag variable to avoid startup errors during migrations

# Update audit log database
dotnet ef database update --project "..\AuditLogLib\AuditLogLib.csproj" --startup-project ".\MillimanAccessPortal.csproj" --context "AuditLogDbContext"

if ($LASTEXITCODE -ne 0)
{
	write-error "Audit log migrations failed with exit code $LASTEXITCODE"
	return -42
}

# Update app database
dotnet ef database update --context "ApplicationDbContext"

if ($LASTEXITCODE -ne 0)
{
	write-error "App database migrations failed with exit code $LASTEXITCODE"
	return -42
}