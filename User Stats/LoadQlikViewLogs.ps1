<#
.DESCRIPTION
  Extracts data from a folder of QlikView Server log files (session and audit logs) and loads them into a database.

  All parameters are required.

.PARAMETER logFolderPath
    The full path to the folder to search for QlikView log files
    
.PARAMETER logDays
    The number of days of logs to process. Default is 0 (current day only)
    
.PARAMETER pgsqlServer
    The PostgreSQL Server hosting the user stats database
    
.PARAMETER pgsqlDatabase
    The name of the user stats database logs should be loaded into
    
.PARAMETER pgsqlUser
    The PostgreSQL user who will make the database connection
    
.PARAMETER pgsqlPassword
    The password for $pgsqlUser

.PARAMETER pgsqlExePath
    The full path to psql.exe, which the script uses to execute database queries

.NOTES
  Author:         Ben Wyatt
  
.EXAMPLE
  <Example goes here. Repeat this attribute for more than one example>
#>

# Define parameters
param (
    [Parameter(Mandatory=$true)][string]$logFolderPath,
    [Parameter(Mandatory=$true)][int]$logDays=0,
    [Parameter(Mandatory=$true)][string]$pgsqlServer,
    [Parameter(Mandatory=$true)][string]$pgsqlDatabase,
    [Parameter(Mandatory=$true)][string]$pgsqlUser,
    [Parameter(Mandatory=$true)][string]$pgsqlPassword,
    [Parameter(Mandatory=$true)][string]$psqlExePath
)

# Set up environment
$sessionInsertFilePath = "$env:temp\sessionInsert.sql"
$auditInsertFilePath = "$env:temp\auditInsert.sql"

# Reset temp files if they exist
if (Test-Path $auditInsertFilePath)
{
    Remove-Item $auditInsertFilePath
}

 if (Test-Path $sessionInsertFilePath)
 {
    Remove-Item $sessionInsertFilePath
 }

$dateSpan = New-TimeSpan -days $logDays
$sinceDate = (get-date).Subtract($dateSpan)

# Identify files to be loaded

$fileList = $logFolderPath | Get-ChildItem | where {$_.LastWriteTime -gt $sinceDate}
$auditFileList = $fileList | where {$_.Name -like "Audit*.log"}
$auditFileCount = $auditFileList.Count
$sessionFileList = $fileList | where {$_.Name -like "Session*.log"}
$sessionFileCount = $sessionFileList.Count

if (($auditFileCount -gt 0) -or ($sessionFileCount -gt 0))
{    
    $env:PGPASSWORD = $pgsqlPassword
    $env:PGSSLMODE = "require"
    $env:PGCLIENTENCODING="utf-8"
}
else
{
    write-host "(get-date) No log files were found in the indicated date range"
    return 0 # There's nothing else to do if we didn't find any files
}

# Extract records from files
write-output "$(get-date) $sessionFileCount session files found"

# Load session file entries to be inserted
if ($sessionFileCount -gt 0)
{
    $fileCounter = 1

    foreach ($file in $sessionFileList)
    {
        $sessionRecordCount = 0
        $sessionValues = ""
        $firstValue = $true

        # Initialize file with INSERT statement
        write-output "$(get-date) Preparing session log insert statement"
        $BeginQuery = "INSERT INTO public.`"QlikViewSession`"(`"Timestamp`", `"Document`", `"ExitReason`", `"SessionStartTime`", `"SessionDuration`", `"SessionEndTime`", `"Username`", `"CalType`", `"Browser`", `"Session`", `"LogFileName`", `"LogFileLineNumber`") VALUES"
        $BeginQuery | set-content $sessionInsertFilePath -Force -Encoding UTF8
        
        write-output "$(get-date) Processing session file $fileCounter of $sessionFileCount - $($file.Name) ($([math]::Round($file.length / 1MB, 2)) MB)"

        $sessions = Import-CsV $file.FullName -Delimiter "`t"

        $lineNumber = 0
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
           
            $duration = New-Timespan

            if ([TimeSpan]::TryParse($session.'Session Duration', [ref]$duration) -eq $false)
            {
                # This is a temporary workaround for a QlikView 12 bug: https://qliksupport.force.com/articles/000055500 
                # This should not execute if the bug has been fixed
                $duration = New-TimeSpan -seconds ([float]$session.'Session Duration' * 86400) 
            } 
           
           
           if ($session.'Exit Reason' -eq "Session expired after idle time")
           {
                $duration = $duration - (New-TimeSpan -minutes 30) # subtract 30 minutes when the session closed due to timeout
           }

           $sessionStartTime = Get-Date
          

           # Convert session start time to a DateTime object - Custom format is required because .NET doesn't understand the QlikView timestamp format by default.
           $dateparse = [System.DateTimeOffset]::TryParseExact( $session.'Session Start', "yyyyMMddTHHmmss.fffzzz", [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::None, [ref]$sessionStartTime)
           
           $sessionEndTime = $sessionStartTime.AddSeconds($duration.TotalSeconds)

           if ($sessionEndTime -lt $sessionStartTime)
           {
               write-output "$(get-date) ERROR: Calculated sessionEndTime is before the sessionStartTime"
               write-output "Start time was $sessionStartTime"
               write-output "End time was calculated as $sessionEndTime"
               exit -42
           }

           $sessionValues += "('$($session.Timestamp)', '$($session.Document)', '$($session.'Exit Reason')', '$($session.'Session Start')', '$duration', '$($sessionEndTime.ToString())', '$($session.'Authenticated user')', '$($session.'Cal Type')', '', '$($session.Session)', '$($file.Name)', $lineNumber)"
           $lineNumber++
        }
        
		write-output "$(get-date) $lineNumber records processed"
		$sessionRecordCount += $lineNumber
        $fileCounter++

        # Load into database
        if ($sessionRecordCount -gt 0)
        {
            write-output "$(get-date) writing session insert statements to file ($sessionRecordCount rows)"
            $sessionValues | Add-Content $sessionInsertFilePath -Force -Encoding UTF8

            # Finalize file with ON CONFLICT [...] DO NOTHING statement
            write-output "$(get-date) finalizing query"
            $EndQuery = "`r`n ON CONFLICT ON CONSTRAINT `"UNIQUE_QVSession_LogFileName_LogFileLine`" DO NOTHING"
            $EndQuery | Add-Content $sessionInsertFilePath -Force -Encoding UTF8
            
            # Load session records
            write-output "$(get-date) Loading into datbase"
            $command = "$psqlExePath --dbname='$pgsqlDatabase' --username=$pgsqlUser --host=$pgsqlServer --file=`"$sessionInsertFilePath`" --echo-errors"
            Invoke-Expression $command

            if ($LASTEXITCODE -ne 0)
            {
                write-output "$(get-date) ERROR: Failed to write QlikView session records into the database. See output above for failure details."
                return 42
            }
        }
        else
        {
            write-host "(get-date) No session records were found in this file"
        }
    }
}

# Load audit file entries to be inserted
$auditRecordCount = 0
write-output "$(get-date) $auditFileCount audit files found"
if ($auditFileCount -gt 0)
{
    $fileCounter = 1

    foreach ($file in $auditFileList)
    {
        write-output "$(get-date) Processing audit file $fileCounter of $auditFileCount - $($file.Name) ($([math]::Round($file.length / 1MB, 2)) MB)"

        $audits = Import-CsV $file.FullName -Delimiter "`t"

        # Initialize file with INSERT statement
        $BeginQuery = "INSERT INTO public.`"QlikViewAudit`" (`"Session`", `"Timestamp`", `"Document`", `"EventType`", `"Message`", `"LogFileName`", `"LogFileLineNumber`") VALUES"
        $BeginQuery | set-content $auditInsertFilePath -Force -Encoding UTF8

        $auditValues = ""
        $firstValue = $true

        $lineNumber = 0
        $validLines = 0
        $invalidLines = 0
        foreach ($audit in $audits)
        {
            # Check for invalid data cases which have been seen in the past and skip processing for such lines in the file
            if (($null -eq $audit.Document) -or ($audit.Document.StartsWith("\\") -eq $false)) {
                $invalidLines++
                $lineNumber++
                continue;
            }

            if ($firstValue) # This approach allows us to ensure we're working with the first overall values, regardless of which file or line number it is
            {
                $auditValues += "`r`n`r`n " # the first set of values should stand alone
                $firstValue = $false
            }
            else
            {
                $auditValues += "`r`n`r`n ," # Subsequent values should be preceded by a comma
            }

            $auditValues += "($($audit.Session), '$($audit.Timestamp)', '$($audit.Document)', '$($audit.Type)', '$($audit.Message.Replace('''', ''))', '$($file.Name)', $lineNumber)"
            $validLines++
            $lineNumber++
        }
        
		write-output "$(get-date) $lineNumber records processed"
        write-output "$(get-date) $validLines were valid and were added to the insert statement"
        write-output "$(get-date) $invalidLines were invalid and were ignored"
	    $auditRecordCount += $validLines

        if ($validLines -gt 0)
        {
            write-output "$(get-date) writing audit insert statements to file ($auditRecordCount rows)"
            $auditValues | Add-Content $auditInsertFilePath -Force -Encoding UTF8

            # Finalize file with ON CONFLICT [...] DO NOTHING statement
            $EndQuery = "`r`n ON CONFLICT ON CONSTRAINT `"UNIQUE_QVAudit_LogFileName_LogFileLine`" DO NOTHING"
            $EndQuery | Add-Content $auditInsertFilePath -Force -Encoding UTF8

            # Load audit records to database
            write-output "$(get-date) Loading to database"
            $command = "$psqlExePath --dbname='$pgsqlDatabase' --username=$pgsqlUser --host=$pgsqlServer --file=`"$auditInsertFilePath`" --echo-errors"
            Invoke-Expression $command

            if ($LASTEXITCODE -ne 0)
            {
                write-output "$(get-date) ERROR: Failed to write QlikView audit records into the database. See output above for failure details."
                return 42
            }
        }
        else 
        {
            Write-Output "$(get-date) No valid audit records were found in this file."
        }
        
        $fileCounter++
    }
}

$env:PGPASSWORD = ""

# Delete temp files
if (test-path $sessionInsertFilePath)
{
    remove-item $sessionInsertFilePath
}

if (test-path $auditInsertFilePath)
{
    remove-item $auditInsertFilePath
}
