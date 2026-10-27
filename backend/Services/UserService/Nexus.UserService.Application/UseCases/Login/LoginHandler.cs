using MediatR;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;
using Nexus.UserService.Application.UseCases.CreateUser;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Application.UseCases.Login;

public class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtService;
    private readonly IEventBus _eventBus;

    public LoginHandler(IUserRepository userRepo, IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher, IJwtTokenService jwtService, IEventBus eventBus)
    {
        _userRepo = userRepo;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _eventBus = eventBus;
    }

    public async Task<LoginResult> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await _userRepo.GetByEmailAsync(cmd.Email, ct)
            ?? throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");

        if (user.IsDeleted)
            throw new UnauthorizedAccessException("Tài khoản đã bị xóa.");

        if (user.IsLocked)
            throw new UnauthorizedAccessException("Tài khoản của bạn đã bị quản trị viên khóa.");

        if (!_passwordHasher.Verify(cmd.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");

        if (!user.IsEmailVerified)
            throw new InvalidOperationException("Email chưa được xác thực. Vui lòng kiểm tra hộp thư.");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenRaw = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = Domain.Entities.RefreshToken.Create(
            user.Id, refreshTokenRaw, DateTime.UtcNow.AddDays(30));
        await _refreshTokens.AddAsync(refreshTokenEntity, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(new UserLoggedInEvent(user.Id, user.Email), ct);

        return new LoginResult(accessToken, refreshTokenRaw, user.Id, user.Role.Code, user.Email, user.FullName);
    }
}

public interface IJwtTokenService
{
    string GenerateAccessToken(Domain.Entities.User user);
    string GenerateRefreshToken();
}
