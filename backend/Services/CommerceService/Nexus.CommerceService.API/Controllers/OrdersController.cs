using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.CommerceService.Application.UseCases.CancelOrder;
using Nexus.CommerceService.Application.UseCases.CompleteOrder;
using Nexus.CommerceService.Application.UseCases.ConfirmPayment;
using Nexus.CommerceService.Application.UseCases.GetOrders;
using Nexus.CommerceService.Application.UseCases.ManageSeller;
using Nexus.CommerceService.Domain.Entities;
using Nexus.CommerceService.Domain.Repositories;
using Nexus.CommerceService.Infrastructure.Payment;
using Nexus.CommerceService.Infrastructure.Persistence;
using System.Security.Claims;

namespace Nexus.CommerceService.API.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IOrderRepository _orders;
    private readonly CommerceDbContext _db;

    public OrdersController(IMediator mediator, IOrderRepository orders, CommerceDbContext db)
    {
        _mediator = mediator;
        _orders = orders;
        _db = db;
    }

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException());

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new GetOrdersQuery(userId, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new GetOrdersQuery(userId, 1, 100), ct);
        var order = result.Items.FirstOrDefault(o => o.OrderId == id);
        return order is null ? NotFound() : Ok(order);
    }

    // Hủy đơn hàng với lý do
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var email = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email) ?? "";
        try
        {
            await _mediator.Send(new CancelOrderCommand(id, userId, request.Reason, email), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Seller confirm đơn COD / PreOrder
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null) return NotFound();
        order.Confirm();
        await _orders.UpdateAsync(order, ct);
        await _orders.SaveChangesAsync(ct);
        return Ok(new { status = order.Status.ToString() });
    }

    // Seller bắt đầu chuẩn bị hàng
    [HttpPost("{id:guid}/process")]
    public async Task<IActionResult> Process(Guid id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null) return NotFound();
        order.StartProcessing();
        await _orders.UpdateAsync(order, ct);
        await _orders.SaveChangesAsync(ct);
        return Ok(new { status = order.Status.ToString() });
    }

    // Đóng gói xong, chờ GHN đến lấy
    [HttpPost("{id:guid}/ready")]
    public async Task<IActionResult> ReadyToShip(Guid id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null) return NotFound();
        order.MarkReadyToShip();
        await _orders.UpdateAsync(order, ct);
        await _orders.SaveChangesAsync(ct);
        return Ok(new { status = order.Status.ToString() });
    }

    // GHN webhook / manual: đánh dấu đã giao
    [HttpPost("{id:guid}/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var success = await _mediator.Send(new CompleteOrderCommand(id), ct);
        return success ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    // Lịch sử thay đổi trạng thái đơn hàng
    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null) return NotFound();
        if (order.UserId != userId) return Forbid();

        var history = await _db.OrderStatusHistories
            .Where(h => h.OrderId == id && !h.IsDeleted)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new { h.Status, h.Note, h.Actor, h.CreatedAt })
            .ToListAsync(ct);

        return Ok(history);
    }

    // Danh sách lý do hủy chuẩn (GHN + e-commerce VN)
    [HttpGet("cancellation-reasons")]
    [AllowAnonymous]
    public IActionResult GetCancellationReasons()
    {
        return Ok(CancellationReasons.AllReasons);
    }

    // ── Seller endpoints ──────────────────────────────────────────────────────

    [HttpGet("seller")]
    public async Task<IActionResult> GetSellerOrders(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await _mediator.Send(new GetSellerOrdersQuery(CurrentUserId, status, page, pageSize), ct);
        return Ok(new { items, total, page, pageSize });
    }

    [HttpPatch("{orderId:guid}/seller-confirm")]
    public async Task<IActionResult> SellerConfirm(Guid orderId, CancellationToken ct)
    {
        try { await _mediator.Send(new ConfirmOrderCommand(orderId, CurrentUserId), ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPatch("{orderId:guid}/ready-to-ship")]
    public async Task<IActionResult> SellerReadyToShip(Guid orderId, CancellationToken ct)
    {
        try { await _mediator.Send(new MarkReadyToShipCommand(orderId, CurrentUserId), ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPatch("{orderId:guid}/delivered")]
    public async Task<IActionResult> SellerMarkDelivered(Guid orderId, CancellationToken ct)
    {
        try { await _mediator.Send(new MarkDeliveredCommand(orderId, CurrentUserId), ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("seller/analytics")]
    public async Task<IActionResult> GetSellerAnalytics(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetSellerAnalyticsQuery(CurrentUserId, from, to), ct));
}

[ApiController]
[Route("api/v1/payment")]
public class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IVnpayGateway _vnpay;

    public PaymentController(IMediator mediator, IVnpayGateway vnpay)
    {
        _mediator = mediator;
        _vnpay = vnpay;
    }

    [HttpPost("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromBody] PaymentCallbackRequest request, CancellationToken ct)
    {
        var success = await _mediator.Send(
            new ConfirmPaymentCommand(request.OrderId, request.PaymentRef ?? $"PAY-{request.OrderId:N}"), ct);
        return success ? Ok(new { success = true }) : BadRequest(new { success = false, message = "Order not found or already paid" });
    }

    [HttpPost("vnpay/create")]
    [Authorize]
    public IActionResult CreateVnpayPayment([FromBody] VnpayCreateRequest req)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var returnUrl = req.ReturnUrl ?? $"{Request.Scheme}://{Request.Host}/api/v1/payment/vnpay-return";
        var url = _vnpay.BuildPaymentUrl(req.OrderId, req.Amount,
            $"Thanh toan don hang {req.OrderId}", returnUrl, ipAddress);
        return Ok(new { paymentUrl = url });
    }

    [HttpGet("vnpay-return")]
    [AllowAnonymous]
    public async Task<IActionResult> VnpayReturn([FromQuery] Dictionary<string, string> queryParams, CancellationToken ct)
    {
        if (!_vnpay.ValidateCallback(queryParams))
            return BadRequest(new { success = false, message = "Chữ ký không hợp lệ." });

        var status = _vnpay.GetTransactionStatus(queryParams);
        if (status != "Success")
            return Ok(new { success = false, message = "Thanh toán thất bại." });

        if (!queryParams.TryGetValue("vnp_TxnRef", out var txnRef)) return BadRequest();
        var orderIdStr = txnRef.PadRight(32, '0')[..32];
        if (!Guid.TryParse(orderIdStr, out var orderId)) return BadRequest();

        var paymentRef = queryParams.GetValueOrDefault("vnp_TransactionNo", txnRef);
        var success = await _mediator.Send(new ConfirmPaymentCommand(orderId, paymentRef), ct);
        return Ok(new { success, paymentRef });
    }
}

public record CancelOrderRequest(string Reason);
public record PaymentCallbackRequest(Guid OrderId, string? PaymentRef);
public record VnpayCreateRequest(Guid OrderId, decimal Amount, string? ReturnUrl = null);
