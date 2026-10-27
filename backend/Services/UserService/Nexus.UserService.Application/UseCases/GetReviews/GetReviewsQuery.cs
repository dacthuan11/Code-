using MediatR;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Application.UseCases.GetReviews;

public record GetReviewsQuery(Guid TargetUserId, int Page = 1, int PageSize = 10)
    : IRequest<GetReviewsResult>;

public record ReviewDto(
    Guid Id, Guid ReviewerId, Guid TargetUserId, Guid? OrderId,
    int Rating, string Comment, DateTime CreatedAt);

public record GetReviewsResult(List<ReviewDto> Items, int Total, int Page, int PageSize);

public class GetReviewsHandler : IRequestHandler<GetReviewsQuery, GetReviewsResult>
{
    private readonly IReviewRepository _reviews;

    public GetReviewsHandler(IReviewRepository reviews) => _reviews = reviews;

    public async Task<GetReviewsResult> Handle(GetReviewsQuery q, CancellationToken ct)
    {
        var items = await _reviews.GetByTargetUserAsync(q.TargetUserId, q.Page, q.PageSize, ct);
        var total = await _reviews.CountByTargetUserAsync(q.TargetUserId, ct);

        var dtos = items.Select(r => new ReviewDto(
            r.Id, r.ReviewerId, r.TargetUserId, r.OrderId,
            r.Rating, r.Comment, r.CreatedAt)).ToList();

        return new GetReviewsResult(dtos, total, q.Page, q.PageSize);
    }
}
