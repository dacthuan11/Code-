using MediatR;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Application.UseCases.CreateReview;

public record CreateReviewCommand(
    Guid ReviewerId,
    Guid TargetUserId,
    Guid? OrderId,
    int Rating,
    string Comment
) : IRequest<Guid>;

public class CreateReviewHandler : IRequestHandler<CreateReviewCommand, Guid>
{
    private readonly IReviewRepository _reviews;
    private readonly IUserRepository _users;

    public CreateReviewHandler(IReviewRepository reviews, IUserRepository users)
    {
        _reviews = reviews;
        _users = users;
    }

    public async Task<Guid> Handle(CreateReviewCommand cmd, CancellationToken ct)
    {
        var target = await _users.GetByIdAsync(cmd.TargetUserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.TargetUserId} not found.");

        var review = Domain.Entities.Review.Create(
            cmd.ReviewerId, cmd.TargetUserId, cmd.Rating, cmd.Comment, cmd.OrderId);

        await _reviews.AddAsync(review, ct);
        await _reviews.SaveChangesAsync(ct);
        return review.Id;
    }
}
