using Nexus.Shared.Domain;

namespace Nexus.UserService.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string IdentifyNumber { get; private set; } = default!;
    public string Gender { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public DateTime DateOfBirth { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = default!;
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiry { get; private set; }
    public decimal ReputationScore { get; private set; } = 100;
    public string TrustLevel { get; private set; } = "STANDARD";
    public string? AvatarUrl { get; private set; }
    public bool IsLocked { get; private set; } = false;

    private User() { }

    public static User Create(string email, string phone, string fullName,
        string identifyNumber, string gender, string address,
        DateTime dob, string passwordHash, Guid roleId)
    {
        return new User
        {
            Email = email,
            PhoneNumber = phone,
            FullName = fullName,
            IdentifyNumber = identifyNumber,
            Gender = gender,
            Address = address,
            DateOfBirth = dob,
            PasswordHash = passwordHash,
            RoleId = roleId
        };
    }

    public void Update(string fullName, string gender, string address, DateTime dob, string email, string? avatarUrl = null, string? phone = null)
    {
        FullName = fullName;
        Gender = gender;
        Address = address;
        DateOfBirth = dob;
        Email = email;
        if (avatarUrl is not null) AvatarUrl = avatarUrl;
        if (!string.IsNullOrWhiteSpace(phone)) PhoneNumber = phone;
        Touch();
    }

    public void ToggleLock()
    {
        IsLocked = !IsLocked;
        Touch();
    }

    public void SetVerificationToken(string token)
    {
        EmailVerificationToken = token;
        EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        Touch();
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiry = null;
        Touch();
    }

    public void ChangeRole(Guid newRoleId)
    {
        RoleId = newRoleId;
        Touch();
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        Touch();
    }

    public void ApplyPenalty(decimal penalty)
    {
        ReputationScore = Math.Max(0, ReputationScore - penalty);
        UpdateTrustLevel();
        Touch();
    }

    public void AddScore(decimal score)
    {
        ReputationScore = Math.Min(100, ReputationScore + score);
        UpdateTrustLevel();
        Touch();
    }

    private void UpdateTrustLevel()
    {
        TrustLevel = ReputationScore switch
        {
            >= 80 => "TRUSTED",
            >= 60 => "STANDARD",
            >= 40 => "RESTRICTED",
            _ => "SUSPENDED"
        };
    }
}
