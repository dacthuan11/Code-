using Nexus.AuctionService.Domain.Entities;

namespace Nexus.AuctionService.Domain.Repositories;

// Repository interface định nghĩa ở Domain để không phụ thuộc vào Infrastructure
// Theo DIP (Dependency Inversion Principle): domain không biết gì về EF Core
public interface IAuctionRepository
{
    // Load kèm Bids luôn vì PlaceBid handler cần đọc CurrentWinnerId và Bids list
    Task<Auction?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Dùng cho background job check và xử lý các active auctions
    Task<List<Auction>> GetActiveAuctionsAsync(CancellationToken ct = default);

    // Lấy các auction Scheduled mà StartTime <= before - dùng để auto-activate theo lịch
    Task<List<Auction>> GetScheduledAuctionsAsync(DateTime before, CancellationToken ct = default);

    Task<(List<Auction> Items, int Total)> GetAllAsync(
        AuctionStatus? status, int page, int pageSize, CancellationToken ct = default);

    // Auctions in Ended status whose PaymentDeadline has passed (winner didn't pay)
    Task<List<Auction>> GetExpiredPaymentDeadlineAuctionsAsync(DateTime now, CancellationToken ct = default);

    Task AddAsync(Auction auction, CancellationToken ct = default);
    Task UpdateAsync(Auction auction, CancellationToken ct = default);
    Task AddBidAsync(Bid bid, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
