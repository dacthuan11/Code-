FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Shared/Nexus.Shared/Nexus.Shared.csproj", "Shared/Nexus.Shared/"]
COPY ["Services/CommerceService/Nexus.CommerceService.Domain/Nexus.CommerceService.Domain.csproj", "Services/CommerceService/Nexus.CommerceService.Domain/"]
COPY ["Services/CommerceService/Nexus.CommerceService.Application/Nexus.CommerceService.Application.csproj", "Services/CommerceService/Nexus.CommerceService.Application/"]
COPY ["Services/CommerceService/Nexus.CommerceService.Infrastructure/Nexus.CommerceService.Infrastructure.csproj", "Services/CommerceService/Nexus.CommerceService.Infrastructure/"]
COPY ["Services/CommerceService/Nexus.CommerceService.API/Nexus.CommerceService.API.csproj", "Services/CommerceService/Nexus.CommerceService.API/"]
RUN dotnet restore "Services/CommerceService/Nexus.CommerceService.API/Nexus.CommerceService.API.csproj"
COPY . .
RUN dotnet build "Services/CommerceService/Nexus.CommerceService.API/Nexus.CommerceService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/CommerceService/Nexus.CommerceService.API/Nexus.CommerceService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexus.CommerceService.API.dll"]
