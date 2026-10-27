using MediatR;

namespace Nexus.CatalogService.Application.UseCases.GetProductById;

/// <summary>Query lấy chi tiết một product theo Id.</summary>
public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDetailDto?>;
// Trả nullable → handler trả null nếu không tìm thấy, controller map sang 404

/// <summary>
/// DTO đầy đủ cho trang detail sản phẩm.
/// Stock hardcode = 0 vì stock thực tế quản lý bởi FulfillmentService (cross-service).
/// Có thể aggregate sau bằng BFF hoặc API Gateway.
/// </summary>
public record ProductDetailDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,          // luôn = 0 hiện tại, xem GetProductByIdHandler comment
    string Status,      // string representation của ProductStatus enum
    Guid CategoryId,
    string CategoryName,
    Guid SellerId,
    List<string> ImageUrls,
    DateTime CreatedAt
);
