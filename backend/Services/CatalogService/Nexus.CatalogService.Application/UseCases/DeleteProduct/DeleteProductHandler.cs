using MediatR;
using Nexus.CatalogService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.CatalogService.Application.UseCases.DeleteProduct;

/// <summary>
/// Handler xử lý việc "xóa" sản phẩm.
/// Luồng: tìm product → soft delete → save → publish event.
/// </summary>
public class DeleteProductHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IProductRepository _products;
    private readonly IEventBus _eventBus;

    public DeleteProductHandler(IProductRepository products, IEventBus eventBus)
    {
        _products = products;
        _eventBus = eventBus;
    }

    public async Task Handle(DeleteProductCommand cmd, CancellationToken ct)
    {
        // Nếu không tìm thấy → throw KeyNotFoundException
        // Middleware global exception handler sẽ catch và trả 404
        var product = await _products.GetByIdAsync(cmd.ProductId, ct)
            ?? throw new KeyNotFoundException($"Product {cmd.ProductId} not found");

        // MarkDeleted() là từ BaseEntity - set IsDeleted = true + Touch()
        // HasQueryFilter trong DbContext sẽ tự filter !IsDeleted, nên record "biến mất" khỏi query
        product.MarkDeleted();

        await _products.UpdateAsync(product, ct);
        await _products.SaveChangesAsync(ct);

        // Publish event để các service khác biết (ví dụ: OrderService, SearchService)
        // Dùng IEventBus thay vì MassTransit trực tiếp để dễ mock khi unit test
        await _eventBus.PublishAsync(new ProductDeletedEvent(product.Id), ct);
    }
}
