#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM hub.lumigame.com/base/dotnet/core/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.GameFundTransfer/Tgs.GameFundTransfer.csproj", "Tgs.GameFundTransfer/"]
COPY ["Tgs.GameFundTransfer.Common/Tgs.GameFundTransfer.Common.csproj", "Tgs.GameFundTransfer.Common/"]
COPY ["Tgs.GameFundTransfer.Core/Tgs.GameFundTransfer.Core.csproj", "Tgs.GameFundTransfer.Core/"]
COPY ["Tgs.GameFundTransfer.Repositories/Tgs.GameFundTransfer.Repositories.csproj", "Tgs.GameFundTransfer.Repositories/"]
RUN dotnet restore "./Tgs.GameFundTransfer/Tgs.GameFundTransfer.csproj"
COPY . .
WORKDIR "/src/Tgs.GameFundTransfer"
RUN dotnet build "./Tgs.GameFundTransfer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.GameFundTransfer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.GameFundTransfer.dll"]