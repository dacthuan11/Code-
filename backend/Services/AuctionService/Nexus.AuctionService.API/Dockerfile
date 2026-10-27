FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Shared/Nexus.Shared/Nexus.Shared.csproj", "Shared/Nexus.Shared/"]
COPY ["Services/AuctionService/Nexus.AuctionService.Domain/Nexus.AuctionService.Domain.csproj", "Services/AuctionService/Nexus.AuctionService.Domain/"]
COPY ["Services/AuctionService/Nexus.AuctionService.Application/Nexus.AuctionService.Application.csproj", "Services/AuctionService/Nexus.AuctionService.Application/"]
COPY ["Services/AuctionService/Nexus.AuctionService.Infrastructure/Nexus.AuctionService.Infrastructure.csproj", "Services/AuctionService/Nexus.AuctionService.Infrastructure/"]
COPY ["Services/AuctionService/Nexus.AuctionService.API/Nexus.AuctionService.API.csproj", "Services/AuctionService/Nexus.AuctionService.API/"]
RUN dotnet restore "Services/AuctionService/Nexus.AuctionService.API/Nexus.AuctionService.API.csproj"
COPY . .
RUN dotnet build "Services/AuctionService/Nexus.AuctionService.API/Nexus.AuctionService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/AuctionService/Nexus.AuctionService.API/Nexus.AuctionService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexus.AuctionService.API.dll"]
