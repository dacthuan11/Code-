namespace Nexus.CatalogService.Application.Services;

public interface IStockService
{
    Task<Dictionary<Guid, int>> GetAvailableStockAsync(IEnumerable<Guid> skuIds, CancellationToken ct = default);
}
