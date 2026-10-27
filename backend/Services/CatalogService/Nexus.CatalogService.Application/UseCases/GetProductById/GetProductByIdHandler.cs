using MediatR;
using Nexus.CatalogService.Application.Services;
using Nexus.CatalogService.Domain.Repositories;

namespace Nexus.CatalogService.Application.UseCases.GetProductById;

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDetailDto?>
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IStockService _stock;

    public GetProductByIdHandler(IProductRepository products, ICategoryRepository categories, IStockService stock)
    {
        _products = products;
        _categories = categories;
        _stock = stock;
    }

    public async Task<ProductDetailDto?> Handle(GetProductByIdQuery query, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(query.ProductId, ct);
        if (product is null) return null;

        var category = await _categories.GetByIdAsync(product.CategoryId, ct);

        var stockMap = await _stock.GetAvailableStockAsync([product.Id], ct);
        var stockQty = stockMap.GetValueOrDefault(product.Id, 0);

        return new ProductDetailDto(
            product.Id, product.Name, product.Description, product.Price,
            stockQty,
            product.Status.ToString(), product.CategoryId,
            category?.Name ?? "",
            product.SellerId, product.ImageUrls, product.CreatedAt);
    }
}
