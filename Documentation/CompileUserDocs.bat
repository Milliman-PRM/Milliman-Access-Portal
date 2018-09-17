REM ### CODE OWNERS: Michael Reisz

REM ### OBJECTIVE:
REM   	Compile User Documentation for consumption.

REM ### DEVELOPER NOTES:
REM   	* PanDoc+MikTex is installed and usable.

SETLOCAL ENABLEDELAYEDEXPANSION

REM Create the documentation directory in the wwwroot folder (if it doesn't exist)
echo Compiling Release Notes
if not exist "../MillimanAccessPortal/MillimanAccessPortal/wwwroot/Documentation/" mkdir "../MillimanAccessPortal/MillimanAccessPortal/wwwroot/Documentation/"

REM Compile Release Notes
pandoc --self-contained --css=".\_CSS\style.css" --metadata=pagetitle:"Milliman Access Portal - Release Notes" -o "../MillimanAccessPortal/MillimanAccessPortal/wwwroot/Documentation/ReleaseNotes.html" "ReleaseNotes.md"

xcopy /I /D /Y "_CSS" "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\_CSS"
xcopy /I /D /Y "_JS" "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\_JS"

echo Copying user guides
for %%x in (
	AuthorizedContent
 	AccountSettings
 	ContentAccessAdmin
 	ContentPublishing
 	ClientAdmin
 	SystemAdmin
 	) do (
 		copy ".\%%x.html" "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\"
 	)