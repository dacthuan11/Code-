using MediatR;

namespace Nexus.UserService.Application.UseCases.DeleteUser;

/// <summary>
/// Command xóa user - thực ra là soft delete (IsDeleted = true), không xóa hẳn khỏi DB.
/// Lý do: cần giữ lại data để audit, và các order/transaction liên quan vẫn còn tham chiếu.
/// </summary>
public record DeleteUserCommand(Guid UserId) : IRequest<Unit>;
