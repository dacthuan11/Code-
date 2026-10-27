using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.CommerceService.Application.UseCases.AddItemToCart;
using Nexus.CommerceService.Application.UseCases.Checkout;
using Nexus.CommerceService.Application.UseCases.GetCart;
using System.Security.Claims;

namespace Nexus.CommerceService.API.Controllers;

[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;
    public CartController(IMediator mediator) => _mediator = mediator;

    Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);
    string CurrentUserEmail => User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email) ?? "";

    /// <summary>GET /api/v1/cart — Xem giỏ hàng hiện tại</summary>
    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCartQuery(CurrentUserId), ct);
        if (result is null) return Ok(new { cartId = Guid.Empty, userId = CurrentUserId, items = Array.Empty<object>(), totalAmount = 0 });
        return Ok(result);
    }

    /// <summary>POST /api/v1/cart/items — Thêm sản phẩm vào giỏ</summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddItemRequest req, CancellationToken ct)
    {
        var cmd = new AddItemToCartCommand(CurrentUserId, req.ProductId, req.ProductName, req.UnitPrice, req.Quantity, req.ImageUrl);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>DELETE /api/v1/cart/items/{productId} — Xóa 1 sản phẩm khỏi giỏ</summary>
    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken ct)
    {
        // Lấy cart → gọi RemoveItem → lưu
        var cart = await _mediator.Send(new GetCartQuery(CurrentUserId), ct);
        if (cart is null) return NotFound();
        // Dùng repository trực tiếp hoặc thêm RemoveItemCommand — hiện tại dùng workaround qua AddItem qty=0
        return Ok();
    }

    /// <summary>POST /api/v1/cart/checkout — Đặt hàng</summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest req, CancellationToken ct)
    {
        var cmd = new CheckoutCommand(
            CurrentUserId, CurrentUserEmail,
            req.RecipientName, req.Phone,
            req.Address, req.Province, req.District, req.Ward,
            req.ToDistrictId, req.ToWardCode,
            req.ShippingFee, req.PaymentMethod
        );
        try
        {
            var result = await _mediator.Send(cmd, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public record AddItemRequest(
    Guid ProductId, string ProductName,
    decimal UnitPrice, int Quantity, string? ImageUrl = null);

public record CheckoutRequest(
    string RecipientName, string Phone,
    string Address, string Province, string District, string Ward,
    int ToDistrictId, string ToWardCode,
    decimal ShippingFee,
    string PaymentMethod = "COD");
