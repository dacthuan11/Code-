using MediatR;
using Nexus.CatalogService.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Nexus.CatalogService.Application.UseCases.UpdateSku;

public record UpdateSkuCommand(Guid ProductId, Guid SkuId, decimal Price) : IRequest;

public class UpdateSkuHandler : IRequestHandler<UpdateSkuCommand>
{
    private readonly ICatalogDatabase _db;
    public UpdateSkuHandler(ICatalogDatabase db) => _db = db;

    public async Task Handle(UpdateSkuCommand cmd, CancellationToken ct)
    {
        var sku = await _db.ProductSkus.FirstOrDefaultAsync(
            s => s.Id == cmd.SkuId && s.ProductId == cmd.ProductId, ct)
            ?? throw new KeyNotFoundException("SKU không tồn tại.");

        sku.UpdatePrice(cmd.Price);
        await _db.SaveChangesAsync(ct);
    }
}
