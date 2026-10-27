using MediatR;
using Nexus.UserService.Application.UseCases.Login;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Application.UseCases.RefreshToken;

public record RefreshTokenCommand(string Token) : IRequest<RefreshTokenResult?>;

public record RefreshTokenResult(string AccessToken, string RefreshToken);

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult?>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenService _jwt;

    public RefreshTokenHandler(IUserRepository users, IRefreshTokenRepository refreshTokens, IJwtTokenService jwt)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _jwt = jwt;
    }

    public async Task<RefreshTokenResult?> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var existing = await _refreshTokens.GetByTokenAsync(cmd.Token, ct);
        if (existing is null || !existing.IsValid) return null;

        var user = await _users.GetByIdWithRoleAsync(existing.UserId, ct);
        if (user is null || user.IsDeleted) return null;

        // Revoke old token (rotation)
        existing.Revoke();
        await _refreshTokens.SaveChangesAsync(ct);

        // Issue new tokens
        var newAccess = _jwt.GenerateAccessToken(user);
        var newRefreshRaw = _jwt.GenerateRefreshToken();
        var newRefresh = Domain.Entities.RefreshToken.Create(user.Id, newRefreshRaw, DateTime.UtcNow.AddDays(30));
        await _refreshTokens.AddAsync(newRefresh, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        return new RefreshTokenResult(newAccess, newRefreshRaw);
    }
}
