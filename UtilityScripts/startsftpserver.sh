#! /bin/sh

./certificate-tool add -f /mnt/filedropshare/FileDropSP.pfx -p $1 -t $2

dotnet /app/SftpServer.dll
