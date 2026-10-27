using MediatR;

namespace Nexus.UserService.Application.UseCases.GetUserById;

public record GetUserByIdQuery(Guid UserId) : IRequest<UserDetailDto?>;

public record UserDetailDto(
    Guid Id, string FullName, string Email,
    string PhoneNumber, string Gender, string Address,
    DateTime DateOfBirth, string TrustLevel, decimal ReputationScore, string Role,
    string? AvatarUrl = null, Guid? RoleId = null);
