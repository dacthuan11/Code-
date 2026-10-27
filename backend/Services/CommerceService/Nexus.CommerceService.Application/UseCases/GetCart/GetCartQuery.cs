using MediatR;
using Nexus.CommerceService.Domain.Repositories;

namespace Nexus.CommerceService.Application.UseCases.GetCart;

public record GetCartQuery(Guid UserId) : IRequest<CartDto?>;

public record CartDto(
    Guid CartId, Guid UserId,
    List<CartItemDto> Items,
    decimal TotalAmount);

public record CartItemDto(
    Guid ProductId, string ProductName,
    decimal UnitPrice, int Quantity, decimal Subtotal,
    string? ImageUrl = null);

public class GetCartHandler : IRequestHandler<GetCartQuery, CartDto?>
{
    private readonly ICartRepository _carts;
    public GetCartHandler(ICartRepository carts) => _carts = carts;

    public async Task<CartDto?> Handle(GetCartQuery q, CancellationToken ct)
    {
        var cart = await _carts.GetActiveByUserIdAsync(q.UserId, ct);
        if (cart is null) return null;

        var items = cart.Items.Select(i =>
            new CartItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity, i.ImageUrl)
        ).ToList();

        return new CartDto(cart.Id, cart.UserId, items, cart.Subtotal);
    }
}
