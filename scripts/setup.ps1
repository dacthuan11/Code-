# ============================================================
# ProjectNexus — Setup Script
# Chạy 1 lần để tạo solution + add tất cả projects
# Yêu cầu: .NET 8 SDK
# ============================================================
Write-Host "=== Project Nexus Setup ===" -ForegroundColor Cyan

# Tạo solution
dotnet new sln -n ProjectNexus

# ── Shared ──────────────────────────────────────────────────
dotnet new classlib -n Nexus.Shared -o src/Shared/Nexus.Shared --framework net8.0
dotnet sln add src/Shared/Nexus.Shared/Nexus.Shared.csproj

# ── Gateway ─────────────────────────────────────────────────
dotnet new webapi -n Nexus.Gateway -o src/Gateway/Nexus.Gateway --framework net8.0
dotnet sln add src/Gateway/Nexus.Gateway/Nexus.Gateway.csproj
dotnet add src/Gateway/Nexus.Gateway/Nexus.Gateway.csproj package Ocelot --version 23.3.3

# ── User Service ────────────────────────────────────────────
dotnet new classlib  -n Nexus.UserService.Domain         -o src/Services/UserService/Nexus.UserService.Domain         --framework net8.0
dotnet new classlib  -n Nexus.UserService.Application    -o src/Services/UserService/Nexus.UserService.Application    --framework net8.0
dotnet new classlib  -n Nexus.UserService.Infrastructure -o src/Services/UserService/Nexus.UserService.Infrastructure --framework net8.0
dotnet new webapi    -n Nexus.UserService.API            -o src/Services/UserService/Nexus.UserService.API            --framework net8.0
dotnet sln add src/Services/UserService/Nexus.UserService.Domain/Nexus.UserService.Domain.csproj
dotnet sln add src/Services/UserService/Nexus.UserService.Application/Nexus.UserService.Application.csproj
dotnet sln add src/Services/UserService/Nexus.UserService.Infrastructure/Nexus.UserService.Infrastructure.csproj
dotnet sln add src/Services/UserService/Nexus.UserService.API/Nexus.UserService.API.csproj
# References
dotnet add src/Services/UserService/Nexus.UserService.Application/Nexus.UserService.Application.csproj reference src/Services/UserService/Nexus.UserService.Domain/Nexus.UserService.Domain.csproj
dotnet add src/Services/UserService/Nexus.UserService.Application/Nexus.UserService.Application.csproj reference src/Shared/Nexus.Shared/Nexus.Shared.csproj
dotnet add src/Services/UserService/Nexus.UserService.Infrastructure/Nexus.UserService.Infrastructure.csproj reference src/Services/UserService/Nexus.UserService.Application/Nexus.UserService.Application.csproj
dotnet add src/Services/UserService/Nexus.UserService.API/Nexus.UserService.API.csproj reference src/Services/UserService/Nexus.UserService.Infrastructure/Nexus.UserService.Infrastructure.csproj
# Packages
dotnet add src/Services/UserService/Nexus.UserService.Application/Nexus.UserService.Application.csproj package MediatR --version 12.4.0
dotnet add src/Services/UserService/Nexus.UserService.Application/Nexus.UserService.Application.csproj package FluentValidation.DependencyInjectionExtensions --version 11.9.2
dotnet add src/Services/UserService/Nexus.UserService.Infrastructure/Nexus.UserService.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Tools --version 8.0.8
dotnet add src/Services/UserService/Nexus.UserService.Infrastructure/Nexus.UserService.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.8
dotnet add src/Services/UserService/Nexus.UserService.Infrastructure/Nexus.UserService.Infrastructure.csproj package MassTransit.RabbitMQ --version 8.2.5
dotnet add src/Services/UserService/Nexus.UserService.API/Nexus.UserService.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.8

# ── Catalog Service ─────────────────────────────────────────
dotnet new classlib -n Nexus.CatalogService.Domain         -o src/Services/CatalogService/Nexus.CatalogService.Domain         --framework net8.0
dotnet new classlib -n Nexus.CatalogService.Application    -o src/Services/CatalogService/Nexus.CatalogService.Application    --framework net8.0
dotnet new classlib -n Nexus.CatalogService.Infrastructure -o src/Services/CatalogService/Nexus.CatalogService.Infrastructure --framework net8.0
dotnet new webapi   -n Nexus.CatalogService.API            -o src/Services/CatalogService/Nexus.CatalogService.API            --framework net8.0
dotnet sln add src/Services/CatalogService/Nexus.CatalogService.Domain/Nexus.CatalogService.Domain.csproj
dotnet sln add src/Services/CatalogService/Nexus.CatalogService.Application/Nexus.CatalogService.Application.csproj
dotnet sln add src/Services/CatalogService/Nexus.CatalogService.Infrastructure/Nexus.CatalogService.Infrastructure.csproj
dotnet sln add src/Services/CatalogService/Nexus.CatalogService.API/Nexus.CatalogService.API.csproj
dotnet add src/Services/CatalogService/Nexus.CatalogService.Application/Nexus.CatalogService.Application.csproj reference src/Services/CatalogService/Nexus.CatalogService.Domain/Nexus.CatalogService.Domain.csproj
dotnet add src/Services/CatalogService/Nexus.CatalogService.Application/Nexus.CatalogService.Application.csproj reference src/Shared/Nexus.Shared/Nexus.Shared.csproj
dotnet add src/Services/CatalogService/Nexus.CatalogService.Infrastructure/Nexus.CatalogService.Infrastructure.csproj reference src/Services/CatalogService/Nexus.CatalogService.Application/Nexus.CatalogService.Application.csproj
dotnet add src/Services/CatalogService/Nexus.CatalogService.API/Nexus.CatalogService.API.csproj reference src/Services/CatalogService/Nexus.CatalogService.Infrastructure/Nexus.CatalogService.Infrastructure.csproj
dotnet add src/Services/CatalogService/Nexus.CatalogService.Application/Nexus.CatalogService.Application.csproj package MediatR --version 12.4.0
dotnet add src/Services/CatalogService/Nexus.CatalogService.Application/Nexus.CatalogService.Application.csproj package FluentValidation.DependencyInjectionExtensions --version 11.9.2
dotnet add src/Services/CatalogService/Nexus.CatalogService.Infrastructure/Nexus.CatalogService.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.8
dotnet add src/Services/CatalogService/Nexus.CatalogService.Infrastructure/Nexus.CatalogService.Infrastructure.csproj package MassTransit.RabbitMQ --version 8.2.5

# ── Commerce Service ────────────────────────────────────────
dotnet new classlib -n Nexus.CommerceService.Domain         -o src/Services/CommerceService/Nexus.CommerceService.Domain         --framework net8.0
dotnet new classlib -n Nexus.CommerceService.Application    -o src/Services/CommerceService/Nexus.CommerceService.Application    --framework net8.0
dotnet new classlib -n Nexus.CommerceService.Infrastructure -o src/Services/CommerceService/Nexus.CommerceService.Infrastructure --framework net8.0
dotnet new webapi   -n Nexus.CommerceService.API            -o src/Services/CommerceService/Nexus.CommerceService.API            --framework net8.0
dotnet sln add src/Services/CommerceService/Nexus.CommerceService.Domain/Nexus.CommerceService.Domain.csproj
dotnet sln add src/Services/CommerceService/Nexus.CommerceService.Application/Nexus.CommerceService.Application.csproj
dotnet sln add src/Services/CommerceService/Nexus.CommerceService.Infrastructure/Nexus.CommerceService.Infrastructure.csproj
dotnet sln add src/Services/CommerceService/Nexus.CommerceService.API/Nexus.CommerceService.API.csproj
dotnet add src/Services/CommerceService/Nexus.CommerceService.Application/Nexus.CommerceService.Application.csproj reference src/Services/CommerceService/Nexus.CommerceService.Domain/Nexus.CommerceService.Domain.csproj
dotnet add src/Services/CommerceService/Nexus.CommerceService.Application/Nexus.CommerceService.Application.csproj reference src/Shared/Nexus.Shared/Nexus.Shared.csproj
dotnet add src/Services/CommerceService/Nexus.CommerceService.Infrastructure/Nexus.CommerceService.Infrastructure.csproj reference src/Services/CommerceService/Nexus.CommerceService.Application/Nexus.CommerceService.Application.csproj
dotnet add src/Services/CommerceService/Nexus.CommerceService.API/Nexus.CommerceService.API.csproj reference src/Services/CommerceService/Nexus.CommerceService.Infrastructure/Nexus.CommerceService.Infrastructure.csproj
dotnet add src/Services/CommerceService/Nexus.CommerceService.Application/Nexus.CommerceService.Application.csproj package MediatR --version 12.4.0
dotnet add src/Services/CommerceService/Nexus.CommerceService.Application/Nexus.CommerceService.Application.csproj package FluentValidation.DependencyInjectionExtensions --version 11.9.2
dotnet add src/Services/CommerceService/Nexus.CommerceService.Infrastructure/Nexus.CommerceService.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.8
dotnet add src/Services/CommerceService/Nexus.CommerceService.Infrastructure/Nexus.CommerceService.Infrastructure.csproj package MassTransit.RabbitMQ --version 8.2.5
dotnet add src/Services/CommerceService/Nexus.CommerceService.Infrastructure/Nexus.CommerceService.Infrastructure.csproj package Stripe.net --version 46.1.0

# ── Auction Service ─────────────────────────────────────────
dotnet new classlib -n Nexus.AuctionService.Domain         -o src/Services/AuctionService/Nexus.AuctionService.Domain         --framework net8.0
dotnet new classlib -n Nexus.AuctionService.Application    -o src/Services/AuctionService/Nexus.AuctionService.Application    --framework net8.0
dotnet new classlib -n Nexus.AuctionService.Infrastructure -o src/Services/AuctionService/Nexus.AuctionService.Infrastructure --framework net8.0
dotnet new webapi   -n Nexus.AuctionService.API            -o src/Services/AuctionService/Nexus.AuctionService.API            --framework net8.0
dotnet sln add src/Services/AuctionService/Nexus.AuctionService.Domain/Nexus.AuctionService.Domain.csproj
dotnet sln add src/Services/AuctionService/Nexus.AuctionService.Application/Nexus.AuctionService.Application.csproj
dotnet sln add src/Services/AuctionService/Nexus.AuctionService.Infrastructure/Nexus.AuctionService.Infrastructure.csproj
dotnet sln add src/Services/AuctionService/Nexus.AuctionService.API/Nexus.AuctionService.API.csproj
dotnet add src/Services/AuctionService/Nexus.AuctionService.Application/Nexus.AuctionService.Application.csproj reference src/Services/AuctionService/Nexus.AuctionService.Domain/Nexus.AuctionService.Domain.csproj
dotnet add src/Services/AuctionService/Nexus.AuctionService.Application/Nexus.AuctionService.Application.csproj reference src/Shared/Nexus.Shared/Nexus.Shared.csproj
dotnet add src/Services/AuctionService/Nexus.AuctionService.Infrastructure/Nexus.AuctionService.Infrastructure.csproj reference src/Services/AuctionService/Nexus.AuctionService.Application/Nexus.AuctionService.Application.csproj
dotnet add src/Services/AuctionService/Nexus.AuctionService.API/Nexus.AuctionService.API.csproj reference src/Services/AuctionService/Nexus.AuctionService.Infrastructure/Nexus.AuctionService.Infrastructure.csproj
dotnet add src/Services/AuctionService/Nexus.AuctionService.Application/Nexus.AuctionService.Application.csproj package MediatR --version 12.4.0
dotnet add src/Services/AuctionService/Nexus.AuctionService.Application/Nexus.AuctionService.Application.csproj package FluentValidation.DependencyInjectionExtensions --version 11.9.2
dotnet add src/Services/AuctionService/Nexus.AuctionService.Infrastructure/Nexus.AuctionService.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.8
dotnet add src/Services/AuctionService/Nexus.AuctionService.Infrastructure/Nexus.AuctionService.Infrastructure.csproj package MassTransit.RabbitMQ --version 8.2.5
dotnet add src/Services/AuctionService/Nexus.AuctionService.Infrastructure/Nexus.AuctionService.Infrastructure.csproj package StackExchange.Redis --version 2.8.16

# ── Fulfillment Service ─────────────────────────────────────
dotnet new classlib -n Nexus.FulfillmentService.Domain         -o src/Services/FulfillmentService/Nexus.FulfillmentService.Domain         --framework net8.0
dotnet new classlib -n Nexus.FulfillmentService.Application    -o src/Services/FulfillmentService/Nexus.FulfillmentService.Application    --framework net8.0
dotnet new classlib -n Nexus.FulfillmentService.Infrastructure -o src/Services/FulfillmentService/Nexus.FulfillmentService.Infrastructure --framework net8.0
dotnet new webapi   -n Nexus.FulfillmentService.API            -o src/Services/FulfillmentService/Nexus.FulfillmentService.API            --framework net8.0
dotnet sln add src/Services/FulfillmentService/Nexus.FulfillmentService.Domain/Nexus.FulfillmentService.Domain.csproj
dotnet sln add src/Services/FulfillmentService/Nexus.FulfillmentService.Application/Nexus.FulfillmentService.Application.csproj
dotnet sln add src/Services/FulfillmentService/Nexus.FulfillmentService.Infrastructure/Nexus.FulfillmentService.Infrastructure.csproj
dotnet sln add src/Services/FulfillmentService/Nexus.FulfillmentService.API/Nexus.FulfillmentService.API.csproj
dotnet add src/Services/FulfillmentService/Nexus.FulfillmentService.Application/Nexus.FulfillmentService.Application.csproj reference src/Services/FulfillmentService/Nexus.FulfillmentService.Domain/Nexus.FulfillmentService.Domain.csproj
dotnet add src/Services/FulfillmentService/Nexus.FulfillmentService.Application/Nexus.FulfillmentService.Application.csproj reference src/Shared/Nexus.Shared/Nexus.Shared.csproj
dotnet add src/Services/FulfillmentService/Nexus.FulfillmentService.Infrastructure/Nexus.FulfillmentService.Infrastructure.csproj reference src/Services/FulfillmentService/Nexus.FulfillmentService.Application/Nexus.FulfillmentService.Application.csproj
dotnet add src/Services/FulfillmentService/Nexus.FulfillmentService.API/Nexus.FulfillmentService.API.csproj reference src/Services/FulfillmentService/Nexus.FulfillmentService.Infrastructure/Nexus.FulfillmentService.Infrastructure.csproj
dotnet add src/Services/FulfillmentService/Nexus.FulfillmentService.Application/Nexus.FulfillmentService.Application.csproj package MediatR --version 12.4.0
dotnet add src/Services/FulfillmentService/Nexus.FulfillmentService.Infrastructure/Nexus.FulfillmentService.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.8
dotnet add src/Services/FulfillmentService/Nexus.FulfillmentService.Infrastructure/Nexus.FulfillmentService.Infrastructure.csproj package MassTransit.RabbitMQ --version 8.2.5

# ── Notification Service ────────────────────────────────────
dotnet new classlib -n Nexus.NotificationService.Domain         -o src/Services/NotificationService/Nexus.NotificationService.Domain         --framework net8.0
dotnet new classlib -n Nexus.NotificationService.Application    -o src/Services/NotificationService/Nexus.NotificationService.Application    --framework net8.0
dotnet new classlib -n Nexus.NotificationService.Infrastructure -o src/Services/NotificationService/Nexus.NotificationService.Infrastructure --framework net8.0
dotnet new webapi   -n Nexus.NotificationService.API            -o src/Services/NotificationService/Nexus.NotificationService.API            --framework net8.0
dotnet sln add src/Services/NotificationService/Nexus.NotificationService.Domain/Nexus.NotificationService.Domain.csproj
dotnet sln add src/Services/NotificationService/Nexus.NotificationService.Application/Nexus.NotificationService.Application.csproj
dotnet sln add src/Services/NotificationService/Nexus.NotificationService.Infrastructure/Nexus.NotificationService.Infrastructure.csproj
dotnet sln add src/Services/NotificationService/Nexus.NotificationService.API/Nexus.NotificationService.API.csproj
dotnet add src/Services/NotificationService/Nexus.NotificationService.Application/Nexus.NotificationService.Application.csproj reference src/Services/NotificationService/Nexus.NotificationService.Domain/Nexus.NotificationService.Domain.csproj
dotnet add src/Services/NotificationService/Nexus.NotificationService.Application/Nexus.NotificationService.Application.csproj reference src/Shared/Nexus.Shared/Nexus.Shared.csproj
dotnet add src/Services/NotificationService/Nexus.NotificationService.Infrastructure/Nexus.NotificationService.Infrastructure.csproj reference src/Services/NotificationService/Nexus.NotificationService.Application/Nexus.NotificationService.Application.csproj
dotnet add src/Services/NotificationService/Nexus.NotificationService.API/Nexus.NotificationService.API.csproj reference src/Services/NotificationService/Nexus.NotificationService.Infrastructure/Nexus.NotificationService.Infrastructure.csproj
dotnet add src/Services/NotificationService/Nexus.NotificationService.Application/Nexus.NotificationService.Application.csproj package MediatR --version 12.4.0
dotnet add src/Services/NotificationService/Nexus.NotificationService.Infrastructure/Nexus.NotificationService.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.8
dotnet add src/Services/NotificationService/Nexus.NotificationService.Infrastructure/Nexus.NotificationService.Infrastructure.csproj package MassTransit.RabbitMQ --version 8.2.5
dotnet add src/Services/NotificationService/Nexus.NotificationService.Infrastructure/Nexus.NotificationService.Infrastructure.csproj package SendGrid --version 9.29.3

Write-Host "=== Done! Run 'dotnet build' to verify ===" -ForegroundColor Green
