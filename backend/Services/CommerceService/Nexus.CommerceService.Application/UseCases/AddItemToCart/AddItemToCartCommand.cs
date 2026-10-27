using MediatR;

namespace Nexus.CommerceService.Application.UseCases.AddItemToCart;

// Command: yêu cầu thêm 1 sản phẩm vào giỏ hàng.
// Dùng record thay vì class vì command là immutable - không nên sửa sau khi tạo.
// IRequest<AddItemToCartResult> báo cho MediatR biết command này trả về kết quả gì.
public record AddItemToCartCommand(
    Guid UserId,        // ai đang thêm vào giỏ
    Guid ProductId,     // sản phẩm nào
    string ProductName, // lưu snapshot tên để giỏ không bị ảnh hưởng khi product bị đổi tên
    decimal UnitPrice,  // snapshot giá tại thời điểm này
    int Quantity,
    string? ImageUrl    // ImageUrl không được persist vào domain entity hiện tại,
                        // nhưng giữ lại trong command để FE có thể dùng trong response nếu cần
) : IRequest<AddItemToCartResult>;

// Result trả về sau khi thêm thành công - đủ thông tin để FE update UI mà không cần fetch lại cart
public record AddItemToCartResult(Guid CartId, int ItemCount, decimal Subtotal);
