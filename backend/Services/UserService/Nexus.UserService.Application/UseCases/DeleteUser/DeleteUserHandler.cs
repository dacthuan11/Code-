using MediatR;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Application.UseCases.DeleteUser;

/// <summary>
/// Handler xử lý soft-delete user.
/// Không xóa record khỏi DB, chỉ đánh dấu IsDeleted = true.
/// Query filter trong DbContext sẽ tự lọc user này ra khỏi mọi query sau đó.
/// </summary>
public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserRepository _userRepo;
    private readonly IEventBus _eventBus;

    public DeleteUserHandler(IUserRepository userRepo, IEventBus eventBus)
    {
        _userRepo = userRepo;
        _eventBus = eventBus;
    }

    public async Task<Unit> Handle(DeleteUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        // Tránh double-delete gây ra event trùng hoặc data inconsistency
        if (user.IsDeleted)
            throw new InvalidOperationException("User already deleted.");

        // MarkDeleted() là method từ BaseEntity - set IsDeleted=true và Touch() UpdatedAt
        user.MarkDeleted();
        await _userRepo.SaveChangesAsync(ct);

        // Publish event để các service khác dọn dẹp data liên quan:
        // CartService xóa giỏ hàng, SessionService thu hồi token, v.v.
        await _eventBus.PublishAsync(new UserDeletedEvent(user.Id), ct);

        return Unit.Value;
    }
}
