using Microsoft.EntityFrameworkCore;
using Nexus.NotificationService.Domain.Entities;

namespace Nexus.NotificationService.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext cho NotificationService.
/// Hai bảng: Notifications (main data) và EventLogs (audit trail).
/// Enum properties lưu dạng string để dễ đọc khi query thẳng vào DB.
/// </summary>
public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<EventLog> EventLogs => Set<EventLog>(); // Audit log, không bao giờ delete

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            // Lưu enum dạng string thay int: "Email"/"Push" thay vì 0/1
            // Dễ đọc hơn khi query thủ công, không bị ảnh hưởng nếu thêm enum value
            e.Property(n => n.Channel).HasConversion<string>();
            e.Property(n => n.Status).HasConversion<string>();
            // Index trên UserId vì GetByUserIdAsync query theo UserId rất thường xuyên
            e.HasIndex(n => n.UserId);
        });

        mb.Entity<EventLog>(e =>
        {
            e.HasKey(l => l.Id);
            // Dùng jsonb (Postgres native JSON) thay varchar để:
            // 1. Có thể query bên trong JSON nếu cần sau này
            // 2. Postgres tự validate format JSON khi insert
            e.Property(l => l.Payload).HasColumnType("jsonb");
        });
    }
}
