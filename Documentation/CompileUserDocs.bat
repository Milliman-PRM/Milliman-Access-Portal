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
copy "..\MillimanAccessPortal\MillimanAccessPortal\src\images\map-logo-white.svg" "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\images\"

echo Compiling user guides
for %%x in (
	Content
 	AccountSettings
 	ContentAccessAdmin
 	ContentPublishing
 	ClientAdmin
	FileDrop
 	SystemAdmin
 	) do (
		pandoc --metadata=pagetitle:"%%x" -o "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\%%x.html" "%%x.html" --template=".\_TEMPLATES\user_guide_pandoc_template.html"
 	)

pandoc --metadata=pagetitle:"File Drop Admin" -o "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\FileDropAdmin.html" "FileDropAdmin.html" "FileDrop.html" --template=".\_TEMPLATES\user_guide_pandoc_template.html"
