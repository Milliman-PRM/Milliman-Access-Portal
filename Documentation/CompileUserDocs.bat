REM ### CODE OWNERS: Michael Reisz

REM ### OBJECTIVE:
REM   	Compile User Documentation for consumption.

REM ### DEVELOPER NOTES:
REM   	* PanDoc+MikTex is installed and usable.

SETLOCAL ENABLEDELAYEDEXPANSION

REM Compile Release Notes
pandoc --self-contained --metadata=pagetitle:"Milliman Access Portal - Release Notes" -o "../MillimanAccessPortal/MillimanAccessPortal/wwwroot/Documentation/ReleaseNotes.html" "ReleaseNotes.md"

REM Compile all user guides
for %%x in (
	AuthorizedContent
	AccountSettings
	ContentAccessAdmin
	ContentPublishing
	ClientAdmin
	SystemAdmin
	) do (
		echo Compiling documentation for %%x
		
		REM echo Compiling HTML version
		REM pandoc -o "%%x_Header.html" "%%x_Header.md" 
		REM pandoc --self-contained --css=".\CSS\style.css" -B "%%x_Header.html" --toc -o "%%x.html" "%%x.yaml" "%%x.md"
		REM DEL "%%x_Header.html"

		echo Compiling PDF version
		pandoc -o "%%x_Header.tex" "%%x_Header.md"
		pandoc -s -V geometry:margin=1in --variable mainfont="Arial" --variable linkcolor="Blue" --pdf-engine=xelatex -B "%%x_Header.tex" --toc -o "../MillimanAccessPortal/MillimanAccessPortal/wwwroot/Documentation/%%x.pdf" "%%x.md"
		DEL "%%x_Header.tex"
	)
