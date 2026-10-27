using Microsoft.EntityFrameworkCore;
using Nexus.AuctionService.Domain.Entities;

namespace Nexus.AuctionService.Application.Persistence;

public interface IAuctionDatabase
{
    DbSet<Auction>       Auctions       { get; }
    DbSet<Bid>           Bids           { get; }
    DbSet<FlashSale>     FlashSales     { get; }
    DbSet<FlashSaleItem> FlashSaleItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
