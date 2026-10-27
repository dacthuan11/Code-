// ============================================================
// NotificationService - gửi email, push notification cho user
// ============================================================
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nexus.NotificationService.Application.BackgroundJobs;
using Nexus.NotificationService.Domain.Repositories;
using Nexus.NotificationService.Infrastructure.Adapters;
using System.Net.Mail;
using Nexus.NotificationService.Infrastructure.Persistence;
using Nexus.NotificationService.Infrastructure.Persistence.Repositories;
using Nexus.Shared.Messaging;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
// Postgres qua Npgsql - cùng cluster với các service khác nhưng DB riêng (isolated)
builder.Services.AddDbContext<NotificationDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── MediatR + FluentValidation ────────────────────────────────────────────────
// NotificationService ít dùng MediatR (chủ yếu dùng MassTransit consumers)
// nhưng vẫn đăng ký để consistent với các service khác
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
    typeof(Nexus.NotificationService.Application.AssemblyMarker).Assembly));
builder.Services.AddValidatorsFromAssembly(
    typeof(Nexus.NotificationService.Application.AssemblyMarker).Assembly);

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// ── Email + Push adapters ─────────────────────────────────────────────────────
// Dùng SmtpEmailSender nếu Email:UseSmtp = true, ngược lại dùng Stub (chỉ log)
if (bool.TryParse(builder.Configuration["Email:UseSmtp"], out var useSmtp) && useSmtp)
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
else
    builder.Services.AddScoped<IEmailSender, StubEmailSender>();

builder.Services.AddScoped<IPushSender, StubPushSender>();

// ── Event bus ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IEventBus, InMemoryEventBus>();
builder.Services.AddMassTransit(x =>
{
    // Đăng ký tất cả consumers trong assembly Application bằng assembly scan
    // Thêm consumer mới chỉ cần tạo file, không cần đăng ký thủ công ở đây
    x.AddConsumers(typeof(Nexus.NotificationService.Application.AssemblyMarker).Assembly);

    if (builder.Environment.IsDevelopment())
        // Dev: không cần RabbitMQ thật, dùng InMemory transport
        x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
    else
        // Production: kết nối RabbitMQ thật với credentials từ config
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

// ── JWT Auth ──────────────────────────────────────────────────────────────────
// Cùng Jwt:Key và Jwt:Issuer với các service khác trong cluster
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,   // Không check audience cho đơn giản
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Background job: retry notifications thất bại mỗi 5 phút (tối đa 3 lần per notification)
builder.Services.AddHostedService<NotificationRetryJob>();

// Authorization mặc định - không có custom policy ở service này
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(opt => opt.AddPolicy("AllowFrontend",
    p => p.WithOrigins("http://localhost:5173", "http://localhost:5174").AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── Auto migrate database khi khởi động ──────────────────────────────────────
// Tiện cho dev, production nên dùng migration script riêng trong CI/CD pipeline
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.MigrateAsync();

app.UseSwagger(); app.UseSwaggerUI();

// Thứ tự middleware: CORS → Auth → Controllers
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check - load balancer dùng để kiểm tra service còn sống không
app.MapGet("/health", () => Results.Ok(new { service = "NotificationService", status = "Healthy" }));

app.Run();
