# Code Owners: Ben Wyatt, Steve Gredell

### OBJECTIVE:
#  Run configuration steps for CI builds of Milliman Access Portal

### DEVELOPER NOTES:
#

function log_statement {
    Param([string]$statement)

    $datestring = get-date -Format "yyyy-MM-dd HH:mm:ss"

    write-output $datestring"|"$statement
}

$branchName = $env:GIT_BRANCH
$ci_username = $env:pool_username
$ci_password = $env:pool_password

$branchFolder = "D:\installedapplications\map_ci\$branchName\"
$AppPool = "MAP_CI_$branchName"
$MAPDBNAME = "MillimanAccessPortal_CI_$branchName".ToLower()
$MAPDBNAME_DEVELOP = "millimanaccessportal_ci_develop"
$LOGDBNAME = "MapAuditLog_CI_$branchName".ToLower()
$LOGDBNAME_DEVELOP = "mapauditlog_ci_develop"
$ASPNETCORE_ENVIRONMENT = "CI"
$PublishURL = "http://indy-qvtest01/$appPool"

# Set environment variable (utilized by dotnet commands)
$env:ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT


log_statement "Adding the branch name to database names in connection strings"

cd MillimanAccessPortal\MillimanAccessPortal

(Get-Content Appsettings.CI.JSON).replace('((branch_name))', '$branchName') | Set-Content AppSettings.CI.JSON

log_statement "Test build before publishing"
# If this build fails, we don't want to do the subsequent (destructive) steps
dotnet restore

if ($LASTEXITCODE -ne 0) {
	log_statement "ERROR: Initial package restore failed"
	log_statement "errorlevel was $LASTEXITCODE"
	exit $LASTEXITCODE
}

dotnet build

if ( $LASTEXITCODE -ne 0 ) {
	log_statement "ERROR: Initial test build failed"
	log_statement "errorlevel was $LASTEXITCODE"
	exit $LASTEXITCODE
}

log_statement "Stop running application pool"
$requestURL = "http://localhost:8042/iis_stop_pool?pool_name=$appPool"
    $requestResult = Invoke-WebRequest -Uri $requestURL | ConvertFrom-Json

    if ($requestResult.returncode -ne 0 -and $requestResult.returncode -ne 1062) {
        log_statement "ERROR: Failed to stop application pool"
        log_statement $requestResult.stdout
        #return -1
    }

if ($branchName -ne "DEVELOP") {
    log_statement "Copy databases from DEVELOP branch space, if this branch doesn't have its databases yet"
	$MAPDBFOUND=0
	$LOGDBFOUND=0

    # Check for existing databases
    $command = "'c:\program` files\postgresql\9.6\bin\psql.exe' --dbname=postgres  -h localhost --tuples-only --command=`"select datname from Pg_database`" --echo-errors"
    $output = invoke-expression "&$command"

    if ($LASTEXITCODE -ne 0) {
        $error_code = $LASTEXITCODE
        log_statement "ERROR: Failed to query for existing databases"
        log_statement "Command was: $command"
        $user = whoami
        log_statement "User is $user"
        log_statement "errorlevel was $LASTEXITCODE"
        exit $error_code
    }

    foreach ($db in $output) {
        if ($db.trim() -eq $MAPDBNAME) {
            log_statement "MAP application database found for this branch."
            $MAPDBFOUND = 1
        }
        elseif ($db.trim() -eq $LOGDBNAME) {
            log_statement "Logging database found for this branch."
           $LOGDBFOUND = 1
        }
    }

    # Create MAP application database, if necessary
	if ($MAPDBFOUND -ne 1) {
		# Back up DEVELOP branch MAP application database & restore w/ branch name
		log_statement "Copying $MAPDBNAME_DEVELOP to $MAPDBNAME"

        log_statement "Executing backup"
	    $command = "'c:\program` files\postgresql\9.6\bin\pg_dump.exe' -d $MAPDBNAME_DEVELOP -F c -h localhost -f mapdb_develop.pgsql"
        invoke-expression "&$command"

	    if ($LASTEXITCODE -ne 0) {
        $error_code = $LASTEXITCODE
        log_statement "ERROR: Failed to back up application database"
        log_statement "Command was: $command"
        $user = whoami
        log_statement "User is $user"
		    log_statement "errorlevel was $LASTEXITCODE"
		    exit $error_code
	    }

	    log_statement "Creating application database"
	    $command = "'c:\program` files\postgresql\9.6\bin\psql.exe' -d postgres -h localhost -e -q --command=`"create database $MAPDBNAME`""
        invoke-expression "&$command"

	    if ($LASTEXITCODE -ne 0) {
        $error_code = $LASTEXITCODE
        log_statement "ERROR: Failed to create application database"
        log_statement "Command was: $command"
        $user = whoami
        log_statement "User is $user"
		    log_statement "errorlevel was $LASTEXITCODE"
		    exit $error_code
	    }

		log_statement "Executing restore"
		$command = "'c:\program` files\postgresql\9.6\bin\pg_restore.exe' -h localhost -d $MAPDBNAME mapdb_develop.pgsql"
        invoke-expression "&$command"

		if ($LASTEXITCODE -ne 0) {
      $error_code = $LASTEXITCODE
      log_statement "ERROR: Failed to restore application database"
      log_statement "Command was: $command"
      $user = whoami
      log_statement "User is $user"
			log_statement "errorlevel was $LASTEXITCODE"
			exit $error_code
		}

		log_statement "Deleting backup file"
		rm mapdb_develop.pgsql

	}
  else {
		log_statement "$MAPDBNAME already exists. No backup/restore is necessary."
	}

	if ($LOGDBFOUND -ne 1) {
		# Back up DEVELOP branch Logging database & restore w/ branch name
		log_statement "Copying $LOGDBNAME_DEVELOP to $LOGDBNAME"

		log_statement "Executing backup"
		$command = "'c:\program` files\postgresql\9.6\bin\pg_dump.exe' -d $LOGDBNAME_DEVELOP -F c -h localhost -f logdb_develop.pgsql"
        invoke-expression "&$command"

		if ($LASTEXITCODE -ne 0) {
      $error_code = $LASTEXITCODE
      log_statement "ERROR: Failed to back up logging database"
      log_statement "Command was: $command"
      $user = whoami
      log_statement "User is $user"
			log_statement "errorlevel was $LASTEXITCODE"
			exit $error_code
		}

		log_statement "Creating logging database"
		$command = "'c:\program` files\postgresql\9.6\bin\psql.exe' -d postgres -h localhost -e -q --command=`"create database $LOGDBNAME`""
        invoke-expression "&$command"

		if ($LASTEXITCODE -ne 0) {
      $error_code = $LASTEXITCODE
      log_statement "ERROR: Failed to create logging database"
      log_statement "Command was: $command"
      $user = whoami
      log_statement "User is $user"
			log_statement "errorlevel was $LASTEXITCODE"
			exit $error_code
		}

		log_statement "Executing restore"
		$command = "'c:\program` files\postgresql\9.6\bin\pg_restore.exe' -d $LOGDBNAME -h localhost logdb_develop.pgsql"
        invoke-expression "&$command"

		if ($LASTEXITCODE -ne 0) {
      $error_code = $LASTEXITCODE
			log_statement "ERROR: Failed to restore logging database"
      log_statement "Command was: $command"
      $user = whoami
      log_statement "User is $user"
			log_statement "errorlevel was $LASTEXITCODE"
			exit $error_code
		}

		log_statement "Deleting backup file"
		rm logdb_develop.pgsql

	}
    else {
		log_statement "$LOGDBNAME already exists. No backup/restore is necessary."
	}
}
else {
	log_statement "Develop branch detected. No database backup/restore is necessary."
}

log_statement "Performing application database migrations"

dotnet ef database update

if ($LASTEXITCODE -ne 0) {
	log_statement "ERROR: Failed to update application database"
	log_statement "errorlevel was $LASTEXITCODE"
	exit $LASTEXITCODE
}

log_statement "Performing logging database migrations"
cd ../AuditLogLib
dotnet ef database update

if ($LASTEXITCODE -ne 0) {
	log_statement "ERROR: Failed to update logging database"
	log_statement "errorlevel was $LASTEXITCODE"
	exit $LASTEXITCODE
}

cd ../MillimanAccessPortal

log_statement "Build and publish application files"
dotnet publish -o $branchFolder

if ($LASTEXITCODE -ne 0) {
	log_statement "Build failed"
	log_statement "errorlevel was $LASTEXITCODE"
	exit $LASTEXITCODE
}


# (Re-)create applications
try
{

    $name = "MAP_CI_$branchName"

    # Create application pool if it doesn't already exist
    $requestURL = "http://localhost:8042/iis_create_pool?pool_name=$name"
    $requestResult = Invoke-WebRequest -Uri $requestURL | ConvertFrom-Json

    if ($requestResult.returncode -eq 183) {
        log_statement "Application pool already exists."
    }
    elseif ($requestResult.returncode -ne 0) {
        log_statement "ERROR: Failed to create application pool"
        log_statement $requestResult.stdout
        exit -1
    }
    elseif ($requestResult.returncode -eq 0) {
        # This step should only be performed when the application pool is initially created
        # Configure Application Pool credentials
        # Configuring credentials must be done separately from creating the application pool
        $requestURL = "http://localhost:8042/iis_create_pool?pool_name=$name&username=$ci_username&password=$ci_password"
        $requestResult = Invoke-WebRequest -Uri $requestURL | ConvertFrom-Json

        if ($requestResult.returncode -ne 0) {
            log_statement "ERROR: Failed to configure application pool credentials"
            log_statement $requestResult.stdout
            exit -1
        }
    }

    # If the web application already exists, remove it
    log_statement "Remove existing web application (if any)"
    New-WebApplication -Name $name -PhysicalPath $branchFolder -Site "Default Web Site" -ApplicationPool "$name"
    $requestURL = "http://localhost:8042/iis_delete_app?app_name=$name&action=$delete"
    $requestResult = Invoke-WebRequest -Uri $requestURL | ConvertFrom-Json

    # Return code 50 indicates the app doesn't currently exist. That's fine in this case.
    if ($requestResult.returncode -ne 0 -and $requestResult.returncode -ne 50) {
        log_statement "ERROR: Failed to create the web application"
        log_statement $requestResult.stdout
        exit -1
    }

    # Create web application
    log_statement "Creating web application"
    $requestURL = "http://localhost:8042/iis_create_app?app_name=$name&pool_name=$name&folder_path=$branchFolder"
    $requestResult = Invoke-WebRequest -Uri $requestURL | ConvertFrom-Json

    if ($requestResult.returncode -ne 0) {
        log_statement "ERROR: Failed to create the web application"
        log_statement $requestResult.stdout
        exit -1
    }

    # Configure Application Pool ASPNETCORE_ENVIRONMENT variable
    log_statement "Configuring ASPNETCORE_ENVIRONMENT variable"
    $requestURL = "http://localhost:8042/iis_set_env?app_name=$name&env_variable_name=ASPNETCORE_ENVIRONMENT&env_variable_value=$ASPNETCORE_ENVIRONMENT"
    $requestResult = Invoke-WebRequest -Uri $requestURL | ConvertFrom-Json

    if ($requestResult.returncode -ne 0) {
        log_statement "ERROR: Failed to configure application environment variable"
        log_statement $requestResult.stdout
        exit -1
    }

    # Stop Pool
    log_statement "Stopping application pool to reset it"
    $requestURL = "http://localhost:8042/iis_stop_pool?pool_name=$name"
    $requestResult = Invoke-WebRequest -Uri $requestURL | ConvertFrom-Json

    if ($requestResult.returncode -ne 0 -and $requestResult.returncode -ne 1062) {
        log_statement "ERROR: Failed to stop application pool"
        log_statement $requestResult.stdout
        exit -1
    }

    # Start Pool
    log_statement "Final application pool startup"
    $requestURL = "http://localhost:8042/iis_start_pool?pool_name=$name"
    $requestResult = Invoke-WebRequest -Uri $requestURL | ConvertFrom-Json

    if ($requestResult.returncode -ne 0) {
        log_statement "ERROR: Failed to start application pool"
        log_statement $requestResult.stdout
        exit -1
    }

    log_statement "SUCCESS: Published to " ($urlBase + "/" + $name + "/")
}
catch [Exception]
{
    log_statement "ERROR: Publishing failed"
    log_statement "Last request URL: $requestURL"
    $_.Exception | format-list -force
    exit -1
}
