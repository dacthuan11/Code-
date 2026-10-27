using Nexus.UserService.Application.UseCases.CreateUser;
using System.Security.Cryptography;
using System.Text;

namespace Nexus.UserService.Infrastructure.Security;

/// <summary>
/// Implementation dùng PBKDF2-SHA256 để hash password.
/// Tại sao PBKDF2 thay vì MD5/SHA1? Vì PBKDF2 có key stretching (100k iterations),
/// làm chậm brute force attack xuống còn rất khó tấn công ngay cả khi lộ DB.
///
/// Format lưu: "{base64(salt)}:{base64(hash)}"
/// Salt được sinh ngẫu nhiên mỗi lần hash → cùng 1 password sẽ ra hash khác nhau.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        // Sinh 16 bytes salt ngẫu nhiên - đủ để tránh rainbow table attack
        var salt = RandomNumberGenerator.GetBytes(16);

        // PBKDF2 với 100,000 iterations và SHA256
        // 100k iterations là NIST recommendation năm 2023 cho SHA-256
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32); // lấy 32 bytes = 256 bits output

        // Lưu salt kèm hash để khi verify có thể tái tạo lại đúng hash
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        // Parse format "salt:hash" - nếu format sai thì chắc chắn không match
        var parts = hash.Split(':');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var storedHash = Convert.FromBase64String(parts[1]);

        // Tái tạo hash từ password + salt đã lưu
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var inputHash = pbkdf2.GetBytes(32);

        // FixedTimeEquals thay vì == hay SequenceEqual để chống timing attack:
        // nếu dùng so sánh thông thường, attacker có thể đo thời gian để biết
        // bao nhiêu byte đầu khớp, từ đó brute force từng byte một.
        return CryptographicOperations.FixedTimeEquals(inputHash, storedHash);
    }
}
