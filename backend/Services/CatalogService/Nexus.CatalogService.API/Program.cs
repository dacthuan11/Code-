using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nexus.CatalogService.Application.Services;
using Nexus.CatalogService.Domain.Repositories;
using Nexus.CatalogService.Application.Persistence;
using Nexus.CatalogService.Infrastructure.Persistence;
using Nexus.CatalogService.Infrastructure.Persistence.Repositories;
using Nexus.CatalogService.Infrastructure.Services;
using Nexus.Shared.Messaging;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────
// Dùng Npgsql (PostgreSQL) - connection string lấy từ appsettings.json / env var
// Khi deploy lên container, override bằng env: ConnectionStrings__DefaultConnection
builder.Services.AddDbContext<CatalogDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ICatalogDatabase>(sp => sp.GetRequiredService<CatalogDbContext>());

// ── MediatR + Validators ──────────────────────────────────────────────────
// Scan assembly Application để tìm tất cả IRequestHandler và IValidator
// AssemblyMarker là class rỗng đánh dấu assembly này (xem AssemblyMarker.cs)
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Nexus.CatalogService.Application.AssemblyMarker).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Nexus.CatalogService.Application.AssemblyMarker).Assembly);

// ── Repositories ──────────────────────────────────────────────────────────
// Scoped vì DbContext là scoped - mỗi HTTP request có instance riêng
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// ── HttpClient: FulfillmentService (stock query) ──────────────────────────
builder.Services.AddHttpClient("FulfillmentService", c =>
    c.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Services:FulfillmentService") ?? "http://localhost:5005"));
builder.Services.AddScoped<IStockService, FulfillmentStockService>();

// ── Event bus ─────────────────────────────────────────────────────────────
// InMemoryEventBus: đơn giản, dùng khi chạy local hoặc test không cần message broker
// MassTransit: message broker thực tế, tự route message tới consumer đúng
builder.Services.AddScoped<IEventBus, InMemoryEventBus>();
builder.Services.AddMassTransit(x =>
{
    if (builder.Environment.IsDevelopment())
        // Dev: dùng in-memory transport, không cần cài RabbitMQ
        x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
    else
        // Production: kết nối RabbitMQ thật, config từ appsettings / secret
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
// Token do IdentityService phát hành, CatalogService chỉ validate (không phát)
// ValidateAudience = false vì multi-service, mỗi service không cần check audience
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,   // không check audience - xem comment trên
            ValidateLifetime = true,    // check hết hạn token
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("PRODUCT.CREATE", p => p.RequireAuthenticatedUser().RequireRole("SELLER", "ADMIN"));
    opt.AddPolicy("PRODUCT.UPDATE", p => p.RequireAuthenticatedUser().RequireRole("SELLER", "ADMIN"));
    opt.AddPolicy("PRODUCT.DELETE", p => p.RequireAuthenticatedUser().RequireRole("SELLER", "ADMIN"));
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: chỉ cho phép FE dev server (localhost:5173 là Vite default port)
// Prod sẽ cần thêm domain thật vào đây
builder.Services.AddCors(opt => opt.AddPolicy("AllowFrontend",
    p => p.WithOrigins("http://localhost:5173", "http://localhost:5174").AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Auto-migrate khi khởi động - tiện cho dev, nên cân nhắc bỏ ở production
// Prod thường chạy migration trong CI/CD pipeline riêng, không để app tự migrate
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();

// Swagger chỉ mở trên Dev để tránh expose API docs ra ngoài
app.UseSwagger(); app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseAuthentication(); // phải đặt trước UseAuthorization
app.UseAuthorization();
app.MapControllers();

// Health check endpoint - được API Gateway hoặc load balancer ping định kỳ
app.MapGet("/health", () => Results.Ok(new { service = "CatalogService", status = "Healthy" }));

app.Run();
