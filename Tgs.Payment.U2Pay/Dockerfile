FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
EXPOSE 5000

FROM hub.lumigame.com/base/dotnet/core/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Payment.U2Pay/Tgs.Payment.U2Pay.csproj", "Tgs.Payment.U2Pay/"]
COPY ["U2Pay.Core/U2Pay.Core.csproj", "U2Pay.Core/"]
COPY ["U2Pay.Client/U2Pay.Client.csproj", "U2Pay.Client/"]
RUN dotnet restore "./Tgs.Payment.U2Pay/./Tgs.Payment.U2Pay.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.U2Pay"
RUN dotnet build "./Tgs.Payment.U2Pay.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.Payment.U2Pay.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Payment.U2Pay.dll"]