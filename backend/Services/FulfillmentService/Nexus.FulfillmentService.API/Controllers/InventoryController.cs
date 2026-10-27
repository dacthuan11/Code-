using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.FulfillmentService.Application.UseCases.CommitInventory;
using Nexus.FulfillmentService.Application.UseCases.ReleaseInventory;
using Nexus.FulfillmentService.Application.UseCases.ReserveInventory;
using Nexus.FulfillmentService.Domain.Repositories;

namespace Nexus.FulfillmentService.API.Controllers;

/// <summary>
/// Controller quản lý tồn kho - expose 3 operations chính của inventory lifecycle:
/// Reserve → Commit (nếu thanh toán thành công) hoặc Release (nếu cancel/timeout).
/// Chỉ SELLER và ADMIN mới được gọi các endpoint này (policy "INVENTORY.MANAGE").
/// </summary>
[ApiController]
[Route("api/v1/inventory")]
[Authorize] // Require đăng nhập ở tất cả endpoints trong controller này
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IInventoryRepository _inventory;
    public InventoryController(IMediator mediator, IInventoryRepository inventory)
    {
        _mediator = mediator;
        _inventory = inventory;
    }

    /// <summary>
    /// Trả về available stock cho nhiều SkuId cùng lúc.
    /// Endpoint công khai (AllowAnonymous) để CatalogService gọi khi hiển thị danh sách sản phẩm.
    /// </summary>
    [HttpGet("stock")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStock([FromQuery] string skuIds, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(skuIds)) return Ok(new Dictionary<string, int>());
        var ids = skuIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();
        var result = await _inventory.GetAvailableStockBySkuIdsAsync(ids, ct);
        return Ok(result.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));
    }

    /// <summary>
    /// Giữ chỗ hàng khi user checkout.
    /// Trả về ReservationId và ExpiresAt để FE hiển thị countdown.
    /// </summary>
    [HttpPost("reserve")]
    [Authorize(Policy = "INVENTORY.MANAGE")] // SELLER hoặc ADMIN
    public async Task<IActionResult> Reserve([FromBody] ReserveInventoryCommand cmd, CancellationToken ct)
    {
        var result = await _mediator.Send(cmd, ct);
        return Ok(result); // Trả 200 + ReservationId + ExpiresAt
    }

    /// <summary>
    /// Xác nhận xuất kho sau khi payment thành công.
    /// Trả 204 No Content vì không có data mới cần trả về.
    /// </summary>
    [HttpPost("commit")]
    [Authorize(Policy = "INVENTORY.MANAGE")]
    public async Task<IActionResult> Commit([FromBody] CommitInventoryCommand cmd, CancellationToken ct)
    {
        await _mediator.Send(cmd, ct);
        return NoContent(); // 204 - thành công nhưng không có response body
    }

    /// <summary>
    /// Giải phóng hàng đang giữ chỗ khi đơn bị cancel hoặc payment thất bại.
    /// </summary>
    [HttpPost("release")]
    [Authorize(Policy = "INVENTORY.MANAGE")]
    public async Task<IActionResult> Release([FromBody] ReleaseInventoryCommand cmd, CancellationToken ct)
    {
        await _mediator.Send(cmd, ct);
        return NoContent();
    }
}
