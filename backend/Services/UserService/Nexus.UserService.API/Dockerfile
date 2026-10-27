FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Shared/Nexus.Shared/Nexus.Shared.csproj", "Shared/Nexus.Shared/"]
COPY ["Services/UserService/Nexus.UserService.Domain/Nexus.UserService.Domain.csproj", "Services/UserService/Nexus.UserService.Domain/"]
COPY ["Services/UserService/Nexus.UserService.Application/Nexus.UserService.Application.csproj", "Services/UserService/Nexus.UserService.Application/"]
COPY ["Services/UserService/Nexus.UserService.Infrastructure/Nexus.UserService.Infrastructure.csproj", "Services/UserService/Nexus.UserService.Infrastructure/"]
COPY ["Services/UserService/Nexus.UserService.API/Nexus.UserService.API.csproj", "Services/UserService/Nexus.UserService.API/"]
RUN dotnet restore "Services/UserService/Nexus.UserService.API/Nexus.UserService.API.csproj"
COPY . .
RUN dotnet build "Services/UserService/Nexus.UserService.API/Nexus.UserService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/UserService/Nexus.UserService.API/Nexus.UserService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexus.UserService.API.dll"]
