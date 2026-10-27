using MediatR;
using Nexus.CatalogService.Domain.Repositories;
using Nexus.Shared.Messaging;
using Nexus.Shared.Events;

namespace Nexus.CatalogService.Application.UseCases.UpdateProduct;

/// <summary>
/// Handler cập nhật product - tương tự Delete nhưng trả về result summary.
/// </summary>
public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, UpdateProductResult>
{
    private readonly IProductRepository _products;
    private readonly IEventBus _eventBus;

    public UpdateProductHandler(IProductRepository products, IEventBus eventBus)
    {
        _products = products;
        _eventBus = eventBus;
    }

    public async Task<UpdateProductResult> Handle(UpdateProductCommand cmd, CancellationToken ct)
    {
        // Không tìm thấy → throw, middleware catch và trả 404
        // TODO: check cmd.RequestedByUserId == product.SellerId (hoặc role Admin)
        var product = await _products.GetByIdAsync(cmd.ProductId, ct)
            ?? throw new KeyNotFoundException($"Product {cmd.ProductId} not found");

        // Product.Update chỉ nhận 4 tham số theo domain model hiện tại
        // cmd.Stock và cmd.ImageUrls bị bỏ qua ở đây:
        // - Stock: FulfillmentService quản lý
        // - ImageUrls: cần implement AddImage/ReplaceImages trên entity nếu muốn update ảnh
        product.Update(cmd.Name, cmd.Description, cmd.Price, cmd.CategoryId);

        await _products.UpdateAsync(product, ct);
        await _products.SaveChangesAsync(ct);

        // Notify các service khác về thay đổi (ví dụ: SearchService cần reindex)
        await _eventBus.PublishAsync(new ProductUpdatedEvent(product.Id, product.Name, product.Price), ct);

        return new UpdateProductResult(product.Id, product.Name, product.Price);
    }
}
