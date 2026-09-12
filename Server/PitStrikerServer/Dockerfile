# Multi-stage build for PitStriker Cloud Dedicated Server
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

# Copy project files
COPY Server/PitStrikerServer/PitStrikerServer.csproj Server/PitStrikerServer/
COPY pitstricker/Assets/_Project/Scripts/Networking/Shared/ pitstricker/Assets/_Project/Scripts/Networking/Shared/

WORKDIR /src/Server/PitStrikerServer
RUN dotnet restore PitStrikerServer.csproj

COPY Server/PitStrikerServer/ .
RUN dotnet publish PitStrikerServer.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime image - Alpine Linux for minimal memory & disk footprint (~65MB)
FROM mcr.microsoft.com/dotnet/runtime:10.0-preview-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV PORT=7777
ENV HOST=0.0.0.0
ENV ENVIRONMENT=Production
ENV LOG_LEVEL=Info

EXPOSE 7777

ENTRYPOINT ["dotnet", "PitStrikerServer.dll"]
