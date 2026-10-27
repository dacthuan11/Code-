using FluentValidation;

namespace Nexus.AuctionService.Application.UseCases.CreateAuction;

// Validation tách riêng khỏi Handler - Single Responsibility Principle
// FluentValidation tự động được gọi bởi MediatR pipeline (ValidationBehavior)
// Nếu fail sẽ throw ValidationException trước khi vào Handler
public class CreateAuctionValidator : AbstractValidator<CreateAuctionCommand>
{
    public CreateAuctionValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.StartingPrice).GreaterThan(0);
        RuleFor(x => x.BidIncrement).GreaterThan(0);

        // ReservePrice chỉ validate khi có giá trị - optional field
        // Reserve phải >= starting price để có ý nghĩa business
        RuleFor(x => x.ReservePrice).GreaterThanOrEqualTo(x => x.StartingPrice)
            .When(x => x.ReservePrice.HasValue);

        // StartTime phải trong tương lai - check theo UTC để tránh timezone issue
        RuleFor(x => x.StartTime).GreaterThan(DateTime.UtcNow);

        // EndTime phải sau StartTime - dùng expression để so sánh với field khác trong cùng command
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime);
    }
}
