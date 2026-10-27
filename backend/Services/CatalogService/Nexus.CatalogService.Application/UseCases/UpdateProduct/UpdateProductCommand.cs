using MediatR;

namespace Nexus.CatalogService.Application.UseCases.UpdateProduct;

/// <summary>
/// Command cập nhật thông tin product.
/// RequestedByUserId dùng để verify seller chỉ được sửa product của mình,
/// admin có thể sửa tất cả (logic authorization nằm trong handler).
///
/// Lưu ý: Stock field có trong command nhưng domain model Product hiện tại
/// không lưu stock - stock được quản lý bởi FulfillmentService.
/// Field này bị ignore trong handler, có thể bỏ ở sprint sau.
/// </summary>
public record UpdateProductCommand(
    Guid ProductId,
    Guid RequestedByUserId,   // lấy từ JWT claims, không để client tự truyền
    string Name,
    string Description,
    decimal Price,
    int Stock,                // TODO: forward sang FulfillmentService thay vì bỏ qua
    Guid CategoryId,
    List<string> ImageUrls    // toàn bộ danh sách ảnh mới (replace all, không append)
) : IRequest<UpdateProductResult>;

/// <summary>
/// Trả về thông tin tóm tắt sau khi update thành công.
/// FE dùng để cập nhật state mà không cần gọi thêm GET request.
/// </summary>
public record UpdateProductResult(Guid ProductId, string Name, decimal Price);
