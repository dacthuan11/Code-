using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.FulfillmentService.Application.UseCases.CreateShipment;
using Nexus.FulfillmentService.Application.UseCases.GetShipment;
using Nexus.FulfillmentService.Application.UseCases.ManageShipment;
using Nexus.FulfillmentService.Application.UseCases.UpdateShipmentStatus;

namespace Nexus.FulfillmentService.API.Controllers;

/// <summary>
/// Controller quản lý shipment - tạo đơn giao hàng và tra cứu trạng thái.
/// Tạo shipment cần quyền INVENTORY.MANAGE (SELLER/ADMIN).
/// Xem trạng thái thì authenticated user nào cũng xem được.
/// </summary>
[ApiController]
[Route("api/v1/shipments")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ShipmentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Tạo đơn giao hàng mới thông qua carrier API (GHN).
    /// Trả 201 Created với Location header trỏ về endpoint GetById.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "INVENTORY.MANAGE")]
    public async Task<IActionResult> Create([FromBody] CreateShipmentCommand cmd, CancellationToken ct)
    {
        var result = await _mediator.Send(cmd, ct);
        // CreatedAtAction tự gen Location header: /api/v1/shipments/{shipmentId}
        return CreatedAtAction(nameof(GetById), new { id = result.ShipmentId }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetShipmentByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrderId(Guid orderId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetShipmentByOrderIdQuery(orderId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Lấy timeline tracking của 1 vận đơn</summary>
    [HttpGet("{shipmentId:guid}/tracking")]
    public async Task<IActionResult> GetTracking(Guid shipmentId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetShipmentTrackingQuery(shipmentId), ct));

    /// <summary>
    /// GHN webhook - called by GHN delivery service to update shipment status.
    /// AllowAnonymous because GHN authenticates via X-GHN-Token header, not JWT.
    /// </summary>
    [HttpPost("webhook/ghn")]
    [AllowAnonymous]
    public async Task<IActionResult> GhnWebhook([FromBody] GhnWebhookPayload payload, CancellationToken ct)
    {
        if (payload.ShipmentId == Guid.Empty) return BadRequest();
        var success = await _mediator.Send(new UpdateShipmentStatusCommand(payload.ShipmentId, payload.Status), ct);
        return success ? Ok() : NotFound();
    }
}

public record GhnWebhookPayload(Guid ShipmentId, string Status, string? TrackingNumber);
