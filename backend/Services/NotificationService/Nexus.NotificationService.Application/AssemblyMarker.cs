namespace Nexus.NotificationService.Application;

/// <summary>
/// Lớp marker rỗng - chỉ dùng để lấy Assembly reference khi đăng ký MediatR,
/// FluentValidation, và MassTransit consumers (AddConsumers scan theo Assembly này).
/// Không có logic gì ở đây, đừng thêm gì vào.
/// </summary>
public sealed class AssemblyMarker { }
