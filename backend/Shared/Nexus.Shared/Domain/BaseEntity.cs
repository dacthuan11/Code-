namespace Nexus.Shared.Domain;

/// <summary>
/// Lớp cơ sở cho tất cả các entity trong hệ thống.
/// Mọi domain entity đều kế thừa từ đây để có Id, timestamp và soft-delete.
/// </summary>
public abstract class BaseEntity
{
    // Dùng Guid thay vì int để tránh xung đột khi scale ra nhiều service
    public Guid Id { get; protected set; } = Guid.NewGuid();

    // UTC để tránh vấn đề timezone khi hệ thống deploy ở nhiều nơi
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Nullable vì entity mới tạo chưa có UpdatedAt
    public DateTime? UpdatedAt { get; protected set; }

    // Soft delete: không xóa thật, chỉ đánh dấu để audit trail còn nguyên
    public bool IsDeleted { get; protected set; }

    // Đánh dấu xóa mềm — gọi cái này thay vì xóa trực tiếp trong DB
    public void MarkDeleted() => IsDeleted = true;

    // Cập nhật timestamp mỗi khi entity thay đổi
    public void Touch() => UpdatedAt = DateTime.UtcNow;
}
