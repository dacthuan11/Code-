namespace Nexus.CommerceService.Application;

// Marker class - không có logic gì cả.
// Dùng để đăng ký MediatR handlers và FluentValidation validators qua reflection:
//   cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly)
// Cách này tốt hơn là hardcode assembly name vì nếu đổi tên project thì tự update theo.
public sealed class AssemblyMarker { }
