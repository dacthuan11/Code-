FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Shared/Nexus.Shared/Nexus.Shared.csproj", "Shared/Nexus.Shared/"]
COPY ["Services/NotificationService/Nexus.NotificationService.Domain/Nexus.NotificationService.Domain.csproj", "Services/NotificationService/Nexus.NotificationService.Domain/"]
COPY ["Services/NotificationService/Nexus.NotificationService.Application/Nexus.NotificationService.Application.csproj", "Services/NotificationService/Nexus.NotificationService.Application/"]
COPY ["Services/NotificationService/Nexus.NotificationService.Infrastructure/Nexus.NotificationService.Infrastructure.csproj", "Services/NotificationService/Nexus.NotificationService.Infrastructure/"]
COPY ["Services/NotificationService/Nexus.NotificationService.API/Nexus.NotificationService.API.csproj", "Services/NotificationService/Nexus.NotificationService.API/"]
RUN dotnet restore "Services/NotificationService/Nexus.NotificationService.API/Nexus.NotificationService.API.csproj"
COPY . .
RUN dotnet build "Services/NotificationService/Nexus.NotificationService.API/Nexus.NotificationService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/NotificationService/Nexus.NotificationService.API/Nexus.NotificationService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexus.NotificationService.API.dll"]
