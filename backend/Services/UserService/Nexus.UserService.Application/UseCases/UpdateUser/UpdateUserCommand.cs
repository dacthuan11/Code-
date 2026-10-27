using MediatR;

namespace Nexus.UserService.Application.UseCases.UpdateUser;

public record UpdateUserCommand(
    Guid UserId,
    string FullName,
    string Gender,
    string Address,
    DateTime DateOfBirth,
    string Email,
    string? AvatarUrl = null,
    string? PhoneNumber = null
) : IRequest<Unit>;
