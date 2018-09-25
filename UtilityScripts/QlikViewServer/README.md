# QlikView Server Utility Scripts

These files are will enable the QlikView server to automatically scale documents to fit the window on load.  These files need to be placed on the QlikView Server in specific locations:

opendoc.htm         ->  C:\Program Files\QlikView\Server\QlikViewClients\QlikViewAjax (overwrite the existing file)
ScaleToFit.js       ->  C:\Program Files\QlikView\Server\QlikViewClients\QlikViewAjax\htc
map-overrides.css   ->  C:\Program Files\QlikView\Server\QlikViewClients\QlikViewAjax\htc

These scripts should only be required when an update to the QlikView Server has been performed.
