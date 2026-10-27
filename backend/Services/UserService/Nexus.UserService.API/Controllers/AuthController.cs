using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.UserService.Application.UseCases.Login;
using Nexus.UserService.Application.UseCases.RefreshToken;
using Nexus.UserService.Application.UseCases.VerifyEmail;

namespace Nexus.UserService.API.Controllers;

/// <summary>
/// Controller xử lý authentication: login, logout, forgot password.
/// Tách riêng khỏi UsersController vì đây là auth flow, không phải CRUD user.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Đăng nhập bằng email + password, trả về JWT access token.
    /// Không cần auth vì đây là điểm vào đầu tiên.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand cmd)
    {
        try
        {
            var result = await _mediator.Send(cmd);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest req)
    {
        var success = await _mediator.Send(new VerifyEmailCommand(req.Token));
        if (!success) return BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn." });
        return Ok(new { message = "Email xác thực thành công! Bạn có thể đăng nhập ngay bây giờ." });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(req.Token));
        if (result is null) return Unauthorized(new { message = "Invalid or expired refresh token." });
        return Ok(result);
    }

    /// <summary>
    /// Logout - invalidate token hiện tại.
    /// Cần [Authorize] để biết token nào đang được dùng.
    ///
    /// TODO: Chưa implement blacklist. Hiện tại JWT là stateless nên logout ở server
    /// không thực sự revoke token - token vẫn valid đến khi hết hạn.
    /// Cần implement Redis-based token blacklist để fix vấn đề này.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // Invalidate token via blacklist (implement in infrastructure)
        return NoContent();
    }

    /// <summary>
    /// Yêu cầu gửi link reset password về email.
    /// Trả về Accepted (202) thay vì 200 vì action này là async (gửi email mất thời gian).
    /// Message luôn giống nhau dù email tồn tại hay không - tránh email enumeration.
    ///
    /// TODO: Implement gửi email reset link qua NotificationService.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        // TODO: Send reset link via email
        return Accepted(new { message = "If the email exists, a reset link has been sent." });
    }
}

/// <summary>Request DTO cho forgot password - chỉ cần email</summary>
public record ForgotPasswordRequest(string Email);

public record RefreshRequest(string Token);
public record VerifyEmailRequest(string Token);
