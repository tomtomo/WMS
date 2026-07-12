# syntax=docker/dockerfile:1
# Thin REST host Notifications: build SDK 10 (global.json), runtime net8 chiseled non-root.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Hosts/Azure/Wms.Notifications.Host.Azure/Wms.Notifications.Host.Azure.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Wms.Notifications.Host.Azure.dll"]
