#! /bin/sh

/app/certificate-tool add -f /mnt/filedroppk/SPFileDropStaging.pfx -p $1 -t $2

dotnet /app/SftpServer.dll
