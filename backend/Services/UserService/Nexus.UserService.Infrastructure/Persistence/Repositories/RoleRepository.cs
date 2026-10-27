using Microsoft.EntityFrameworkCore;
using Nexus.UserService.Domain.Entities;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation của IRoleRepository.
/// Role ít thay đổi nên có thể cache sau này nếu cần optimize,
/// nhưng hiện tại query thẳng DB là đủ.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly UserDbContext _db;
    public RoleRepository(UserDbContext db) => _db = db;

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);

    /// <summary>Tìm role theo code - dùng khi cần map từ string "BUYER" sang entity</summary>
    public Task<Role?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Code == code, ct);

    /// <summary>
    /// Default role là BUYER - user tự đăng ký luôn được gán role này.
    /// Hardcode "BUYER" ở đây thay vì đọc config vì đây là business rule cố định,
    /// đổi thì cần change code có review chứ không phải đổi config là xong.
    /// </summary>
    public Task<Role?> GetDefaultRoleAsync(CancellationToken ct = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Code == "BUYER", ct);

    /// <summary>Lấy tất cả roles - dùng cho admin UI dropdown</summary>
    public Task<List<Role>> GetAllAsync(CancellationToken ct = default) =>
        _db.Roles.ToListAsync(ct);

    public async Task AddAsync(Role role, CancellationToken ct = default) =>
        await _db.Roles.AddAsync(role, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
