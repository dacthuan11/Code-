using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.UserService.Application.UseCases.ChangeUserRole;
using Nexus.UserService.Application.UseCases.CreateUser;
using Nexus.UserService.Application.UseCases.DeleteUser;
using Nexus.UserService.Application.UseCases.GetUserById;
using Nexus.UserService.Application.UseCases.GetUsers;
using Nexus.UserService.Application.UseCases.UpdateUser;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRoleRepository _roleRepo;
    private readonly IUserRepository _userRepo;

    public UsersController(IMediator mediator, IRoleRepository roleRepo, IUserRepository userRepo)
    {
        _mediator = mediator;
        _roleRepo = roleRepo;
        _userRepo = userRepo;
    }

    /// <summary>
    /// Lấy thông tin tài khoản hiện tại của người dùng đang đăng nhập.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentProfile()
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(currentUserIdStr) || !Guid.TryParse(currentUserIdStr, out var currentUserId))
            return Unauthorized(new { message = "Không xác định được danh tính người dùng." });

        var result = await _mediator.Send(new GetUserByIdQuery(currentUserId));
        if (result is null) return NotFound(new { message = "Không tìm thấy thông tin tài khoản." });
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách users có phân trang. Chỉ ADMIN mới được gọi.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "USER.LIST")]
    public async Task<IActionResult> GetList([FromQuery] string? search, int page = 1, int pageSize = 20)
    {
        var result = await _mediator.Send(new GetUsersQuery(search, page, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết 1 user theo ID.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value;

        if (currentUserRole != "ADMIN" && currentUserId != userId.ToString())
            return Forbid();

        var result = await _mediator.Send(new GetUserByIdQuery(userId));
        if (result is null) return NotFound(new { message = "User not found." });
        return Ok(result);
    }

    /// <summary>
    /// Tạo user mới (self-registration).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserCommand cmd,
        [FromServices] IValidator<CreateUserCommand>? validator = null)
    {
        if (validator is not null)
        {
            var valResult = await validator.ValidateAsync(cmd);
            if (!valResult.IsValid)
            {
                var errorsDict = valResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return BadRequest(new
                {
                    message = valResult.Errors.First().ErrorMessage,
                    errors = errorsDict
                });
            }
        }

        try
        {
            var result = await _mediator.Send(cmd);
            return CreatedAtAction(nameof(GetList), new { id = result.UserId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi tạo tài khoản: " + ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật thông tin profile của user.
    /// </summary>
    [HttpPut("{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid userId, [FromBody] UpdateUserRequest req)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value;

        if (currentUserRole != "ADMIN" && currentUserId != userId.ToString())
            return Forbid();

        await _mediator.Send(new UpdateUserCommand(
            userId, req.FullName, req.Gender, req.Address, req.DateOfBirth, req.Email, req.AvatarUrl, req.PhoneNumber));
        return NoContent();
    }

    /// <summary>
    /// Soft-delete user. Chỉ ADMIN mới được làm.
    /// </summary>
    [HttpDelete("{userId:guid}")]
    [Authorize(Policy = "USER.DELETE")]
    public async Task<IActionResult> Delete(Guid userId)
    {
        await _mediator.Send(new DeleteUserCommand(userId));
        return NoContent();
    }

    /// <summary>
    /// Khóa / Mở khóa tài khoản. Chỉ duy nhất ADMIN mới có quyền.
    /// </summary>
    [HttpPatch("{userId:guid}/lock")]
    [Authorize(Policy = "USER.DELETE")]
    public async Task<IActionResult> ToggleLock(Guid userId)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (currentUserIdStr == userId.ToString())
            return BadRequest(new { message = "Không thể tự khóa tài khoản của chính mình." });

        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null) return NotFound(new { message = "Tài khoản không tồn tại." });

        user.ToggleLock();
        await _userRepo.SaveChangesAsync();
        return Ok(new { isLocked = user.IsLocked, message = user.IsLocked ? "Đã khóa tài khoản thành công." : "Đã mở khóa tài khoản thành công." });
    }

    /// <summary>
    /// Đổi role cho user. Chỉ duy nhất ADMIN mới có quyền thực hiện.
    /// </summary>
    [HttpPatch("{userId:guid}/role")]
    [Authorize(Policy = "USER.DELETE")]
    public async Task<IActionResult> ChangeRole(Guid userId, [FromBody] ChangeRoleRequest req)
    {
        try
        {
            await _mediator.Send(new ChangeUserRoleCommand(userId, req.RoleId));
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách roles — dùng cho dropdown phân quyền. Chỉ ADMIN mới được gọi.
    /// </summary>
    [HttpGet("/api/v1/roles")]
    [Authorize(Policy = "USER.LIST")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleRepo.GetAllAsync();
        return Ok(roles.Select(r => new { r.Id, r.Name, r.Code }));
    }
}

public record UpdateUserRequest(
    string FullName,
    string Gender,
    string Address,
    DateTime DateOfBirth,
    string Email,
    string? AvatarUrl = null,
    string? PhoneNumber = null
);

public record ChangeRoleRequest(Guid RoleId);
