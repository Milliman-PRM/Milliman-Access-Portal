# Run the following commands consecutively to build a successful image:
# dotnet publish -c Release -o out
# podman build -f Dockerfile -t reverse-proxy out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
RUN apt-get update && apt-get install -y iputils-ping curl lsof vim
WORKDIR /app
COPY ./out .

ENTRYPOINT ["/bin/sh", "-c", "echo 172.16.3.132    map-uat-app.trafficmanager.net >> /etc/hosts dotnet ContainerReverseProxy.dll"]
