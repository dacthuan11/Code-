using MediatR;
using Nexus.CommerceService.Domain.Entities;
using Nexus.CommerceService.Domain.Repositories;

namespace Nexus.CommerceService.Application.UseCases.AddItemToCart;

// Handler xử lý AddItemToCartCommand - đây là "use case" trong Clean Architecture.
// Handler này chỉ biết về domain + repository interfaces, không biết gì về HTTP hay EF Core.
public class AddItemToCartHandler : IRequestHandler<AddItemToCartCommand, AddItemToCartResult>
{
    private readonly ICartRepository _carts;

    public AddItemToCartHandler(ICartRepository carts) => _carts = carts;

    public async Task<AddItemToCartResult> Handle(AddItemToCartCommand cmd, CancellationToken ct)
    {
        // Tìm giỏ hàng đang active của user
        // Nếu chưa có (lần đầu dùng hoặc giỏ cũ đã expired) thì tạo mới
        var cart = await _carts.GetActiveByUserIdAsync(cmd.UserId, ct);

        if (cart is null)
        {
            cart = Cart.Create(cmd.UserId);
            cart.AddItem(cmd.ProductId, cmd.ProductName, cmd.UnitPrice, cmd.Quantity, cmd.ImageUrl);
            await _carts.AddAsync(cart, ct);  // graph traversal picks up CartItems
        }
        else
        {
            var newItem = cart.AddItem(cmd.ProductId, cmd.ProductName, cmd.UnitPrice, cmd.Quantity, cmd.ImageUrl);
            if (newItem is not null)
                await _carts.AddCartItemAsync(newItem, ct);  // explicit INSERT, no EF tracking guesswork
        }

        // Không gọi UpdateAsync — EF change tracker tự detect modification
        // trên entity đã được fetch hoặc đã được Add vào context
        await _carts.SaveChangesAsync(ct);

        // Trả về thông tin đủ để FE update badge số lượng và tổng tiền trong giỏ
        return new AddItemToCartResult(cart.Id, cart.Items.Count, cart.Subtotal);
    }
}
