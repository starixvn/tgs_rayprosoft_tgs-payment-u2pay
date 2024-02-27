#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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