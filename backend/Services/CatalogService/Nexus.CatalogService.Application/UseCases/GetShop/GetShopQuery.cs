using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.CatalogService.Application.Persistence;

namespace Nexus.CatalogService.Application.UseCases.GetShop;

public record GetShopQuery(Guid SellerId, int Page = 1, int PageSize = 20) : IRequest<ShopDto>;

public record ShopProductDto(Guid ProductId, string Name, decimal Price,
    double AverageRating, int ReviewCount, List<string> ImageUrls, string Status);

public record ShopDto(Guid SellerId, List<ShopProductDto> Products,
    int TotalProducts, double AverageRating);

public class GetShopHandler : IRequestHandler<GetShopQuery, ShopDto>
{
    private readonly ICatalogDatabase _db;
    public GetShopHandler(ICatalogDatabase db) => _db = db;

    public async Task<ShopDto> Handle(GetShopQuery q, CancellationToken ct)
    {
        var query = _db.Products.Where(p => p.SellerId == q.SellerId && !p.IsDeleted);
        var total = await query.CountAsync(ct);
        var avgRating = total > 0
            ? await query.AverageAsync(p => p.AverageRating, ct)
            : 0;

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(p => new ShopProductDto(p.Id, p.Name, p.Price,
                p.AverageRating, p.ReviewCount, p.ImageUrls, p.Status.ToString()))
            .ToListAsync(ct);

        return new ShopDto(q.SellerId, products, total, Math.Round(avgRating, 1));
    }
}
