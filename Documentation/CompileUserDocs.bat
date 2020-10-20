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

REM Copy all of the necessary files over
xcopy /I /D /Y "_CSS" "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\_CSS"
xcopy /I /D /Y "_JS" "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\_JS"
copy "..\MillimanAccessPortal\MillimanAccessPortal\src\images\map-logo-white.svg" "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\images\"

echo Compiling user guides
pandoc --metadata=pagetitle:"Content" -o "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\Content.html" "Content.html" --template=".\_TEMPLATES\user_guide_pandoc_template.html"
pandoc --metadata=pagetitle:"Account Settings" -o "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\AccountSettings.html" "AccountSettings.html" --template=".\_TEMPLATES\user_guide_pandoc_template.html"
pandoc --metadata=pagetitle:"Content Access Admin" -o "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\ContentAccessAdmin.html" "ContentAccessAdmin.html" --template=".\_TEMPLATES\user_guide_pandoc_template.html"
pandoc --metadata=pagetitle:"Content Publishing" -o "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\ContentPublishing.html" "ContentPublishing.html" --template=".\_TEMPLATES\user_guide_pandoc_template.html"
pandoc --metadata=pagetitle:"Client Access Review" -o "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\ClientAccessReview.html" "ClientAccessReview.html" --template=".\_TEMPLATES\user_guide_pandoc_template.html"
pandoc --metadata=pagetitle:"Client Admin" -o "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\ClientAdmin.html" "ClientAdmin.html" --template=".\_TEMPLATES\user_guide_pandoc_template.html"
pandoc --metadata=pagetitle:"File Drop" -o "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\FileDrop.html" "FileDrop.html" --template=".\_TEMPLATES\user_guide_pandoc_template.html"
pandoc --metadata=pagetitle:"System Admin" -o "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\SystemAdmin.html" "SystemAdmin.html" --template=".\_TEMPLATES\user_guide_pandoc_template.html"

pandoc --metadata=pagetitle:"File Drop Admin" -o "..\MillimanAccessPortal\MillimanAccessPortal\wwwroot\Documentation\FileDropAdmin.html" "FileDropAdmin.html" "FileDrop.html" --template=".\_TEMPLATES\user_guide_pandoc_template.html"
