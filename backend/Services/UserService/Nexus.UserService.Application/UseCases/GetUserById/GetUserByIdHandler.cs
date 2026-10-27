using MediatR;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Application.UseCases.GetUserById;

public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserDetailDto?>
{
    private readonly IUserRepository _userRepo;

    public GetUserByIdHandler(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<UserDetailDto?> Handle(GetUserByIdQuery query, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdWithRoleAsync(query.UserId, ct);
        if (user is null) return null;

        return new UserDetailDto(
            user.Id, user.FullName, user.Email, user.PhoneNumber,
            user.Gender, user.Address, user.DateOfBirth,
            user.TrustLevel, user.ReputationScore,
            user.Role?.Code ?? "UNKNOWN",
            user.AvatarUrl,
            user.RoleId);
    }
}
