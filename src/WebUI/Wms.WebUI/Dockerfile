# syntax=docker/dockerfile:1
# WebUI Blazor: satu image cloud-agnostic; Bff__GatewayAddress diinjek deploy-time (APIM di Azure).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/WebUI/Wms.WebUI/Wms.WebUI.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Wms.WebUI.dll"]
