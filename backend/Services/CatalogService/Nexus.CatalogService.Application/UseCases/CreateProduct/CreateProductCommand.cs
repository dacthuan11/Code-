using MediatR;

namespace Nexus.CatalogService.Application.UseCases.CreateProduct;

/// <summary>
/// Command tạo product mới - CQRS pattern.
/// Dùng record vì command là immutable data, không cần thay đổi sau khi tạo.
/// Trả về Guid của product vừa tạo để FE redirect sang trang detail.
///
/// Lưu ý: Handler của command này chưa được implement (TODO).
/// Xem DeleteProductHandler hoặc UpdateProductHandler để tham khảo pattern.
/// </summary>
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    Guid SellerId,    // lấy từ JWT claims trong controller, không để client truyền lên
    Guid CategoryId
) : IRequest<Guid>;
