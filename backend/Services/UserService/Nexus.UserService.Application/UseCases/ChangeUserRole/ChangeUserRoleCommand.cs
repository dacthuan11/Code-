using MediatR;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Application.UseCases.ChangeUserRole;

public record ChangeUserRoleCommand(Guid UserId, Guid RoleId) : IRequest;

public class ChangeUserRoleHandler : IRequestHandler<ChangeUserRoleCommand>
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;

    public ChangeUserRoleHandler(IUserRepository userRepo, IRoleRepository roleRepo)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
    }

    public async Task Handle(ChangeUserRoleCommand cmd, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(cmd.UserId, ct)
            ?? throw new InvalidOperationException($"User {cmd.UserId} không tồn tại.");

        var role = await _roleRepo.GetByIdAsync(cmd.RoleId, ct)
            ?? throw new InvalidOperationException($"Role {cmd.RoleId} không tồn tại.");

        user.ChangeRole(role.Id);
        await _userRepo.SaveChangesAsync(ct);
    }
}
