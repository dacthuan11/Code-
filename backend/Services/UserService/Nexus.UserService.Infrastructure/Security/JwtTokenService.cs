using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Nexus.UserService.Application.UseCases.Login;
using Nexus.UserService.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Nexus.UserService.Infrastructure.Security;

/// <summary>
/// Tạo JWT access token và refresh token.
/// Token được ký bằng HMAC-SHA256 với secret key từ config.
///
/// QUAN TRỌNG: Jwt:Key phải đủ dài (ít nhất 32 bytes / 256 bits) và bí mật.
/// Không được commit key này lên git - lưu trong environment variable hoặc secret manager.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public string GenerateAccessToken(User user)
    {
        // Lấy secret key từ config và tạo signing credentials
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Default expire 60 phút nếu không cấu hình - không nên để quá dài vì token không revocable
        var expires = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60"));

        var claims = new[]
        {
            // sub: user ID - đây là standard claim, nhiều middleware đọc cái này
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),

            // role: dùng để check policy trong controller [Authorize(Policy = "USER.LIST")]
            new Claim("role", user.Role?.Code ?? "BUYER"), // fallback BUYER nếu role chưa loaded

            // jti (JWT ID): unique ID cho mỗi token - dùng để blacklist token sau này nếu cần
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],  // phải match ValidIssuer trong Program.cs
            claims: claims,
            expires: expires,
            signingCredentials: creds);
        // Không set audience vì ValidateAudience = false trong Program.cs

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Refresh token chỉ là random bytes không có cấu trúc.
    /// Hiện tại CHƯA được lưu vào DB nên chưa thể dùng để xin access token mới.
    /// TODO: Implement refresh token rotation - lưu vào Redis/DB với expiry dài hơn.
    /// </summary>
    public string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)); // 64 bytes = 512 bits
}
