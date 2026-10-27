FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Shared/Nexus.Shared/Nexus.Shared.csproj", "Shared/Nexus.Shared/"]
COPY ["Services/FulfillmentService/Nexus.FulfillmentService.Domain/Nexus.FulfillmentService.Domain.csproj", "Services/FulfillmentService/Nexus.FulfillmentService.Domain/"]
COPY ["Services/FulfillmentService/Nexus.FulfillmentService.Application/Nexus.FulfillmentService.Application.csproj", "Services/FulfillmentService/Nexus.FulfillmentService.Application/"]
COPY ["Services/FulfillmentService/Nexus.FulfillmentService.Infrastructure/Nexus.FulfillmentService.Infrastructure.csproj", "Services/FulfillmentService/Nexus.FulfillmentService.Infrastructure/"]
COPY ["Services/FulfillmentService/Nexus.FulfillmentService.API/Nexus.FulfillmentService.API.csproj", "Services/FulfillmentService/Nexus.FulfillmentService.API/"]
RUN dotnet restore "Services/FulfillmentService/Nexus.FulfillmentService.API/Nexus.FulfillmentService.API.csproj"
COPY . .
RUN dotnet build "Services/FulfillmentService/Nexus.FulfillmentService.API/Nexus.FulfillmentService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/FulfillmentService/Nexus.FulfillmentService.API/Nexus.FulfillmentService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexus.FulfillmentService.API.dll"]
