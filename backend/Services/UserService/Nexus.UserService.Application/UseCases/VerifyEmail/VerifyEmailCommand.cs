using MediatR;
using Nexus.UserService.Domain.Repositories;

namespace Nexus.UserService.Application.UseCases.VerifyEmail;

public record VerifyEmailCommand(string Token) : IRequest<bool>;

public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, bool>
{
    private readonly IUserRepository _userRepo;

    public VerifyEmailHandler(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<bool> Handle(VerifyEmailCommand cmd, CancellationToken ct)
    {
        var user = await _userRepo.GetByVerificationTokenAsync(cmd.Token, ct);
        if (user is null) return false;
        if (user.EmailVerificationTokenExpiry < DateTime.UtcNow) return false;

        user.VerifyEmail();
        await _userRepo.SaveChangesAsync(ct);
        return true;
    }
}
