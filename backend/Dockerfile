FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/TrustVault.API/TrustVault.API.csproj", "src/TrustVault.API/"]
COPY ["src/TrustVault.Core/TrustVault.Core.csproj", "src/TrustVault.Core/"]
COPY ["src/TrustVault.Infrastructure/TrustVault.Infrastructure.csproj", "src/TrustVault.Infrastructure/"]
RUN dotnet restore "src/TrustVault.API/TrustVault.API.csproj"
COPY . .
WORKDIR "/src/src/TrustVault.API"
RUN dotnet build "TrustVault.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TrustVault.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TrustVault.API.dll"]
