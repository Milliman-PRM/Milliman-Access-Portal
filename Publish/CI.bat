@echo off

REM Required environment variables: GIT_BRANCH, EPHI_USERNAME, EPHI_PASSWORD

SETLOCAL ENABLEDELAYEDEXPANSION
SET publishTarget=d:\installedApplications\MAP_CI\%git_branch%
SET AppPool=MAP_CI_%git_branch%
SET MAPDBNAME=MillimanAccessPortal_CI_%git_branch%
SET MAPDBNAME_DEVELOP=MillimanAccessPortal_CI_Develop
SET LOGDBNAME=MapAuditLog_CI_%git_branch%
SET LOGDBNAME_DEVELOP=MapAuditLog_CI_Develop

REM Add the branch name to database names in connection strings
cd Milliman-access-Portal\MillimanAccessPortal
powershell -C "(Get-Content Appsettings.CI.JSON).replace('((branch_name))', '%GIT_BRANCH%') | Set-Content AppSettings.CI.JSON"

REM Test build before doing anything else
REM If this build fails, we don't want to do the following (destructive) steps
dotnet build

if !errorlevel! neq 0 (
	echo Initial test build failed!
	echo errorlevel was !errorlevel!
	exit /b !errorlevel!
)

REM Stop running application pool
%windir%\system32\inetsrv\appcmd stop apppool %AppPool%

if !errorlevel! neq 0 (
	echo Failed to stop running IIS application pool!
	echo errorlevel was !errorlevel!
	exit /b !errorlevel!
)

REM Copy database from DEVELOP branch space, if this branch doesn't have a database yet
if NOT %git_branch%==DEVELOP (
	REM This branch is not develop, so check if a database exists
	SET MAPDBFOUND=0
	SET LOGDBFOUND=1
	
	echo Branch is not develop. Checking for existing databases.
	for %%G in (psql.exe --dbname=postgres --username=%pgsql_username% --host=indy-qvtest01 --tuples-only --command="" --echo-errors) DO (
		echo Checking %G
		IF %G==%MAPDBNAME% (SET MAPDBFOUND=1)
		IF %G==%LOGDBNAME% (SET LOGDBFOUND=1)
	)
	
	if NOT MAPDBFOUND EQU 1 (
		REM Back up DEVELOP branch MAP database & restore w/ branch name
		echo Copying %MAPDBNAME_DEVELOP% to %MAPDBNAME%
		
		echo Executing backup
		pg_dump -d %MAPDBNAME_DEVELOP% -h localhost -f mapdb_develop.pgsql
		
		if !errorlevel! neq 0 (
			echo Failed to back up application database!
			echo errorlevel was !errorlevel!
			exit /b !errorlevel!
		)
		
		echo Executing restore
		pg_restore -d %MAPDBNAME% -C mapdb_develop.pgsql
		
		if !errorlevel! neq 0 (
			echo Failed to restore application database!
			echo errorlevel was !errorlevel!
			exit /b !errorlevel!
		)
		
		echo Deleting backup file
		rm mapdb_develop.pgsql
		
	) else (
		echo %MAPDBNAME% already exists. No backup/restore is necessary.
	)
	
	if NOT LOGDBFOUND EQU 1 (
		REM Back up DEVELOP branch Logging database & restore w/ branch name
		echo Copying %LOGDBNAME_DEVELOP% to %LOGDBNAME%
		
		echo Executing backup
		pg_dump -d %LOGDBNAME_DEVELOP% -h localhost -f logdb_develop.pgsql
		
		if !errorlevel! neq 0 (
			echo Failed to back up logging database!
			echo errorlevel was !errorlevel!
			exit /b !errorlevel!
		)
		
		echo Executing restore
		pg_restore -d %LOGDBNAME% -C logdb_develop.pgsql
		
		if !errorlevel! neq 0 (
			echo Failed to restore logging database!
			echo errorlevel was !errorlevel!
			exit /b !errorlevel!
		)
		
		echo Deleting backup file
		rm logdb_develop.pgsql
		
	) else (
		echo %LOGDBNAME% already exists. No backup/restore is necessary.
	)
) else (
	echo Develop branch detected. No database backup/restore is necessary.
)

REM Application database migrations
cd MillimanAccessPortal
dotnet restore
dotnet ef database update

if !errorlevel! neq 0 (
	echo Failed to update application database!
	echo errorlevel was !errorlevel!
	exit /b !errorlevel!
)

REM Logging database migrations
cd ../AuditLogLib
REM dotnet restore
dotnet ef database update

if !errorlevel! neq 0 (
	echo Failed to update logging database!
	echo errorlevel was !errorlevel!
	exit /b !errorlevel!
)

cd ../MillimanAccessPortal

REM Build & publish files
dotnet publish -o %publishTarget% 

if !errorlevel! neq 0 (
	echo Build failed!
	echo errorlevel was !errorlevel!
	exit /b !errorlevel!
)

REM Run Powershell configuration script
powershell -file "..\..\Publish\CI_Publish.ps1" -executionPolicy Bypass 

REM Output success or failure
if !errorlevel! neq 0 (
	echo Publishing failed!
	echo errorlevel was !errorlevel!
	echo Publication error status:
	type ../error.log
	dir
	exit /b !errorlevel!
)

REM Stop running application pool
%windir%\system32\inetsrv\appcmd start apppool %AppPool%

if !errorlevel! neq 0 (
	echo Failed to start IIS application pool!
	echo errorlevel was !errorlevel!
	exit /b !errorlevel!
)

echo Publishing completed successfully
cd ../../
type urls.log
