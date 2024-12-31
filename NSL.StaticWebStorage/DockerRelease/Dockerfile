FROM mcr.microsoft.com/dotnet/aspnet:8.0
USER root
WORKDIR .
COPY . /app

WORKDIR /app

ENTRYPOINT ["dotnet", "NSL.StaticWebStorage.dll"]