using MediatR;
using Nexus.CatalogService.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.CatalogService.Application.UseCases.RemoveSku;

public record RemoveSkuCommand(Guid ProductId, Guid SkuId) : IRequest;

public class RemoveSkuHandler : IRequestHandler<RemoveSkuCommand>
{
    private readonly ICatalogDatabase _db;
    private readonly IEventBus _eventBus;
    public RemoveSkuHandler(ICatalogDatabase db, IEventBus eventBus) { _db = db; _eventBus = eventBus; }

    public async Task Handle(RemoveSkuCommand cmd, CancellationToken ct)
    {
        var product = await _db.Products.Include(p => p.Skus)
            .FirstOrDefaultAsync(p => p.Id == cmd.ProductId, ct)
            ?? throw new KeyNotFoundException("Sản phẩm không tồn tại.");

        product.RemoveSku(cmd.SkuId);
        await _db.SaveChangesAsync(ct);
        await _eventBus.PublishAsync(new SkuRemovedEvent(cmd.ProductId, cmd.SkuId), ct);
    }
}
