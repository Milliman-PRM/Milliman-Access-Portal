<#
    .SYNOPSIS
        Prepare the staging database to run migrations
 
    .DESCRIPTION
        Drops all existing tables from the targetDatabase and replaces them with tables (including data)
        from the sourceDatabase

        This will be used to prepare the staging environment database to run new pre-release branches,
        but it could possibly find other uses in the future

        All parameters are required

    .PARAMETER pgsqlToolsPath
        The fully-qualified path to a folder that contains the PostgreSQL command line tools
        The path is tested when the script is called

    .PARAMETER sourceDatabase
        The name of the database that will be copied to the target

    .PARAMETER sourceServer
        The FQDN of the server that hosts the source database

    .PARAMETER sourceUsername
        The username that will be used to generate the backup of sourceDatabase

    .PARAMETER sourcePassword
        The password for sourceUsername

    .PARAMETER targetDatabase
        The name of the database that will have its contents replaced

    .PARAMETER targetServer
        The FQDN of the server that hosts the target database

    .PARAMETER targetUsername
        The username that will be used to empty and restore the targetDatabase

    .PARAMETER targetPassword
        The password for targetUsername

    .NOTES
        AUTHORS - Ben Wyatt, Steve Gredell
#>

Param(
    [Parameter(Mandatory=$true)]
    [string]$sourceDatabase,
    [Parameter(Mandatory=$true)]
    [string]$sourceServer,
    [Parameter(Mandatory=$true)]
    [string]$sourceUser,
    [Parameter(Mandatory=$true)]
    [SecureString]$sourcePassword,
    [Parameter(Mandatory=$true)]
    [string]$targetDatabase,
    [Parameter(Mandatory=$true)]
    [string]$targetServer,
    [Parameter(Mandatory=$true)]
    [string]$targetUser,
    [Parameter(Mandatory=$true)]
    [SecureString]$targetPassword,
    [Parameter(Mandatory=$true)]
    [ValidateScript({test-path $_})]
    [string]$pgsqlToolsPath
)

# Build and test full paths to needed tools

$psqlPath = "$pgsqlToolsPath\psql.exe"
$pgDumpPath = "$pgsqlToolsPath\pg_dump.exe"

if (((Test-Path $psqlPath) -eq $false) -or ((test-path $pgDumpPath) -eq $false))
{
    write-error "One or more of the required tools (psql.exe, pg_dump.exe, pg_restore.exe) was not found."
    return -42
}

$env:PGSSLMODE="require"

#region Create a backup of the source database
    $env:PGPASSWORD = [System.Runtime.InteropServices.marshal]::PtrToStringAuto([System.Runtime.InteropServices.marshal]::SecureStringToBSTR($sourcePassword))
    $command = "$pgDumpPath --dbname=$sourceDatabase  -h $sourceServer -U $sourceUser --file=dumpSource.sql"
    invoke-expression "&$command"

    $env:PGPASSWORD = ""

    if ($LASTEXITCODE -ne 0)
    {
        write-error "Backing up from $sourceDatabase on $sourceServer failed."
        return -42
    }
#endregion

#region Drop all tables on the target database

    # Write drop command to a file
    "
    DO `$`$ DECLARE
        r RECORD;
    BEGIN
        FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = current_schema()) LOOP
            EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
        END LOOP;
    END `$`$;
        " | Set-Content dropCommand.txt 

                        
    $env:PGPASSWORD = [System.Runtime.InteropServices.marshal]::PtrToStringAuto([System.Runtime.InteropServices.marshal]::SecureStringToBSTR($targetPassword))
    $command = "$psqlPath --dbname=$targetDatabase  -h $targetServer -U $targetUser -f dropCommand.txt --echo-errors"
    invoke-expression "&$command" -ErrorVariable $errorOutput

    if ($errorOutput -like "*drop cascades*")
    {
        invoke-expression "&$command"
    }

    $env:PGPASSWORD = ""

    if ($LASTEXITCODE -ne 0)
    {
        write-error "Dropping tables from $targetDatabase on $targetServer failed."
        return -42
    }
#endregion 

#region Replace references to $sourceUser with $targetUser in dumpSource.sql

$trimSourceUser = $sourceUser.Substring(0,$sourceUser.IndexOf('@'))
$trimTargetUser = $targetUser.Substring(0,$targetUser.IndexOf('@'))

$sqlText = get-content dumpSource.sql

$sqlText = $sqlText.Replace($trimSourceUser,$trimTargetUser).Replace("prod_app_owners","stage_app_owners")

$sqlText | set-content dumpSource.sql

#endregion

#region Restore the backup to the target database
    $env:PGPASSWORD = [System.Runtime.InteropServices.marshal]::PtrToStringAuto([System.Runtime.InteropServices.marshal]::SecureStringToBSTR($targetPassword))
    $command = "$psqlPath --dbname=$targetDatabase  -h $targetServer -U $targetUser --file=dumpSource.sql"
    invoke-expression "&$command"

    $env:PGPASSWORD = ""
    remove-item dumpSource.sql
    if ($LASTEXITCODE -ne 0)
    {
        write-error "Restoring backup of $sourceDatabase to $targetDatabase failed."
        return -42
    }

#endregion