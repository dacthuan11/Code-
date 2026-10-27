using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;
using Nexus.UserService.Domain.Entities;
using Nexus.UserService.Application.Persistence;

namespace Nexus.UserService.Application.UseCases.RegisterSeller;

public record RegisterSellerCommand(Guid UserId, string ShopName, string Description) : IRequest<Guid>;

public class RegisterSellerHandler : IRequestHandler<RegisterSellerCommand, Guid>
{
    private readonly IUserDatabase _db;
    private readonly IEventBus _eventBus;
    public RegisterSellerHandler(IUserDatabase db, IEventBus eventBus) { _db = db; _eventBus = eventBus; }

    public async Task<Guid> Handle(RegisterSellerCommand cmd, CancellationToken ct)
    {
        var exists = await _db.SellerProfiles.AnyAsync(s => s.UserId == cmd.UserId, ct);
        if (exists) throw new InvalidOperationException("Bạn đã đăng ký shop rồi.");

        var slugExists = await _db.SellerProfiles.AnyAsync(
            s => s.ShopName.ToLower() == cmd.ShopName.ToLower(), ct);
        if (slugExists) throw new InvalidOperationException("Tên shop đã tồn tại.");

        var sellerRole = await _db.Roles.FirstOrDefaultAsync(r => r.Code == "SELLER", ct)
            ?? throw new InvalidOperationException("Role Seller không tồn tại.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

        user.ChangeRole(sellerRole.Id);

        var profile = SellerProfile.Create(cmd.UserId, cmd.ShopName, cmd.Description);
        _db.SellerProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(
            new SellerRegisteredEvent(cmd.UserId, profile.ShopName, profile.ShopSlug), ct);

        return profile.Id;
    }
}
