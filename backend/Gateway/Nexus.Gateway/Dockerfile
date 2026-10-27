FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Shared/Nexus.Shared/Nexus.Shared.csproj", "Shared/Nexus.Shared/"]
COPY ["Gateway/Nexus.Gateway/Nexus.Gateway.csproj", "Gateway/Nexus.Gateway/"]
RUN dotnet restore "Gateway/Nexus.Gateway/Nexus.Gateway.csproj"
COPY . .
RUN dotnet build "Gateway/Nexus.Gateway/Nexus.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gateway/Nexus.Gateway/Nexus.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexus.Gateway.dll"]
