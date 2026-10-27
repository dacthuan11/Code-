using Nexus.Shared.Domain;

namespace Nexus.CommerceService.Domain.Entities;

// Cart - giỏ hàng của user, thuần domain entity, không phụ thuộc infrastructure.
// Dùng private setter để bảo vệ invariant - chỉ có thể thay đổi qua các method
// thay vì set trực tiếp từ ngoài (tránh trạng thái không hợp lệ)
public class Cart : BaseEntity
{
    public Guid UserId { get; private set; }

    // Items là danh sách các sản phẩm trong giỏ - khởi tạo empty để tránh null check
    public List<CartItem> Items { get; private set; } = new();

    public DateTime ExpiresAt { get; private set; }

    // Computed property - giỏ hàng hết hạn sau 7 ngày không dùng
    // Không lưu vào DB, tính lại mỗi lần check
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    // Tổng tiền chưa tính phí ship - tính real-time từ items trong memory
    public decimal Subtotal => Items.Sum(i => i.UnitPrice * i.Quantity);

    // EF Core cần constructor không tham số để reconstruct entity từ DB
    // private để tránh code khác tạo Cart "rỗng" không có UserId
    private Cart() { }

    // Factory method thay vì constructor public - rõ ràng hơn về intent
    // Cart tạo ra sẽ tự expire sau 7 ngày (theo nghiệp vụ)
    public static Cart Create(Guid userId) => new()
    {
        UserId = userId,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    // Thêm item vào giỏ - nếu đã có product đó thì cộng dồn quantity
    // thay vì tạo CartItem mới (tránh duplicate entries trong DB)
    // Returns the new CartItem if created, or null if an existing item was updated.
    // Caller must ensure the returned CartItem is tracked by EF (AddAsync).
    public CartItem? AddItem(Guid productId, string productName, decimal price, int qty, string? imageUrl = null)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == productId);
        Touch();
        if (existing is not null)
        {
            existing.UpdateQuantity(existing.Quantity + qty);
            return null;
        }
        var item = CartItem.Create(Id, productId, productName, price, qty, imageUrl);
        Items.Add(item);
        return item;
    }

    // UpdateItem: qty <= 0 thì xóa luôn, không để item "chết" trong giỏ
    public void UpdateItem(Guid productId, int qty)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new KeyNotFoundException("Item not in cart.");

        if (qty <= 0) Items.Remove(item); // xóa hẳn nếu qty về 0
        else item.UpdateQuantity(qty);

        Touch();
    }

    public void RemoveItem(Guid productId)
    {
        Items.RemoveAll(i => i.ProductId == productId);
        Touch();
    }
}

// CartItem là value-object-like entity - chỉ tồn tại khi gắn với Cart
// Khi Cart bị delete thì CartItem cũng bị cascade delete (xem DbContext config)
public class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public string? ImageUrl { get; private set; }

    // Snapshot giá tại thời điểm thêm vào giỏ
    // Không lấy realtime từ ProductService để tránh giá nhảy loạn khi user đang checkout
    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    private CartItem() { }

    public static CartItem Create(Guid cartId, Guid productId, string name, decimal price, int qty, string? imageUrl = null)
        => new() { CartId = cartId, ProductId = productId, ProductName = name, UnitPrice = price, Quantity = qty, ImageUrl = imageUrl };

    public void UpdateQuantity(int qty) { Quantity = qty; Touch(); }
}
