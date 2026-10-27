using MediatR;
using Nexus.CatalogService.Domain.Repositories;

namespace Nexus.CatalogService.Application.UseCases.AddProductImage;

public record AddProductImageCommand(Guid ProductId, string ImageUrl, Guid RequestedByUserId) : IRequest<bool>;

public class AddProductImageHandler : IRequestHandler<AddProductImageCommand, bool>
{
    private readonly IProductRepository _products;

    public AddProductImageHandler(IProductRepository products) => _products = products;

    public async Task<bool> Handle(AddProductImageCommand cmd, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return false;
        if (product.ImageUrls.Count >= 5) return false;

        product.AddImage(cmd.ImageUrl);
        await _products.SaveChangesAsync(ct);
        return true;
    }
}
