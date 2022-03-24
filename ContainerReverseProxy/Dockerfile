# Run the following commands consecutively to build a successful image:
# dotnet publish -c Release -o out
# podman build -f Dockerfile -t reverse-proxy out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
RUN apt-get update && apt-get install -y iputils-ping curl lsof vim
WORKDIR /app
COPY ./out .
ENTRYPOINT ["dotnet", "ContainerReverseProxy.dll"]
