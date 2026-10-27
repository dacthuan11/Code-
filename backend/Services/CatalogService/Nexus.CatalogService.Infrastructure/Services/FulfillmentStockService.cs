using Nexus.CatalogService.Application.Services;
using System.Net.Http.Json;

namespace Nexus.CatalogService.Infrastructure.Services;

public class FulfillmentStockService : IStockService
{
    private readonly IHttpClientFactory _http;
    public FulfillmentStockService(IHttpClientFactory http) => _http = http;

    public async Task<Dictionary<Guid, int>> GetAvailableStockAsync(IEnumerable<Guid> skuIds, CancellationToken ct = default)
    {
        var ids = skuIds.ToList();
        if (ids.Count == 0) return new();

        var client = _http.CreateClient("FulfillmentService");
        var query = string.Join(",", ids);
        var raw = await client.GetFromJsonAsync<Dictionary<string, int>>(
            $"/api/v1/inventory/stock?skuIds={query}", ct);
        if (raw == null) return new();

        return raw.ToDictionary(kv => Guid.Parse(kv.Key), kv => kv.Value);
    }
}
