FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Server/ Server/
COPY Client/data/ Client/data/
RUN dotnet publish Server/MMORPG.Server.csproj -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/ .
USER app
ENTRYPOINT ["dotnet", "MMORPG.Server.dll"]
