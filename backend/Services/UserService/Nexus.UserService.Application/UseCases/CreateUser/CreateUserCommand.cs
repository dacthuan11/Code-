using MediatR;

namespace Nexus.UserService.Application.UseCases.CreateUser;

/// <summary>
/// Command để tạo user mới (self-registration hoặc admin tạo hộ).
/// Dùng record để immutable - một khi đã dispatch thì không ai sửa được data nữa.
/// IRequest&lt;CreateUserResult&gt; báo cho MediatR biết handler của command này trả về gì.
/// </summary>
public record CreateUserCommand(
    string Email,
    string? PhoneNumber = null,
    string FullName = "",
    string? IdentifyNumber = null,   // CMND/CCCD
    string? Gender = null,
    string? Address = null,
    DateTime? DateOfBirth = null,
    string Password = ""             // plain text, sẽ được hash trong handler
) : IRequest<CreateUserResult>;

/// <summary>
/// Kết quả trả về sau khi tạo user thành công.
/// Chỉ trả về những gì client cần, không expose toàn bộ entity.
/// </summary>
public record CreateUserResult(Guid UserId, string Email, string FullName);
