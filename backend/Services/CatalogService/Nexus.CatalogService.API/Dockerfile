FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Shared/Nexus.Shared/Nexus.Shared.csproj", "Shared/Nexus.Shared/"]
COPY ["Services/CatalogService/Nexus.CatalogService.Domain/Nexus.CatalogService.Domain.csproj", "Services/CatalogService/Nexus.CatalogService.Domain/"]
COPY ["Services/CatalogService/Nexus.CatalogService.Application/Nexus.CatalogService.Application.csproj", "Services/CatalogService/Nexus.CatalogService.Application/"]
COPY ["Services/CatalogService/Nexus.CatalogService.Infrastructure/Nexus.CatalogService.Infrastructure.csproj", "Services/CatalogService/Nexus.CatalogService.Infrastructure/"]
COPY ["Services/CatalogService/Nexus.CatalogService.API/Nexus.CatalogService.API.csproj", "Services/CatalogService/Nexus.CatalogService.API/"]
RUN dotnet restore "Services/CatalogService/Nexus.CatalogService.API/Nexus.CatalogService.API.csproj"
COPY . .
RUN dotnet build "Services/CatalogService/Nexus.CatalogService.API/Nexus.CatalogService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/CatalogService/Nexus.CatalogService.API/Nexus.CatalogService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexus.CatalogService.API.dll"]
