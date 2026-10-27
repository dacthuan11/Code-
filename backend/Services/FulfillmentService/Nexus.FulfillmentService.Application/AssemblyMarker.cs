namespace Nexus.FulfillmentService.Application;

/// <summary>
/// Lớp marker rỗng - chỉ dùng để lấy Assembly reference khi đăng ký MediatR và FluentValidation.
/// Dùng typeof(AssemblyMarker).Assembly thay vì hardcode tên assembly string.
/// Không có logic gì ở đây, đừng thêm method vào đây.
/// </summary>
public sealed class AssemblyMarker { }
