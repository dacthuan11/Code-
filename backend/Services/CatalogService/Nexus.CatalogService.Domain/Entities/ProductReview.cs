using Nexus.Shared.Domain;

namespace Nexus.CatalogService.Domain.Entities;

public class ProductReview : BaseEntity
{
    public Guid   ProductId   { get; private set; }
    public Guid   ReviewerId  { get; private set; }
    public string ReviewerName { get; private set; } = default!;
    public Guid   OrderItemId { get; private set; }  // đảm bảo đã mua mới được review
    public int    Rating      { get; private set; }  // 1-5 sao
    public string Comment     { get; private set; } = default!;
    public List<string> ImageUrls { get; private set; } = new();

    private ProductReview() { }

    public static ProductReview Create(Guid productId, Guid reviewerId, string reviewerName,
        Guid orderItemId, int rating, string comment, List<string> imageUrls) =>
        new()
        {
            ProductId   = productId,
            ReviewerId  = reviewerId,
            ReviewerName = reviewerName,
            OrderItemId = orderItemId,
            Rating      = Math.Clamp(rating, 1, 5),
            Comment     = comment,
            ImageUrls   = imageUrls
        };
}
