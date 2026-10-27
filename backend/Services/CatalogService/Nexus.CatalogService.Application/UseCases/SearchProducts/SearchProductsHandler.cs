using MediatR;
using Nexus.CatalogService.Application.Services;
using Nexus.CatalogService.Domain.Repositories;

namespace Nexus.CatalogService.Application.UseCases.SearchProducts;

public class SearchProductsHandler : IRequestHandler<SearchProductsQuery, SearchProductsResult>
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IStockService _stock;

    public SearchProductsHandler(IProductRepository products, ICategoryRepository categories, IStockService stock)
    {
        _products = products;
        _categories = categories;
        _stock = stock;
    }

    public async Task<SearchProductsResult> Handle(SearchProductsQuery q, CancellationToken ct)
    {
        var (items, total) = await _products.SearchAsync(
            q.Keyword, q.CategoryId, q.MinPrice, q.MaxPrice,
            q.SortBy, q.Page, q.PageSize, ct);

        var categories = await _categories.GetAllAsync(ct);
        var catMap = categories.ToDictionary(c => c.Id, c => c.Name);

        // Lấy stock từ FulfillmentService (bulk query để tránh N+1)
        var stockMap = new Dictionary<Guid, int>();
        try
        {
            stockMap = await _stock.GetAvailableStockAsync(items.Select(p => p.Id), ct);
        }
        catch
        {
            // FulfillmentService unavailable — hiển thị stock 0, không fail request
        }

        var dtos = items.Select(p => new ProductItemDto(
            ProductId:    p.Id,
            Name:         p.Name,
            Description:  p.Description,
            Price:        p.Price,
            StockQuantity: stockMap.GetValueOrDefault(p.Id, 0),
            CategoryId:   p.CategoryId,
            CategoryName: catMap.GetValueOrDefault(p.CategoryId, ""),
            SellerId:     p.SellerId,
            ImageUrl:     p.ImageUrls.FirstOrDefault(),
            CreatedAt:    p.CreatedAt
        )).ToList();

        return new SearchProductsResult(dtos, total, q.Page, q.PageSize);
    }
}
