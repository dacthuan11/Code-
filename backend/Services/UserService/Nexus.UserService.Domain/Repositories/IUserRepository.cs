using Nexus.UserService.Domain.Entities;

namespace Nexus.UserService.Domain.Repositories;

/// <summary>
/// Interface định nghĩa contract cho việc truy cập dữ liệu User.
/// Tách interface ra đây (Domain layer) để Application layer có thể dùng
/// mà không phụ thuộc vào EF Core hay bất kỳ infrastructure cụ thể nào
/// - đây là Dependency Inversion Principle trong Clean Architecture.
/// </summary>
public interface IUserRepository
{
    /// <summary>Lấy user theo ID. Trả về null nếu không tìm thấy (hoặc đã soft-delete).</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Includes Role navigation — cần thiết sau khi issue refresh token
    Task<User?> GetByIdWithRoleAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lấy user theo email - dùng khi login hoặc kiểm tra duplicate email khi đăng ký.
    /// Query filter trong DbContext đã lọc IsDeleted=true rồi nên không cần check lại ở đây.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> GetByVerificationTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách user có phân trang và hỗ trợ tìm kiếm.
    /// Trả về tuple (danh sách items của trang hiện tại, tổng số record) để build pagination.
    /// </summary>
    Task<(List<User> Items, int Total)> GetListAsync(string? search, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Update user - trong EF Core thường chỉ cần gọi SaveChanges nếu entity đang được tracked,
    /// nhưng để explicit thì vẫn expose method này.
    /// </summary>
    Task UpdateAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Lưu tất cả pending changes vào DB. Thường gọi ở cuối mỗi handler sau khi
    /// đã thực hiện xong các thay đổi domain.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task<Entities.RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(Entities.RefreshToken token, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IReviewRepository
{
    Task<List<Entities.Review>> GetByTargetUserAsync(Guid targetUserId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountByTargetUserAsync(Guid targetUserId, CancellationToken ct = default);
    Task AddAsync(Entities.Review review, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Interface cho Role repository. Cũng nằm trong Domain layer vì Role là part of domain.
/// </summary>
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lấy role theo code (BUYER/SELLER/ADMIN) - code là unique trong DB.</summary>
    Task<Role?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>
    /// Lấy role mặc định khi tạo user mới - hiện tại là BUYER.
    /// Tách ra method riêng để dễ đổi business rule sau này nếu cần.
    /// </summary>
    Task<Role?> GetDefaultRoleAsync(CancellationToken ct = default);

    Task<List<Role>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Role role, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
