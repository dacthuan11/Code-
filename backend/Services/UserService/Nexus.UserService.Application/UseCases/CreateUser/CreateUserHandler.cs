using MediatR;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;
using Nexus.UserService.Domain.Entities;
using Nexus.UserService.Domain.Repositories;
using Nexus.UserService.Application.Persistence;

namespace Nexus.UserService.Application.UseCases.CreateUser;

/// <summary>
/// Handler xử lý việc tạo user mới.
/// Flow chính: kiểm tra email duplicate → lấy default role → hash password → tạo user → save → publish event.
/// </summary>
public class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IEventBus _eventBus;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserDatabase _db;

    public CreateUserHandler(IUserRepository userRepo, IRoleRepository roleRepo,
        IEventBus eventBus, IPasswordHasher passwordHasher, IUserDatabase db)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _eventBus = eventBus;
        _passwordHasher = passwordHasher;
        _db = db;
    }

    public async Task<CreateUserResult> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        // Kiểm tra email đã tồn tại chưa - phải check trước khi làm gì khác
        // vì email là unique identifier của user
        var existing = await _userRepo.GetByEmailAsync(cmd.Email, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Email '{cmd.Email}' đã được đăng ký. Vui lòng sử dụng email khác hoặc đăng nhập.");

        // Lấy default role (BUYER) - nếu chưa seed data thì throw luôn,
        // không tạo user thiếu role vì sẽ lỗi auth sau này
        var defaultRole = await _roleRepo.GetDefaultRoleAsync(ct)
            ?? throw new InvalidOperationException("Default role 'BUYER' chưa được cấu hình trong hệ thống.");

        // Hash password trước khi tạo entity - KHÔNG bao giờ lưu plain text vào DB
        var passwordHash = _passwordHasher.Hash(cmd.Password);

        // Chuẩn hóa và gán giá trị mặc định cho các trường tùy chọn khi người dùng tự đăng ký nhanh
        var phone = !string.IsNullOrWhiteSpace(cmd.PhoneNumber)
            ? cmd.PhoneNumber.Trim()
            : "09" + Random.Shared.Next(10000000, 99999999).ToString();

        var identifyNumber = !string.IsNullOrWhiteSpace(cmd.IdentifyNumber)
            ? cmd.IdentifyNumber.Trim()
            : "00" + Random.Shared.Next(10000000, 99999999).ToString() + Random.Shared.Next(10, 99).ToString();

        var gender = !string.IsNullOrWhiteSpace(cmd.Gender) ? cmd.Gender.Trim() : "Other";
        var address = !string.IsNullOrWhiteSpace(cmd.Address) ? cmd.Address.Trim() : "Chưa cập nhật";
        var dob = cmd.DateOfBirth.HasValue && cmd.DateOfBirth.Value != default
            ? DateTime.SpecifyKind(cmd.DateOfBirth.Value, DateTimeKind.Utc)
            : DateTime.SpecifyKind(DateTime.UtcNow.AddYears(-18), DateTimeKind.Utc);

        var user = User.Create(
            cmd.Email.Trim().ToLowerInvariant(),
            phone,
            cmd.FullName.Trim(),
            identifyNumber,
            gender,
            address,
            dob,
            passwordHash,
            defaultRole.Id);

        var verificationToken = Guid.NewGuid().ToString("N");
        user.SetVerificationToken(verificationToken);

        await _userRepo.AddAsync(user, ct);
        await _userRepo.SaveChangesAsync(ct); // commit vào DB trước khi publish event

        var wallet = Wallet.Create(user.Id);
        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(new UserRegisteredEvent(user.Id, user.Email, user.FullName, verificationToken), ct);

        return new CreateUserResult(user.Id, user.Email, user.FullName);
    }
}

/// <summary>
/// Interface để abstract việc hash/verify password.
/// Tách ra đây (Application layer) vì cả CreateUser lẫn Login đều cần dùng,
/// implementation cụ thể (PBKDF2) nằm ở Infrastructure layer.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
