# syntax=docker/dockerfile:1
# Build di SDK 10 (global.json pin 10.0.301); runtime tetap net8 chiseled non-root (port 8080 REST + 8081 gRPC h2c).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Hosts/Azure/Wms.MasterData.Host.Azure/Wms.MasterData.Host.Azure.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080 8081
ENTRYPOINT ["dotnet", "Wms.MasterData.Host.Azure.dll"]
