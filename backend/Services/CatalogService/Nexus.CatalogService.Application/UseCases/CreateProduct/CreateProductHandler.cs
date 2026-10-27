using MediatR;
using Nexus.CatalogService.Domain.Entities;
using Nexus.CatalogService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.CatalogService.Application.UseCases.CreateProduct;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _products;
    private readonly IEventBus _eventBus;

    public CreateProductHandler(IProductRepository products, IEventBus eventBus)
    {
        _products = products;
        _eventBus = eventBus;
    }

    public async Task<Guid> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var product = Product.Create(cmd.Name, cmd.Description, cmd.Price, cmd.SellerId, cmd.CategoryId);

        await _products.AddAsync(product, ct);
        await _products.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(
            new ProductCreatedEvent(product.Id, product.SellerId, product.Name, product.Price), ct);

        return product.Id;
    }
}
