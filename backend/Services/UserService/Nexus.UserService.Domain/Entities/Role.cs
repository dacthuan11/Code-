using Nexus.Shared.Domain;

namespace Nexus.UserService.Domain.Entities;

/// <summary>
/// Entity đại diện cho vai trò (quyền hạn) trong hệ thống.
/// Hiện tại có 3 role cố định: BUYER, SELLER, ADMIN (xem seed data trong UserDbContext).
/// Privileges là danh sách các action mà role này được phép thực hiện.
/// </summary>
public class Role : BaseEntity
{
    /// <summary>Tên hiển thị, ví dụ: "Buyer", "Seller"</summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Code dùng trong JWT claims và policy check, ví dụ: "BUYER", "ADMIN".
    /// Dùng uppercase để dễ so sánh, tránh nhầm lẫn case.
    /// </summary>
    public string Code { get; private set; } = default!;

    public string Description { get; private set; } = default!;

    /// <summary>
    /// Danh sách các privilege mà role này có.
    /// Được store dưới dạng comma-separated string trong DB (xem mapping trong UserDbContext).
    /// Ví dụ: ["AUTH.LOGIN", "CART.CREATE", "ORDER.CREATE"]
    /// </summary>
    public List<string> Privileges { get; private set; } = new();

    /// <summary>Private constructor cho EF Core</summary>
    private Role() { }

    /// <summary>Constructor dùng cho EF Core HasData seeding — không dùng runtime</summary>
    public Role(Guid id, string name, string code, string description, List<string> privileges, DateTime createdAt)
    {
        Id = id; Name = name; Code = code; Description = description;
        Privileges = privileges; CreatedAt = createdAt; IsDeleted = false;
    }

    /// <summary>
    /// Tạo Role mới. Nếu không truyền privileges thì mặc định chỉ có READ_ONLY.
    /// Dùng null-coalescing để an toàn khi caller truyền null vào.
    /// </summary>
    public static Role Create(string name, string code, string description, List<string>? privileges = null)
    {
        return new Role
        {
            Name = name,
            Code = code,
            Description = description,
            Privileges = privileges ?? new List<string> { "READ_ONLY" } // fallback an toàn
        };
    }

    /// <summary>
    /// Cập nhật thông tin role. Code không cho đổi vì đang được dùng trong JWT token đang chạy,
    /// đổi Code sẽ làm token cũ của user bị invalid ngay lập tức.
    /// </summary>
    public void Update(string name, string description, List<string> privileges)
    {
        Name = name;
        Description = description;
        Privileges = privileges;
        Touch();
    }
}
