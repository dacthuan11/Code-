using MediatR;

namespace Nexus.UserService.Application.UseCases.Login;

/// <summary>
/// Command đăng nhập - chỉ cần email + password.
/// Giữ simple, không cần thêm gì (device info, IP, etc.) ở tầng này.
/// </summary>
public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

/// <summary>
/// Kết quả login trả về cho client.
/// AccessToken: JWT ngắn hạn (60 phút) để gọi API.
/// RefreshToken: token dài hạn để xin AccessToken mới - hiện tại chưa store ở server side.
/// Role: client cần biết role để render UI phù hợp (ẩn/hiện các menu).
/// </summary>
public record LoginResult(string AccessToken, string RefreshToken, Guid UserId, string Role, string Email, string DisplayName);
