# Define parameters
param (
    [Parameter(Mandatory=$true)][string]$downloadPath,
    [Parameter(Mandatory=$true)][int]$logDays=0,
    [Parameter(Mandatory=$true)][string]$azureStorageAccountKey,
    [Parameter(Mandatory=$true)][string]$azureStoragePath,
    [Parameter(Mandatory=$true)][string]$azureStorageAccountName
)

# Generate a list of possible file names
$fileNames = @()
for ($i = 0; $i -lt $logDays; $i++)
{
    $date = ([DateTime]::UtcNow - (New-TimeSpan -Days $i)).ToString("yyyy-MM-dd")
    $fileNames += "Audit_MAP-QVS-01_$date.log"
    $fileNames += "Sessions_MAP-QVS-01_$date.log"
}

# Get a list of actual files in the share
$context = New-AzureStorageContext -StorageAccountName $azureStorageAccountName -StorageAccountKey $azureStorageAccountKey 
$directory = Get-AzureStorageFile -Context $context -ShareName live -Path $azureStoragePath
$logFiles = @($directory.ListFilesAndDirectories()) | where {$_.Name -in $fileNames}

if ($logFiles.Length -eq 0)
{
    write-output "$(get-date) No log files were found within $numberOfDays days."
}

# Download files
foreach ($file in $logFiles)
{
    $file.DownloadToFile("$downloadPath/$($file.name)", 2) # "2" represents the Create file mode. More info: https://docs.microsoft.com/en-us/dotnet/api/system.io.filemode?view=netframework-4.7.2

    if ($?)
    {
        write-output "File downloaded: $($file.Name)"
    }
}

