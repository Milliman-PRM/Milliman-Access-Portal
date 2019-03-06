<#
.DESCRIPTION
  Extracts data from a folder of QlikView Server log files (session and audit logs) and loads them into a database.

  All parameters are required.

.PARAMETER downloadPath
    The full path to the folder to search for QlikView log files
    
.PARAMETER logDays
    The number of days of logs to process. Default is 0 (current day only)

.PARAMETER azureStorageAccountKey
    The PostgreSQL Server hosting the user stats database

.PARAMETER azureStorageShareName
    The name of the file share which contains the QlikView log files

.PARAMETER azureStoragePath
    The path to the folder containing QlikView log files, relative to the root of the share
    
.PARAMETER azureStorageAccountName
    The name of the Azure Storage Account that contains the targeted file share

.NOTES
  Author:         Ben Wyatt
  
#>

# Define parameters
param (
    [Parameter(Mandatory=$true)][string]$downloadPath,
    [Parameter(Mandatory=$true)][int]$logDays=0,
    [Parameter(Mandatory=$true)][string]$azureStorageAccountKey,
    [Parameter(Mandatory=$true)][string]$azureStoragePath,
    [Parameter(Mandatory=$true)][string]$azureStorageAccountName,
    [Parameter(Mandatory=$true)][string]$azureStorageShareName
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
$directory = Get-AzureStorageFile -Context $context -ShareName $azureStorageShareName -Path $azureStoragePath
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

