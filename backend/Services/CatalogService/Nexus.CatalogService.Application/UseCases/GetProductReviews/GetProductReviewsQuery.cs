using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.CatalogService.Application.Persistence;

namespace Nexus.CatalogService.Application.UseCases.GetProductReviews;

public record GetProductReviewsQuery(Guid ProductId, int Page = 1, int PageSize = 10)
    : IRequest<GetProductReviewsResult>;

public record ProductReviewDto(
    Guid ReviewId, string ReviewerName, int Rating,
    string Comment, List<string> ImageUrls, DateTime CreatedAt);

public record GetProductReviewsResult(
    List<ProductReviewDto> Items, int Total, double AverageRating);

public class GetProductReviewsHandler : IRequestHandler<GetProductReviewsQuery, GetProductReviewsResult>
{
    private readonly ICatalogDatabase _db;
    public GetProductReviewsHandler(ICatalogDatabase db) => _db = db;

    public async Task<GetProductReviewsResult> Handle(GetProductReviewsQuery q, CancellationToken ct)
    {
        var query = _db.ProductReviews.Where(r => r.ProductId == q.ProductId);
        var total = await query.CountAsync(ct);
        var avg   = total > 0 ? await query.AverageAsync(r => (double)r.Rating, ct) : 0;

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(r => new ProductReviewDto(r.Id, r.ReviewerName, r.Rating,
                r.Comment, r.ImageUrls, r.CreatedAt))
            .ToListAsync(ct);

        return new GetProductReviewsResult(items, total, Math.Round(avg, 1));
    }
}
