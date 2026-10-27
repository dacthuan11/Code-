using Microsoft.EntityFrameworkCore;
using Nexus.UserService.Domain.Entities;

namespace Nexus.UserService.Application.Persistence;

public interface IUserDatabase
{
    DbSet<User>              Users          { get; }
    DbSet<Role>              Roles          { get; }
    DbSet<SellerProfile>     SellerProfiles { get; }
    DbSet<UserAddress>       Addresses      { get; }
    DbSet<Wallet>            Wallets        { get; }
    DbSet<WalletTransaction> WalletTxs      { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
