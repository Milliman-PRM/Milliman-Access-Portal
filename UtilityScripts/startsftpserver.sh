#! /bin/sh

/app/certificate-tool add -f /mnt/filedroppk/SPFileDrop.pfx -p $azCertPass -t $thumbprint

dotnet /app/SftpServer.dll
