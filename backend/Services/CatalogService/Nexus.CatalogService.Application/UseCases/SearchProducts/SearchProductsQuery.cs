using MediatR;

namespace Nexus.CatalogService.Application.UseCases.SearchProducts;

/// <summary>
/// Query tìm kiếm sản phẩm - dùng CQRS read side.
/// Tất cả params đều optional trừ pagination defaults.
/// Controller bind từ query string: GET /api/v1/products?keyword=phone&minPrice=100&page=2
///
/// Lưu ý: Handler của query này chưa được implement (TODO).
/// </summary>
public record SearchProductsQuery(
    string? Keyword,          // search theo Name hoặc Description
    Guid? CategoryId,         // filter theo category cụ thể
    decimal? MinPrice,
    decimal? MaxPrice,
    string SortBy = "CreatedAt",  // default sort mới nhất trước
    bool Desc = true,             // descending by default
    int Page = 1,
    int PageSize = 20             // 20 items/page là reasonable cho mobile
) : IRequest<SearchProductsResult>;

/// <summary>
/// Kết quả search có pagination metadata để FE tính toán tổng số trang.
/// Field names khớp với ProductListResponse trong frontend/src/api/catalog.ts.
/// </summary>
public record SearchProductsResult(
    List<ProductItemDto> Items,
    int TotalCount,  // totalCount để match với frontend interface
    int Page,
    int PageSize
);

/// <summary>
/// DTO cho mỗi item trong list - field names khớp với Product interface ở FE.
/// </summary>
public record ProductItemDto(
    Guid ProductId,       // productId (không phải id) để khớp FE
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,    // placeholder - FulfillmentService quản lý thực
    Guid CategoryId,
    string CategoryName,
    Guid SellerId,
    string? ImageUrl,     // singular - FE dùng một ảnh đại diện
    DateTime CreatedAt
);
