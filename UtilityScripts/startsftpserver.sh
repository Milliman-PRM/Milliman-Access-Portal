#! /bin/sh

/app/certificate-tool add --file /mnt/filedroppk/SPFileDrop.pfx --password $azCertPass

dotnet /app/SftpServer.dll
