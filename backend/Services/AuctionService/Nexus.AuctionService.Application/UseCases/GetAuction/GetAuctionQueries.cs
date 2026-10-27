using MediatR;
using Nexus.AuctionService.Domain.Entities;
using Nexus.AuctionService.Domain.Repositories;

namespace Nexus.AuctionService.Application.UseCases.GetAuction;

public record AuctionDto(
    Guid Id,
    Guid ProductId,
    Guid SellerId,
    decimal StartingPrice,
    decimal BidIncrement,
    decimal CurrentPrice,
    Guid? CurrentWinnerId,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    int TotalBids,
    DateTime CreatedAt);

public record GetAuctionByIdQuery(Guid Id) : IRequest<AuctionDto?>;

public class GetAuctionByIdHandler : IRequestHandler<GetAuctionByIdQuery, AuctionDto?>
{
    private readonly IAuctionRepository _auctions;
    public GetAuctionByIdHandler(IAuctionRepository auctions) => _auctions = auctions;

    public async Task<AuctionDto?> Handle(GetAuctionByIdQuery q, CancellationToken ct)
    {
        var a = await _auctions.GetByIdAsync(q.Id, ct);
        return a is null ? null : ToDto(a);
    }

    internal static AuctionDto ToDto(Auction a) => new(
        a.Id, a.ProductId, a.SellerId,
        a.StartingPrice, a.BidIncrement, a.CurrentPrice,
        a.CurrentWinnerId, a.StartTime, a.EndTime,
        a.Status.ToString(), a.Bids.Count, a.CreatedAt);
}

public record GetAuctionsQuery(string? Status = null, int Page = 1, int PageSize = 20)
    : IRequest<GetAuctionsResult>;

public record GetAuctionsResult(List<AuctionDto> Items, int Total, int Page, int PageSize);

public class GetAuctionsHandler : IRequestHandler<GetAuctionsQuery, GetAuctionsResult>
{
    private readonly IAuctionRepository _auctions;
    public GetAuctionsHandler(IAuctionRepository auctions) => _auctions = auctions;

    public async Task<GetAuctionsResult> Handle(GetAuctionsQuery q, CancellationToken ct)
    {
        AuctionStatus? statusFilter = q.Status is not null
            ? Enum.TryParse<AuctionStatus>(q.Status, true, out var s) ? s : null
            : null;

        var (items, total) = await _auctions.GetAllAsync(statusFilter, q.Page, q.PageSize, ct);
        var dtos = items.Select(GetAuctionByIdHandler.ToDto).ToList();
        return new GetAuctionsResult(dtos, total, q.Page, q.PageSize);
    }
}
