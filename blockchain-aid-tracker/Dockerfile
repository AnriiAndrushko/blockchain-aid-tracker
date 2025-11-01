FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["blockchain-aid-tracker/blockchain-aid-tracker.csproj", "blockchain-aid-tracker/"]
RUN dotnet restore "blockchain-aid-tracker/blockchain-aid-tracker.csproj"
COPY . .
WORKDIR "/src/blockchain-aid-tracker"
RUN dotnet build "./blockchain-aid-tracker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./blockchain-aid-tracker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "blockchain-aid-tracker.dll"]
