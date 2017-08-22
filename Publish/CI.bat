@echo off

REM Required environment variables: GIT_BRANCH, EPHI_USERNAME, EPHI_PASSWORD

SETLOCAL ENABLEDELAYEDEXPANSION
SET publishTarget=d:\installedApplications\MAP_CI\%git_branch%
SET AppPool=MAP_CI_%git_branch%
SET MAPDBNAME=MillimanAccessPortal_CI_%git_branch%
SET MAPDBNAME_DEVELOP=MillimanAccessPortal_CI_Develop
SET LOGDBNAME=MapAuditLog_CI_%git_branch%
SET LOGDBNAME_DEVELOP=MapAuditLog_CI_Develop
SET PGEXEPATH=c:\program files\postgresql\9.6\bin\

echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Adding the branch name to database names in connection strings
cd Milliman-access-Portal\MillimanAccessPortal
powershell -C "(Get-Content Appsettings.CI.JSON).replace('((branch_name))', '%GIT_BRANCH%') | Set-Content AppSettings.CI.JSON"

echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Test build before publishing
REM If this build fails, we don't want to do the following (destructive) steps
dotnet build

if !errorlevel! neq 0 (
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Initial test build failed!
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
	exit /b !errorlevel!
)

echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Stop running application pool
%windir%\system32\inetsrv\appcmd stop apppool %AppPool%

if !errorlevel! neq 0 (
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Failed to stop running IIS application pool!
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
	exit /b !errorlevel!
)

echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Copy database from DEVELOP branch space, if this branch doesn't have a database yet
if NOT %git_branch%==DEVELOP (
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: This branch is not develop, checking if a database exists
	SET MAPDBFOUND=0
	SET LOGDBFOUND=1
	
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Branch is not develop. Checking for existing databases.
	for %%G in (psql.exe --dbname=postgres --username=%ephi_username% --host=indy-qvtest01 --tuples-only --command="" --echo-errors) DO (
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Checking %G
		IF %G==%MAPDBNAME% (SET MAPDBFOUND=1)
		IF %G==%LOGDBNAME% (SET LOGDBFOUND=1)
	)
	
	if NOT MAPDBFOUND EQU 1 (
		REM Back up DEVELOP branch MAP database & restore w/ branch name
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Copying %MAPDBNAME_DEVELOP% to %MAPDBNAME%
		
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Executing backup
		%PGEXEPATH%pg_dump -d %MAPDBNAME_DEVELOP% -F c -h localhost -f mapdb_develop.pgsql
		
		if !errorlevel! neq 0 (
			echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Failed to back up application database!
			echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
			exit /b !errorlevel!
		)
		
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Creating application database
		%PGEXEPATH%psql -d postgres -e -q --command="create database %MAPDBNAME%"
		
		if !errorlevel! neq 0 (
			echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Failed to create application database!
			echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
			exit /b !errorlevel!
		)
		
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Executing restore
		%PGEXEPATH%pg_restore -d %MAPDBNAME% mapdb_develop.pgsql
		
		if !errorlevel! neq 0 (
			echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Failed to restore application database!
			echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
			exit /b !errorlevel!
		)
		
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Deleting backup file
		rm mapdb_develop.pgsql
		
	) else (
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: %MAPDBNAME% already exists. No backup/restore is necessary.
	)
	
	if NOT LOGDBFOUND EQU 1 (
		REM Back up DEVELOP branch Logging database & restore w/ branch name
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Copying %LOGDBNAME_DEVELOP% to %LOGDBNAME%
		
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Executing backup
		%PGEXEPATH%pg_dump -d %LOGDBNAME_DEVELOP% -F c -h localhost -f logdb_develop.pgsql
		
		if !errorlevel! neq 0 (
			echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Failed to back up logging database!
			echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
			exit /b !errorlevel!
		)
		
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Creating logging database
		%PGEXEPATH%psql -d postgres -e -q --command="create database %LOGDBNAME%"
		
		if !errorlevel! neq 0 (
			echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Failed to create logging database!
			echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
			exit /b !errorlevel!
		)
		
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Executing restore
		%PGEXEPATH%pg_restore -d %LOGDBNAME% -C logdb_develop.pgsql
		
		if !errorlevel! neq 0 (
			echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Failed to restore logging database!
			echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
			exit /b !errorlevel!
		)
		
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Deleting backup file
		rm logdb_develop.pgsql
		
	) else (
		echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: %LOGDBNAME% already exists. No backup/restore is necessary.
	)
) else (
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Develop branch detected. No database backup/restore is necessary.
)

echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Performing application database migrations
cd MillimanAccessPortal
dotnet restore
dotnet ef database update

if !errorlevel! neq 0 (
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Failed to update application database!
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
	exit /b !errorlevel!
)

echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Performing logging database migrations
cd ../AuditLogLib
REM dotnet restore
dotnet ef database update

if !errorlevel! neq 0 (
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Failed to update logging database!
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
	exit /b !errorlevel!
)

cd ../MillimanAccessPortal

echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Build & publish application files
dotnet publish -o %publishTarget% 

if !errorlevel! neq 0 (
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Build failed!
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
	exit /b !errorlevel!
)

echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Run Powershell configuration script
powershell -file "..\..\Publish\CI_Publish.ps1" -executionPolicy Bypass 

REM Output success or failure
if !errorlevel! neq 0 (
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Publishing failed!
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Publication error status:
	type ../error.log
	dir
	exit /b !errorlevel!
)

echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Start application pool
%windir%\system32\inetsrv\appcmd start apppool %AppPool%

if !errorlevel! neq 0 (
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Failed to start IIS application pool!
	echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: errorlevel was !errorlevel!
	exit /b !errorlevel!
)

echo %~nx0 !DATE:~-4!-!DATE:~4,2!-!DATE:~7,2! !TIME!: Publishing completed successfully
cd ../../
type urls.log
