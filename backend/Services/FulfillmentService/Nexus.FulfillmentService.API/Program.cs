// ============================================================
// FulfillmentService - quản lý tồn kho và giao hàng
// ============================================================
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Nexus.FulfillmentService.Application.BackgroundJobs;
using Nexus.FulfillmentService.Application.UseCases.CreateShipment;
using Nexus.FulfillmentService.Domain.Repositories;
using Nexus.FulfillmentService.Infrastructure.Adapters;
using Nexus.FulfillmentService.Infrastructure.Adapters.Ghn;
using Nexus.FulfillmentService.Infrastructure.Persistence.Repositories;
using Nexus.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nexus.FulfillmentService.Application.Persistence;
using Nexus.FulfillmentService.Infrastructure.Persistence;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
// Dùng Postgres qua Npgsql - connection string trong appsettings.json / env var
builder.Services.AddDbContext<FulfillmentDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IFulfillmentDatabase>(sp => sp.GetRequiredService<FulfillmentDbContext>());

// ── MediatR + FluentValidation ────────────────────────────────────────────────
// Dùng AssemblyMarker để scan đúng assembly Application, không scan tất cả
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
    typeof(Nexus.FulfillmentService.Application.AssemblyMarker).Assembly));

builder.Services.AddValidatorsFromAssembly(
    typeof(Nexus.FulfillmentService.Application.AssemblyMarker).Assembly);

// ── Repositories ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();

// ── GHN config ───────────────────────────────────────────────────────────────
builder.Services.Configure<GhnSettings>(builder.Configuration.GetSection("GHN"));
builder.Services.AddHttpClient<GhnClient>();

// Dùng GHN thật khi UseRealGhn=true trong config, còn lại dùng Stub để dev offline
var useRealGhn = builder.Configuration.GetValue<bool>("UseRealGhn");
if (useRealGhn)
    builder.Services.AddScoped<IShippingCarrier, GhnShippingCarrier>();
else
    builder.Services.AddScoped<IShippingCarrier, StubShippingCarrier>();

// ── Event bus ─────────────────────────────────────────────────────────────────
// InMemoryEventBus là wrapper đơn giản, MassTransit handle pub/sub thật
builder.Services.AddScoped<IEventBus, InMemoryEventBus>();
builder.Services.AddMassTransit(x =>
{
    // Scan tất cả consumers trong assembly Application (OrderPaid, OrderCancelled, AuctionWon)
    x.AddConsumers(typeof(Nexus.FulfillmentService.Application.AssemblyMarker).Assembly);

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

// Background job: tự động release reservation hết hạn mỗi 60 giây
builder.Services.AddHostedService<ReservationExpiryJob>();

// ── JWT Authentication ────────────────────────────────────────────────────────
// Dùng chung JWT key với các service khác - tất cả verify bằng symmetric key
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,   // Không validate audience cho đơn giản
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// ── Authorization Policies ────────────────────────────────────────────────────
builder.Services.AddAuthorization(opt =>
{
    // Chỉ SELLER và ADMIN mới được quản lý inventory/shipment
    opt.AddPolicy("INVENTORY.MANAGE", p => p.RequireAuthenticatedUser().RequireRole("SELLER", "ADMIN"));
    // Xem shipment thì user nào cũng xem được (kể cả BUYER)
    opt.AddPolicy("SHIPMENT.VIEW", p => p.RequireAuthenticatedUser());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(opt => opt.AddPolicy("AllowFrontend",
    p => p.WithOrigins("http://localhost:5173", "http://localhost:5174").AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── Auto migrate khi khởi động ────────────────────────────────────────────────
// Tiện cho dev nhưng production nên dùng migration script riêng
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>().Database.MigrateAsync();
}

app.UseSwagger(); app.UseSwaggerUI();

// Thứ tự middleware quan trọng: CORS → Auth → AuthZ → Routing
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint - dùng để load balancer ping kiểm tra service còn sống không
app.MapGet("/health", () => Results.Ok(new { service = "FulfillmentService", status = "Healthy" }));

app.Run();
