using MediatR;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Application.UseCases.GetUsers;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, GetUsersResult>
{
    private readonly IUserRepository _userRepo;
    public GetUsersHandler(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<GetUsersResult> Handle(GetUsersQuery query, CancellationToken ct)
    {
        var (items, total) = await _userRepo.GetListAsync(query.Search, query.Page, query.PageSize, ct);

        var dtos = items.Select(u => new UserSummaryDto(
            u.Id, u.FullName, u.Email, u.PhoneNumber,
            u.Gender, u.Address, u.DateOfBirth, u.TrustLevel, u.ReputationScore,
            u.RoleId, u.Role?.Code ?? "BUYER", u.IsEmailVerified,
            u.AvatarUrl, u.IsLocked
        )).ToList();

        return new GetUsersResult(dtos, total, query.Page, query.PageSize);
    }
}
