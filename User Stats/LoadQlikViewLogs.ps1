<#
.DESCRIPTION
  Extracts data from a folder of QlikView Server log files (session and audit logs) and loads them into a database.

  All parameters are required.

.PARAMETER logFolderPath
    The full path to the folder to search for QlikView log files
    
.PARAMETER sinceDate
    The first day of files to parse
    
.PARAMETER pgsqlServer
    The PostgreSQL Server hosting the user stats database
    
.PARAMETER pgsqlDatabase
    The name of the user stats database logs should be loaded into
    
.PARAMETER pgsqlUser
    The PostgreSQL user who will make the database connection
    
.PARAMETER pgsqlPassword
    The password for $pgsqlUser

.NOTES
  Author:         Ben Wyatt
  
.EXAMPLE
  <Example goes here. Repeat this attribute for more than one example>
#>

# Define parameters
param (
    [Parameter(Mandatory=$true)][string]$logFolderPath,
    [Parameter(Mandatory=$true)][DateTime]$sinceDate<#,
    [Parameter(Mandatory=$true)][string]$pgsqlServer,
    [Parameter(Mandatory=$true)][string]$pgsqlDatabase,
    [Parameter(Mandatory=$true)][string]$pgsqlUser,
    [Parameter(Mandatory=$true)][string]$pgsqlPassword#>
)

# Set up environment
$sessionInsertFilePath = "$env:temp\sessionInsert.sql"
$auditInsertFilePath = "$env:temp\auditInsert.sql"

if (Test-Path $auditInsertFilePath)
{
    Remove-Item $auditInsertFilePath
}
 New-Item $auditInsertFilePath



# Identify files to be loaded

$fileList = $logFolderPath | Get-ChildItem | where {$_.LastWriteTime -gt $sinceDate}
$auditFileList = $fileList | where {$_.Name -like "Audit*.log"}
$sessionFileList = $fileList | where {$_.Name -like "Session*.log"}

# Extract records from files

# Load session file entries to be inserted
if ($sessionFileList.Count -gt 0)
{

    # Initialize file with INSERT statement
    write-output "Preparing session log insert statement"
    $BeginQuery = "INSERT INTO public.`"QlikViewSession`"(`"Timestamp`", `"Document`", `"ExitReason`", `"SessionStartTime`", `"SessionDuration`", `"SessionEndTime`", `"Username`", `"CalType`", `"Browser`", `"Session`", `"LogFileName`", `"LogFileLineNumber`") "
    $BeginQuery | set-content $sessionInsertFilePath -Force

    $sessionValues = ""
    $firstValue = $true

    foreach ($file in $sessionFileList)
    {
        $sessions = Import-CsV $file.FullName -Delimiter "`t"

        foreach ($session in $sessions)
        {
            if ($firstValue) # This approach allows us to ensure we're working with the first overall values, regardless of which file or line number it is
            {
                $sessionValues += "`r`n`r`n " # the first set of values should stand alone
                $firstValue = $false
            }
            else
            {
                $sessionValues += "`r`n`r`n ," # Subsequent values should be preceded by a comma
            }

           $duration = New-TimeSpan -seconds ($session.'Session Duration' * 86400) # This is a temporary workaround for a QlikView 12 bug: https://qliksupport.force.com/articles/000055500
           $sessionValues += "('$($session.Timestamp)', '$($session.Document)', '$($session.ExitReason)', '$($session.'Session Start')', '$duration', '', '$($session.'Authenticated user')', '$($session.'Cal Type')', '', '$($session.Session)', '$($file.Name)', $($sessions.IndexOf($session)))"
        }
    }

    write-output "writing insert statements to file"
    $sessionValues | Add-Content $sessionInsertFilePath -Force

    # Finalize file with ON CONFLICT [...] DO NOTHING statement
    write-output "finaliznig query"
    $EndQuery = "`r`n ON CONFLICT ON CONSTRAINT `"UNIQUE_QVsession_LogFileName_LogFileLine`" DO NOTHING"
    $EndQuery | Add-Content $sessionInsertFilePath -Force
}

# Load audit file entries to be inserted
if ($auditFileList.Count -gt 0)
{
    # Initialize file with INSERT statement
    write-output "Preparing audit log insert statement"
    $BeginQuery = "INSERT INTO public.`"QlikViewAudit`" (`"Session`", `"Timestamp`", `"Document`", `"EventType`", `"Message`", `"LogFileName`", `"LogFileLineNumber`") VALUES"
    $BeginQuery | set-content $auditInsertFilePath -Force

    $auditValues = ""
    $firstValue = $true

    foreach ($file in $auditFileList)
    {
        $audits = Import-CsV $file.FullName -Delimiter "`t"

        foreach ($audit in $audits)
        {
            if ($firstValue) # This approach allows us to ensure we're working with the first overall values, regardless of which file or line number it is
            {
                $auditValues += "`r`n`r`n " # the first set of values should stand alone
                $firstValue = $false
            }
            else
            {
                $auditValues += "`r`n`r`n ," # Subsequent values should be preceded by a comma
            }

            $auditValues += "($($audit.Session), '$($audit.Timestamp)', '$($audit.Document)', '$($audit.Type)', '$($audit.Message.Replace('''', ''))', '$($file.Name)', $($audits.IndexOf($audit)))"
        }

    }

    write-output "writing insert statements to file"
    $auditValues | Add-Content $auditInsertFilePath -Force

    # Finalize file with ON CONFLICT [...] DO NOTHING statement
    write-output "finaliznig query"
    $EndQuery = "`r`n ON CONFLICT ON CONSTRAINT `"UNIQUE_QVAudit_LogFileName_LogFileLine`" DO NOTHING"
    $EndQuery | Add-Content $auditInsertFilePath -Force

}

# Load into database


# Delete temp files

#remove-item $sessionInsertFilePath
#remove-item $auditInsertFilePath

notepad $sessionInsertFilePath
