# syntax=docker/dockerfile:1
# Migration + seeding sekali jalan (ACA Job). Base aspnet (bukan runtime): Platform.Shared membawa
# FrameworkReference Microsoft.AspNetCore.App transitif — terbukti live "No frameworks were found".
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Tools/Wms.MigrationRunner/Wms.MigrationRunner.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Wms.MigrationRunner.dll"]
