using MediatR;
using Nexus.AuctionService.Domain.Repositories;

namespace Nexus.AuctionService.Application.UseCases.GetAuction;

public record BidDto(Guid Id, Guid AuctionId, Guid BidderId, string BidderEmail, decimal Amount, DateTime CreatedAt, bool IsWinning);

public record GetBidsQuery(Guid AuctionId) : IRequest<List<BidDto>?>;

public class GetBidsHandler : IRequestHandler<GetBidsQuery, List<BidDto>?>
{
    private readonly IAuctionRepository _auctions;
    public GetBidsHandler(IAuctionRepository auctions) => _auctions = auctions;

    public async Task<List<BidDto>?> Handle(GetBidsQuery q, CancellationToken ct)
    {
        var auction = await _auctions.GetByIdAsync(q.AuctionId, ct);
        if (auction is null) return null;
        var highestAmount = auction.Bids.Count > 0 ? auction.Bids.Max(b => b.Amount) : 0;
        return auction.Bids
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BidDto(b.Id, b.AuctionId, b.BidderId, b.BidderEmail, b.Amount, b.CreatedAt, b.Amount == highestAmount))
            .ToList();
    }
}
