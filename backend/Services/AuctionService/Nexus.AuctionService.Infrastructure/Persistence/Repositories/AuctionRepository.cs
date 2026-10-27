using Microsoft.EntityFrameworkCore;
using Nexus.AuctionService.Domain.Entities;
using Nexus.AuctionService.Domain.Repositories;

namespace Nexus.AuctionService.Infrastructure.Persistence.Repositories;

// Implementation của IAuctionRepository bằng EF Core
// Logic truy vấn DB tập trung ở đây, không rải rác trong từng handler
public class AuctionRepository : IAuctionRepository
{
    private readonly AuctionDbContext _db;
    public AuctionRepository(AuctionDbContext db) => _db = db;

    // Include Bids luôn vì PlaceBid cần đọc CurrentWinnerId và EndAuction cần Bids list để tìm topBid
    // Có thể tối ưu sau bằng split query (.AsSplitQuery()) nếu Bids list quá lớn
    public Task<Auction?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Auctions.Include(a => a.Bids).FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<List<Auction>> GetActiveAuctionsAsync(CancellationToken ct = default) =>
        _db.Auctions.Where(a => a.Status == AuctionStatus.Active).ToListAsync(ct);

    // IAuctionRepository.GetScheduledAuctionsAsync có tham số DateTime before
    // Dùng cho scheduler: lấy các auction cần được activate (StartTime đã qua mốc `before`)
    public Task<List<Auction>> GetScheduledAuctionsAsync(DateTime before, CancellationToken ct = default) =>
        _db.Auctions.Where(a => a.Status == AuctionStatus.Scheduled && a.StartTime <= before).ToListAsync(ct);

    public async Task<(List<Auction> Items, int Total)> GetAllAsync(
        AuctionStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Auctions.AsQueryable();
        if (status.HasValue) q = q.Where(a => a.Status == status.Value);
        q = q.OrderByDescending(a => a.CreatedAt);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<List<Auction>> GetExpiredPaymentDeadlineAuctionsAsync(DateTime now, CancellationToken ct = default) =>
        _db.Auctions.Where(a => a.Status == AuctionStatus.Ended
                                && a.PaymentDeadline.HasValue
                                && a.PaymentDeadline.Value <= now).ToListAsync(ct);

    public async Task AddAsync(Auction auction, CancellationToken ct = default) => await _db.Auctions.AddAsync(auction, ct);

    // EF Core tracks new Bid entities found via navigation property traversal as Modified
    // (because they have a non-null Guid key with no configured value generator).
    // Explicitly calling Add overrides that to Added so an INSERT is generated.
    public Task AddBidAsync(Bid bid, CancellationToken ct = default) { _db.Set<Bid>().Add(bid); return Task.CompletedTask; }

    // UpdateAsync dùng để explicit mark entity là Modified - thường không cần vì EF track tự động
    // Giữ lại phòng trường hợp cần update detached entity (không qua GetByIdAsync trong cùng scope)
    // Entry(auction).State = Modified marks only the Auction entity's scalar properties as Modified,
    // without cascading to navigation properties. DbSet.Update() would cascade and incorrectly mark
    // newly-created Bid entities (not yet in DB) as Modified instead of Added, causing the UPDATE
    // to match 0 rows and throw DbUpdateConcurrencyException.
    public Task UpdateAsync(Auction auction, CancellationToken ct = default) { _db.Entry(auction).State = EntityState.Modified; return Task.CompletedTask; }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
