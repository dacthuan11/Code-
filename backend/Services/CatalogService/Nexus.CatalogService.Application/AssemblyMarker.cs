namespace Nexus.CatalogService.Application;

/// <summary>
/// Class rỗng chỉ để đánh dấu assembly này cho MediatR và FluentValidation.
///
/// Khi đăng ký trong Program.cs:
///   cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly)
///   builder.Services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly)
///
/// .NET reflection sẽ scan toàn bộ assembly của class này để tìm handler/validator.
/// Dùng class thay vì typeof(SomeHandler).Assembly để tránh phải đổi khi rename handler.
/// </summary>
public sealed class AssemblyMarker { }
