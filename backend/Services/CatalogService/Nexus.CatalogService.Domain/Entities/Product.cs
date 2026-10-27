using Nexus.Shared.Domain;

namespace Nexus.CatalogService.Domain.Entities;

// Trạng thái vòng đời của sản phẩm
// Draft → chưa publish, chỉ seller thấy
// Active → đang bán, hiện ra ngoài
// Inactive → tạm ẩn (seller tự ẩn hoặc bị admin ẩn)
public enum ProductStatus { Draft, Active, Inactive, Sold }

/// <summary>
/// Aggregate root đại diện cho một sản phẩm trong catalog.
/// Dùng private setter + factory method để enforce business rules từ domain,
/// tránh bên ngoài tự ý set field lung tung.
/// </summary>
public class Product : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Price { get; private set; }

    // SellerId lưu Guid của seller - không navigation property sang UserService
    // vì đây là separate microservice, cross-service join thì dùng API call hoặc event
    public Guid SellerId { get; private set; }

    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = default!; // EF navigation, load khi cần

    // Mặc định tạo ra là Draft - seller phải Activate thủ công
    public ProductStatus Status { get; private set; } = ProductStatus.Draft;

    // Lưu dạng List<string>, EF sẽ serialize thành chuỗi pipe-separated trong DB
    // (xem config trong CatalogDbContext)
    public List<string> ImageUrls { get; private set; } = new();

    // EF Core cần parameterless constructor nhưng ta không muốn ai gọi trực tiếp
    // → để private, dùng factory method Create() thay thế
    private Product() { }

    /// <summary>
    /// Factory method tạo product mới - status mặc định là Draft.
    /// Không validate ở đây, validation nằm ở FluentValidation trong Application layer.
    /// </summary>
    public static Product Create(string name, string description, decimal price,
        Guid sellerId, Guid categoryId) => new()
    {
        Name = name, Description = description,
        Price = price, SellerId = sellerId, CategoryId = categoryId
    };

    /// <summary>
    /// Cập nhật thông tin cơ bản của product.
    /// Touch() là từ BaseEntity - cập nhật UpdatedAt timestamp.
    /// </summary>
    public void Update(string name, string description, decimal price, Guid categoryId)
    {
        Name = name; Description = description; Price = price; CategoryId = categoryId; Touch();
    }

    // Seller/admin gọi để publish sản phẩm ra ngoài
    public void Activate() { Status = ProductStatus.Active; Touch(); }

    // Ẩn sản phẩm nhưng không xóa - dữ liệu order lịch sử vẫn reference được
    public void Deactivate() { Status = ProductStatus.Inactive; Touch(); }

    // Đánh dấu đã bán (sau khi order/auction settle)
    public void MarkSold() { Status = ProductStatus.Sold; Touch(); }

    /// <summary>
    /// Giới hạn tối đa 5 ảnh/product - tránh seller upload quá nhiều.
    /// Nếu đã đủ 5 thì im lặng bỏ qua, không throw exception.
    /// Có thể xem xét throw InvalidOperationException thay vì silent ignore trong tương lai.
    /// </summary>
    public void AddImage(string url) { if (ImageUrls.Count < 5) ImageUrls.Add(url); }

    // ── Variants (SKU) ───────────────────────────────────────────────────────
    private readonly List<ProductSku> _skus = new();
    public IReadOnlyList<ProductSku> Skus => _skus.AsReadOnly();
    public void AddSku(ProductSku sku)    { _skus.Add(sku); Touch(); }
    public void RemoveSku(Guid skuId)     { _skus.RemoveAll(s => s.Id == skuId); Touch(); }

    // ── Reviews & Rating ─────────────────────────────────────────────────────
    private readonly List<ProductReview> _reviews = new();
    public IReadOnlyList<ProductReview> Reviews => _reviews.AsReadOnly();
    public double AverageRating { get; private set; }
    public int    ReviewCount   { get; private set; }

    public void AddReview(ProductReview review)
    {
        _reviews.Add(review);
        ReviewCount++;
        AverageRating = _reviews.Average(r => r.Rating);
        Touch();
    }
}
