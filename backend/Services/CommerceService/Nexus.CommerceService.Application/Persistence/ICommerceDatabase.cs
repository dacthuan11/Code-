using Microsoft.EntityFrameworkCore;
using Nexus.CommerceService.Domain.Entities;

namespace Nexus.CommerceService.Application.Persistence;

public interface ICommerceDatabase
{
    DbSet<Order>        Orders        { get; }
    DbSet<OrderItem>    OrderItems    { get; }
    DbSet<Voucher>      Vouchers      { get; }
    DbSet<VoucherUsage> VoucherUsages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
