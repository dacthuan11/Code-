using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.CatalogService.Domain.Entities;
using Nexus.CatalogService.Application.Persistence;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.CatalogService.Application.UseCases.CreateProductReview;

public record CreateProductReviewCommand(
    Guid ProductId,
    Guid ReviewerId,
    string ReviewerName,
    Guid OrderItemId,
    int Rating,
    string Comment,
    List<string> ImageUrls
) : IRequest<Guid>;

public class CreateProductReviewValidator : AbstractValidator<CreateProductReviewCommand>
{
    public CreateProductReviewValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Rating phải từ 1 đến 5 sao.");
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ReviewerName).NotEmpty().MaximumLength(100);
    }
}

public class CreateProductReviewHandler : IRequestHandler<CreateProductReviewCommand, Guid>
{
    private readonly ICatalogDatabase _db;
    private readonly IEventBus _eventBus;

    public CreateProductReviewHandler(ICatalogDatabase db, IEventBus eventBus)
    {
        _db = db; _eventBus = eventBus;
    }

    public async Task<Guid> Handle(CreateProductReviewCommand cmd, CancellationToken ct)
    {
        var product = await _db.Products.Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == cmd.ProductId, ct)
            ?? throw new KeyNotFoundException("Sản phẩm không tồn tại.");

        var alreadyReviewed = await _db.ProductReviews
            .AnyAsync(r => r.ReviewerId == cmd.ReviewerId && r.OrderItemId == cmd.OrderItemId, ct);
        if (alreadyReviewed) throw new InvalidOperationException("Bạn đã đánh giá sản phẩm này rồi.");

        var review = ProductReview.Create(
            cmd.ProductId, cmd.ReviewerId, cmd.ReviewerName,
            cmd.OrderItemId, cmd.Rating, cmd.Comment, cmd.ImageUrls);

        product.AddReview(review);
        await _db.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(
            new ProductReviewedEvent(cmd.ProductId, product.SellerId, cmd.ReviewerId, cmd.Rating, cmd.Comment), ct);

        return review.Id;
    }
}
