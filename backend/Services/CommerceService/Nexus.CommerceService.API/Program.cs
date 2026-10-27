using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nexus.CommerceService.Application.UseCases.Checkout;
using Nexus.CommerceService.Domain.Repositories;
using Nexus.CommerceService.Infrastructure.Adapters;
using Nexus.CommerceService.Infrastructure.Payment;
using Nexus.CommerceService.Application.Persistence;
using Nexus.CommerceService.Infrastructure.Persistence;
using Nexus.CommerceService.Infrastructure.Persistence.Repositories;
using Nexus.Shared.Messaging;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────
// PostgreSQL qua connection string trong appsettings / environment variable
// Dùng Npgsql vì project theo chuẩn .NET microservices với Postgres
builder.Services.AddDbContext<CommerceDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ICommerceDatabase>(sp => sp.GetRequiredService<CommerceDbContext>());

// ── MediatR + Validators ──────────────────────────────────────────────────
// Scan toàn bộ Assembly của Application layer để đăng ký handlers và validators
// Dùng AssemblyMarker thay vì hardcode assembly name để tránh lỗi khi rename project
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Nexus.CommerceService.Application.AssemblyMarker).Assembly));
builder.Services.AddValidatorsFromAssembly(
    typeof(Nexus.CommerceService.Application.AssemblyMarker).Assembly);

// ── Repositories ──────────────────────────────────────────────────────────
// Scoped vì repository gắn với DbContext (cũng Scoped theo EF Core convention)
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// ── External adapters (Stub cho dev, thay bằng Stripe khi production) ─────
// Chỉ cần đổi dòng này thành AddScoped<IPaymentGateway, StripePaymentGateway>() là xong
builder.Services.AddScoped<IPaymentGateway, StubPaymentGateway>();

// VNPAY payment gateway
builder.Services.AddScoped<IVnpayGateway, VnpayPaymentGateway>();

// ── Event bus ─────────────────────────────────────────────────────────────
// InMemoryEventBus cho dev (không cần RabbitMQ chạy), RabbitMQ cho production
// MassTransit tự điều phối dựa vào environment
builder.Services.AddScoped<IEventBus, InMemoryEventBus>();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(Nexus.CommerceService.Application.AssemblyMarker).Assembly);

    if (builder.Environment.IsDevelopment())
        x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
    else
        // Production: kết nối RabbitMQ với credentials từ config
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

// ── JWT Auth ──────────────────────────────────────────────────────────────
// Validate JWT từ AuthService - tất cả services trong hệ thống share cùng secret key
// ValidateAudience = false vì nhiều service nhận cùng 1 token (không kiểm tra "aud" claim)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = false, ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Định nghĩa policies cho authorization - hiện tại chỉ yêu cầu authenticated user
// Sau này có thể thêm role/claim check ở đây mà không sửa controller
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("CART.MANAGE",    p => p.RequireAuthenticatedUser());
    opt.AddPolicy("ORDER.VIEW",     p => p.RequireAuthenticatedUser());
    opt.AddPolicy("PRODUCT.CREATE", p => p.RequireAuthenticatedUser());
    opt.AddPolicy("PRODUCT.DELETE", p => p.RequireAuthenticatedUser());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: chỉ cho phép frontend dev trên port 5173 (Vite default)
// Production sẽ cần update origin này hoặc đọc từ config
builder.Services.AddCors(opt => opt.AddPolicy("AllowFrontend",
    p => p.WithOrigins("http://localhost:5173", "http://localhost:5174").AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Auto-migrate khi startup - tiện cho dev, không cần chạy migration thủ công
// Production nên cân nhắc tách migration ra pipeline riêng để không block startup
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<CommerceDbContext>().Database.MigrateAsync();

app.UseSwagger(); app.UseSwaggerUI();

app.UseCors("AllowFrontend");

// Thứ tự middleware quan trọng: Authentication phải trước Authorization
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint - dùng cho Docker healthcheck và load balancer probe
app.MapGet("/health", () => Results.Ok(new { service = "CommerceService", status = "Healthy" }));

app.Run();
