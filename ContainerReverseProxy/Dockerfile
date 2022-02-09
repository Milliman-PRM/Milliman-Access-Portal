# Run the following commands consecutively to build a successful image:
# dotnet publish -c Release -o out
# podman build -f Dockerfile -t reverse-proxy out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY . .
ENV ASPNETCORE_ENVIRONMENT="azure-UAT"
ENTRYPOINT ["dotnet", "ContainerReverseProxy.dll"]