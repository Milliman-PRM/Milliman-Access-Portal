# Run the following commands consecutively to build a successful image:
# dotnet publish -c Release -o out
# podman build -f Dockerfile -t reverse-proxy out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
RUN apt-get update && apt-get install iputils-ping curl
WORKDIR /app
COPY ./out .
ENTRYPOINT ["dotnet", "ContainerReverseProxy.dll"]
