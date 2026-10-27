using MediatR;
using Nexus.CatalogService.Domain.Entities;
using Nexus.CatalogService.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.CatalogService.Application.UseCases.AddSku;

public record SkuAttributeDto(string Name, string Value);

public record AddSkuCommand(
    Guid ProductId,
    string SkuCode,
    decimal Price,
    List<SkuAttributeDto> Attributes
) : IRequest<Guid>;

public class AddSkuHandler : IRequestHandler<AddSkuCommand, Guid>
{
    private readonly ICatalogDatabase _db;
    private readonly IEventBus _eventBus;

    public AddSkuHandler(ICatalogDatabase db, IEventBus eventBus)
    {
        _db = db; _eventBus = eventBus;
    }

    public async Task<Guid> Handle(AddSkuCommand cmd, CancellationToken ct)
    {
        var product = await _db.Products.Include(p => p.Skus)
            .FirstOrDefaultAsync(p => p.Id == cmd.ProductId, ct)
            ?? throw new KeyNotFoundException($"Sản phẩm {cmd.ProductId} không tồn tại.");

        var attrs = cmd.Attributes
            .Select(a => SkuAttribute.Create(Guid.Empty, a.Name, a.Value))
            .ToList();

        var sku = ProductSku.Create(cmd.ProductId, cmd.SkuCode, cmd.Price, attrs);
        // fix attr's SkuId after sku has id
        foreach (var a in attrs) { /* EF sets FK automatically via navigation */ }

        product.AddSku(sku);
        await _db.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(
            new SkuAddedEvent(cmd.ProductId, sku.Id, cmd.SkuCode, product.SellerId), ct);

        return sku.Id;
    }
}
