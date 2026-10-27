using Microsoft.EntityFrameworkCore;
using Nexus.CatalogService.Domain.Entities;

namespace Nexus.CatalogService.Application.Persistence;

public interface ICatalogDatabase
{
    DbSet<Product>       Products       { get; }
    DbSet<ProductSku>    ProductSkus    { get; }
    DbSet<SkuAttribute>  SkuAttributes  { get; }
    DbSet<ProductReview> ProductReviews { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
