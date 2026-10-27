using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.UserService.Application.Persistence;

namespace Nexus.UserService.Application.UseCases.RegisterSeller;

public record SellerProfileDto(
    Guid Id, Guid UserId, string ShopName, string ShopSlug,
    string Description, string? LogoUrl, string? BannerUrl,
    bool IsVerified, double Rating, int TotalSales);

public record GetSellerProfileQuery(Guid UserId) : IRequest<SellerProfileDto?>;

public class GetSellerProfileHandler : IRequestHandler<GetSellerProfileQuery, SellerProfileDto?>
{
    private readonly IUserDatabase _db;
    public GetSellerProfileHandler(IUserDatabase db) => _db = db;

    public async Task<SellerProfileDto?> Handle(GetSellerProfileQuery q, CancellationToken ct)
    {
        var p = await _db.SellerProfiles.FirstOrDefaultAsync(s => s.UserId == q.UserId, ct);
        if (p is null) return null;
        return new SellerProfileDto(p.Id, p.UserId, p.ShopName, p.ShopSlug,
            p.Description ?? "", p.LogoUrl, p.BannerUrl, p.IsVerified, p.Rating, p.TotalSales);
    }
}

public record UpdateSellerProfileCommand(Guid UserId, string ShopName, string Description, string? LogoUrl, string? BannerUrl) : IRequest;

public class UpdateSellerProfileHandler : IRequestHandler<UpdateSellerProfileCommand>
{
    private readonly IUserDatabase _db;
    public UpdateSellerProfileHandler(IUserDatabase db) => _db = db;

    public async Task Handle(UpdateSellerProfileCommand cmd, CancellationToken ct)
    {
        var p = await _db.SellerProfiles.FirstOrDefaultAsync(s => s.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("Chưa có hồ sơ shop.");
        p.UpdateProfile(cmd.ShopName, cmd.Description, cmd.LogoUrl, cmd.BannerUrl);
        await _db.SaveChangesAsync(ct);
    }
}

public record VerifySellerCommand(Guid UserId) : IRequest;

public class VerifySellerHandler : IRequestHandler<VerifySellerCommand>
{
    private readonly IUserDatabase _db;
    public VerifySellerHandler(IUserDatabase db) => _db = db;

    public async Task Handle(VerifySellerCommand cmd, CancellationToken ct)
    {
        var p = await _db.SellerProfiles.FirstOrDefaultAsync(s => s.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy shop.");
        p.Verify();
        await _db.SaveChangesAsync(ct);
    }
}
