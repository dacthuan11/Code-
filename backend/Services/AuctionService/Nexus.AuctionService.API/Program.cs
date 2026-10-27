using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nexus.AuctionService.API.Hubs;
using Nexus.AuctionService.API.Services;
using Nexus.AuctionService.Application.BackgroundJobs;
using Nexus.AuctionService.Application.Services;
using Nexus.AuctionService.Application.UseCases.PlaceBid;
using Nexus.AuctionService.Domain.Repositories;
using Nexus.AuctionService.Infrastructure.Locking;
using Nexus.AuctionService.Infrastructure.Persistence.Repositories;
using Nexus.Shared.Messaging;
using Nexus.AuctionService.Application.Persistence;
using Nexus.AuctionService.Infrastructure.Persistence;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────
// PostgreSQL thay vì SQL Server - chạy nhẹ hơn, free, phù hợp microservices trên Docker
builder.Services.AddDbContext<AuctionDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAuctionDatabase>(sp => sp.GetRequiredService<AuctionDbContext>());

// ── MediatR - tự scan tất cả handlers trong Application assembly ──────────
// Dùng AssemblyMarker thay vì hardcode assembly name - refactor-safe
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
    typeof(Nexus.AuctionService.Application.AssemblyMarker).Assembly));

// ── FluentValidation - scan validators cùng assembly với handlers ─────────
builder.Services.AddValidatorsFromAssembly(
    typeof(Nexus.AuctionService.Application.AssemblyMarker).Assembly);

// ── Repositories ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();

// ── Distributed lock: InMemory khi dev (không cần Redis), Redis khi production
// IMPORTANT: thay InMemoryDistributedLock bằng RedisDistributedLock trước khi deploy nhiều instance
builder.Services.AddSingleton<IDistributedLock, InMemoryDistributedLock>();

// ── Event bus ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IEventBus, InMemoryEventBus>();

// MassTransit: InMemory cho dev (zero config), RabbitMQ cho production
// Tách biệt theo environment - không cần file config riêng cho từng env
builder.Services.AddMassTransit(x =>
{
    if (builder.Environment.IsDevelopment())
        x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
    else
        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(builder.Configuration["RabbitMQ:Host"], builder.Configuration["RabbitMQ:VHost"], h =>
            {
                h.Username(builder.Configuration["RabbitMQ:User"]!);
                h.Password(builder.Configuration["RabbitMQ:Pass"]!);
            });
            cfg.ConfigureEndpoints(ctx);
        });
});

// ── JWT Authentication ────────────────────────────────────────────────────
// ValidateAudience = false vì internal microservice không cần audience check
// Shared symmetric key với các service khác (đơn giản cho học tập, production nên dùng RSA)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
        // SignalR không thể send Authorization header — token qua query string
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

// ── Authorization Policies ────────────────────────────────────────────────
// Claim-based policies thay vì role middleware - flexible và explicit hơn
builder.Services.AddAuthorization(opt =>
{
    // BID: BUYER, SELLER (để seller test bid), ADMIN đều được bid
    // Việc chặn seller bid auction của chính mình là business rule trong domain (Auction.PlaceBid)
    opt.AddPolicy("AUCTION.BID", p => p.RequireAuthenticatedUser().RequireRole("BUYER", "SELLER", "ADMIN"));

    // MANAGE: chỉ SELLER (tạo/kết thúc auction mình) và ADMIN
    opt.AddPolicy("AUCTION.MANAGE", p => p.RequireAuthenticatedUser().RequireRole("SELLER", "ADMIN"));
});

builder.Services.AddHostedService<AuctionScheduler>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IAuctionNotifier, AuctionNotifierImpl>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — cần AllowCredentials cho SignalR WebSocket handshake
builder.Services.AddCors(opt => opt.AddPolicy("AllowFrontend",
    p => p.WithOrigins("http://localhost:5173", "http://localhost:5174")
          .AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

var app = builder.Build();

// Auto-migrate khi khởi động - tiện cho dev/staging
// Production nên tách migration thành init container hoặc CI/CD step riêng
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AuctionDbContext>().Database.MigrateAsync();
}

app.UseSwagger(); app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization(); // Authentication phải trước Authorization - thứ tự middleware QUAN TRỌNG
app.MapControllers();
app.MapHub<AuctionHub>("/hubs/auction");
app.MapHub<FlashSaleHub>("/hubs/flashsale");

app.MapGet("/health", () => Results.Ok(new { service = "AuctionService", status = "Healthy" }));

app.Run();
