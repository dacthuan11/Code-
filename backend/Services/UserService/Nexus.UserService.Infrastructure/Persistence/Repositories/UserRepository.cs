using Microsoft.EntityFrameworkCore;
using Nexus.UserService.Domain.Entities;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implemention của IUserRepository dùng EF Core.
/// Tất cả query đều benefiting từ global query filter (IsDeleted = false)
/// được set trong UserDbContext - không cần lặp lại ở đây.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly UserDbContext _db;

    // Constructor ngắn gọn - không cần body vì chỉ assign 1 field
    public UserRepository(UserDbContext db) => _db = db;

    /// <summary>
    /// FirstOrDefault thay vì Find vì EF Core's Find không apply query filter.
    /// Role sẽ được EF Core lazy-load hoặc caller phải Include() khi cần user.Role.Code.
    /// </summary>
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    /// <summary>
    /// Dùng khi login — Include Role để user.Role.Code không null.
    /// Include() bắt buộc vì EF Core không lazy-load navigation property theo default.
    /// </summary>
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetByVerificationTokenAsync(string token, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token, ct);

    public async Task<(List<User> Items, int Total)> GetListAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Users.Include(u => u.Role).AsQueryable();

        // Nếu có keyword thì tìm theo tên hoặc email - Contains map sang SQL LIKE '%...%'
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

        // Đếm total trước khi Skip/Take để có đúng tổng số cho pagination
        var total = await q.CountAsync(ct);

        // Phân trang: page 1 = skip 0, page 2 = skip pageSize, v.v.
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _db.Users.AddAsync(user, ct);

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        // EF Core tự track entity khi fetch từ DB, không cần gọi Update() tường minh.
        // Nhưng giữ lại method này để fulfill interface contract.
        _db.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task<User?> GetByIdWithRoleAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct);

    /// <summary>Commit tất cả pending changes trong unit of work này</summary>
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
