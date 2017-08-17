@echo off

REM Required environment variables: GIT_BRANCH, EPHI_USERNAME, EPHI_PASSWORD

SETLOCAL ENABLEDELAYEDEXPANSION
SET publishTarget=d:\installedApplications\MAP_CI\%git_branch%
SET AppPool=MAP_CI_%git_branch%

REM Test build before doing anything else
REM If this build fails, we don't want to do the following (destructive) steps
cd MillimanAccessPortal
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
