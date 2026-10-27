using MediatR;

namespace Nexus.UserService.Application.UseCases.GetUsers;

public record GetUsersQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<GetUsersResult>;

public record GetUsersResult(List<UserSummaryDto> Items, int Total, int Page, int PageSize);

public record UserSummaryDto(
    Guid Id, string FullName, string Email,
    string PhoneNumber, string Gender, string Address,
    DateTime DateOfBirth, string TrustLevel, decimal ReputationScore,
    Guid RoleId, string RoleCode, bool IsEmailVerified,
    string? AvatarUrl = null, bool IsLocked = false);
