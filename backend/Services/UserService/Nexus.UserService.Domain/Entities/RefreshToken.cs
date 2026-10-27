using Nexus.Shared.Domain;

namespace Nexus.UserService.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt) => new()
    {
        UserId = userId,
        Token = token,
        ExpiresAt = expiresAt,
        IsRevoked = false
    };

    public void Revoke() { IsRevoked = true; Touch(); }

    public bool IsValid => !IsRevoked && ExpiresAt > DateTime.UtcNow;
}
