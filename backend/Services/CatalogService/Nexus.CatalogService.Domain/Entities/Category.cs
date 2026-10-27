using Nexus.Shared.Domain;

namespace Nexus.CatalogService.Domain.Entities;

/// <summary>
/// Entity đại diện cho danh mục sản phẩm - hỗ trợ cấu trúc cây tối đa 3 cấp.
/// Ví dụ: Điện tử (1) → Điện thoại (2) → Điện thoại Android (3)
/// Level 1 là root category (không có parent).
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; private set; } = default!;

    // Slug dùng cho URL-friendly path, ví dụ: "dien-thoai-android"
    // Tự động generate từ Name khi Create/Update
    public string Slug { get; private set; } = default!;

    // null = root category (cấp 1)
    public Guid? ParentId { get; private set; }

    // EF navigation - chỉ load khi Include() hoặc query cần
    public Category? Parent { get; private set; }
    public List<Category> Children { get; private set; } = new();

    // max 3 - comment này quan trọng, giới hạn cứng trong business rule
    // được enforce trong CreateCategoryHandler, không phải ở đây
    public int Level { get; private set; } = 1;

    // Private constructor - bắt buộc dùng factory method
    private Category() { }

    /// <summary>
    /// Tạo category mới với slug tự động từ Name.
    /// Slug chỉ replace space → dash, chưa handle ký tự đặc biệt tiếng Việt.
    /// TODO: dùng thư viện slugify để handle "Điện thoại" → "dien-thoai" đúng chuẩn
    /// </summary>
    public static Category Create(string name, Guid? parentId, int level) => new()
    {
        Name = name,
        // Slug đơn giản: lowercase + replace space bằng dash
        // Lưu ý: "Điện Thoại" sẽ ra "điện-thoại", không normalize unicode
        Slug = name.ToLower().Replace(" ", "-"),
        ParentId = parentId,
        Level = level
    };

    /// <summary>
    /// Đổi tên category - slug cũng được cập nhật theo.
    /// Cẩn thận nếu slug đang được dùng trong URL/cache bên ngoài.
    /// </summary>
    public void Update(string name) { Name = name; Slug = name.ToLower().Replace(" ", "-"); Touch(); }
}
