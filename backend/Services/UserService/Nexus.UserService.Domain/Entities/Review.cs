using Nexus.Shared.Domain;

namespace Nexus.UserService.Domain.Entities;

public class Review : BaseEntity
{
    public Guid ReviewerId { get; private set; }   // người viết review
    public Guid TargetUserId { get; private set; } // người được review (seller/buyer)
    public Guid? OrderId { get; private set; }     // đơn hàng liên quan
    public int Rating { get; private set; }        // 1-5
    public string Comment { get; private set; } = default!;

    private Review() { }

    public static Review Create(Guid reviewerId, Guid targetUserId, int rating, string comment, Guid? orderId = null)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be 1-5.");
        return new Review
        {
            ReviewerId = reviewerId,
            TargetUserId = targetUserId,
            Rating = rating,
            Comment = comment,
            OrderId = orderId
        };
    }
}
