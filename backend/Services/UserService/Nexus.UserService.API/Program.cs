using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nexus.Shared.Messaging;
using Nexus.UserService.Application.Consumers;
using Nexus.UserService.Infrastructure.Consumers;
using Nexus.UserService.Application.UseCases.CreateUser;
using Nexus.UserService.Application.UseCases.Login;
using Nexus.UserService.Domain.Repositories;
using Nexus.UserService.Application.Persistence;
using Nexus.UserService.Infrastructure.Persistence;
using Nexus.UserService.Infrastructure.Persistence.Repositories;
using Nexus.UserService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────
// Dùng Postgres cho cả dev và prod - tránh vấn đề "works on my machine" khi switch DB
builder.Services.AddDbContext<UserDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
       .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
builder.Services.AddScoped<IUserDatabase>(sp => sp.GetRequiredService<UserDbContext>());

// ── MediatR + Validators ─────────────────────────────────────────────────
// Scan assembly của CreateUserHandler để tự động đăng ký tất cả handler trong Application project
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly));
// Scan để tự động đăng ký tất cả AbstractValidator - không cần đăng ký từng cái
builder.Services.AddValidatorsFromAssembly(typeof(CreateUserValidator).Assembly);

// ── Repositories ─────────────────────────────────────────────────────────
// Scoped = 1 instance per HTTP request, phù hợp với EF Core DbContext lifecycle
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

// ── Application services ─────────────────────────────────────────────────
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// ── Event bus + MassTransit ───────────────────────────────────────────────
// MassTransitEventBus wraps IPublishEndpoint: works with both InMemory (dev) and RabbitMQ (prod).
// In dev mode events are handled by local consumers within the same process.
builder.Services.AddScoped<IEventBus, MassTransitEventBus>();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AuctionPaymentTimeoutConsumer>();
    x.AddConsumer<UserRegisteredConsumer>();
    x.AddConsumer<OrderCancelledConsumer>();

    if (builder.Environment.IsDevelopment())
    {
        x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
    }
    else
    {
        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(builder.Configuration["RabbitMQ:Host"], builder.Configuration["RabbitMQ:VHost"], h =>
            {
                h.Username(builder.Configuration["RabbitMQ:User"]!);
                h.Password(builder.Configuration["RabbitMQ:Pass"]!);
            });
            cfg.ConfigureEndpoints(ctx);
        });
    }
});

// ── JWT Authentication ────────────────────────────────────────────────────
// Cấu hình validation cho JWT token đến từ client.
// ValidateAudience = false vì microservice pattern không enforce audience chặt.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,       // bỏ qua audience check
            ValidateLifetime = true,        // check token chưa hết hạn
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("USER.LIST",   p => p.RequireAuthenticatedUser().RequireRole("ADMIN"));
    opt.AddPolicy("USER.UPDATE", p => p.RequireAuthenticatedUser());
    opt.AddPolicy("USER.DELETE", p => p.RequireAuthenticatedUser().RequireRole("ADMIN"));
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── CORS cho FE React ─────────────────────────────────────────────────────
// Chỉ allow localhost:5173 (Vite dev server) - production cần cấu hình lại
builder.Services.AddCors(opt => opt.AddPolicy("AllowFrontend",
    p => p.WithOrigins("http://localhost:5173", "http://localhost:5174").AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── Auto migrate DB khi startup ───────────────────────────────────────────
// Tiện cho dev - không cần chạy `dotnet ef database update` tay.
// Production nên cân nhắc dùng migration script riêng thay vì auto migrate.
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<UserDbContext>().Database.MigrateAsync();

// Swagger chỉ bật ở dev - không expose API docs ra production
app.UseSwagger(); app.UseSwaggerUI();

// Thứ tự middleware QUAN TRỌNG: CORS → Auth → Authorization → Controllers
app.UseCors("AllowFrontend");
app.UseAuthentication(); // phải trước UseAuthorization
app.UseAuthorization();
app.MapControllers();

// Health check endpoint - cho load balancer hoặc orchestrator (k8s) probe
app.MapGet("/health", () => Results.Ok(new { service = "UserService", status = "Healthy" }));

app.Run();
