# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NSL.StaticWebStorage/NSL.StaticWebStorage.csproj", "NSL.StaticWebStorage/"]
RUN dotnet nuget add source "https://nuget.twicepricegroup.com/api/Package/2774c084-9ffe-4f42-9fe0-db62dd642c91-0a0620dc-ba53-4d87-8640-30cc27968db6-7f3e440b-c431-40b5-96c7-9eb5de23af48/v3/index.json" -n "Relife87"
RUN dotnet restore "./NSL.StaticWebStorage/NSL.StaticWebStorage.csproj"
COPY . .
WORKDIR "/src/NSL.StaticWebStorage"
RUN dotnet build "./NSL.StaticWebStorage.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NSL.StaticWebStorage.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NSL.StaticWebStorage.dll"]