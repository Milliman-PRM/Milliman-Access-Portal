#! /bin/sh

export ASPNETCORE_ENVIRONMENT=STAGING

/app/certificate-tool add -f /mnt/filedropshare/SPFileDropStaging.pfx -p $1 -t $2

dotnet /app/SftpServer.dll
