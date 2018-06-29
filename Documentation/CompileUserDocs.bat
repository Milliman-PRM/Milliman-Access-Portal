REM ### CODE OWNERS: Michael Reisz

REM ### OBJECTIVE:
REM   	Compile User Documentation for consumption.

REM ### DEVELOPER NOTES:
REM   	* PanDoc+MikTex is installed and usable.

SETLOCAL ENABLEDELAYEDEXPANSION

REM Create the documentation directory in the wwwroot folder (if it doesn't exist)
if not exist "../MillimanAccessPortal/MillimanAccessPortal/wwwroot/Documentation/" mkdir "../MillimanAccessPortal/MillimanAccessPortal/wwwroot/Documentation/"

REM Compile Release Notes
pandoc --self-contained --metadata=pagetitle:"Milliman Access Portal - Release Notes" -o "../MillimanAccessPortal/MillimanAccessPortal/wwwroot/Documentation/ReleaseNotes.html" "ReleaseNotes.md"

Compile all user guides
for %%x in (
	AuthorizedContent
	AccountSettings
	ContentAccessAdmin
	ContentPublishing
	ClientAdmin
	SystemAdmin
	) do (
		echo Compiling documentation for %%x
		pandoc -o "%%x\%%x_Header.html" "%%x\%%x_Header.md" 
		pandoc --self-contained --css=".\_CSS\style.css" -B "%%x\%%x_Header.html" --toc -o "../MillimanAccessPortal/MillimanAccessPortal/wwwroot/Documentation/%%x.html" "%%x\%%x.yaml" "%%x\%%x.md"
		DEL "%%x\%%x_Header.html"
	)