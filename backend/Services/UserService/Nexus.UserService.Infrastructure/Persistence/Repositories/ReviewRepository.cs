using Microsoft.EntityFrameworkCore;
using Nexus.UserService.Domain.Entities;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Infrastructure.Persistence.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly UserDbContext _db;
    public ReviewRepository(UserDbContext db) => _db = db;

    public Task<List<Review>> GetByTargetUserAsync(Guid targetUserId, int page, int pageSize, CancellationToken ct = default) =>
        _db.Reviews
            .Where(r => r.TargetUserId == targetUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountByTargetUserAsync(Guid targetUserId, CancellationToken ct = default) =>
        _db.Reviews.CountAsync(r => r.TargetUserId == targetUserId, ct);

    public async Task AddAsync(Review review, CancellationToken ct = default) =>
        await _db.Reviews.AddAsync(review, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
