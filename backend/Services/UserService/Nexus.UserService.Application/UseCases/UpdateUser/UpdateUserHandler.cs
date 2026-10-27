using MediatR;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Application.UseCases.UpdateUser;

public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly IUserRepository _userRepo;
    private readonly IEventBus _eventBus;

    public UpdateUserHandler(IUserRepository userRepo, IEventBus eventBus)
    {
        _userRepo = userRepo;
        _eventBus = eventBus;
    }

    public async Task<Unit> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        user.Update(cmd.FullName, cmd.Gender, cmd.Address, cmd.DateOfBirth, cmd.Email, cmd.AvatarUrl, cmd.PhoneNumber);
        await _userRepo.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(new UserUpdatedEvent(user.Id, user.FullName), ct);

        return Unit.Value;
    }
}
