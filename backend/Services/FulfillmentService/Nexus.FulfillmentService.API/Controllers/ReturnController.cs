using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.FulfillmentService.Application.UseCases.ManageReturn;

namespace Nexus.FulfillmentService.API.Controllers;

[ApiController]
[Route("api/v1/returns")]
[Authorize]
public class ReturnController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReturnController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException());

    /// <summary>Buyer tạo yêu cầu hoàn trả</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReturnRequest req)
    {
        try
        {
            var id = await _mediator.Send(new CreateReturnRequestCommand(
                req.OrderId, req.SellerId, CurrentUserId,
                req.Reason, req.Description, req.RefundAmount, req.EvidenceImageUrls));
            return Created($"api/v1/returns/{id}", new { id });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Lấy danh sách return của 1 đơn hàng</summary>
    [HttpGet("order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrder(Guid orderId)
        => Ok(await _mediator.Send(new GetReturnsByOrderQuery(orderId)));

    /// <summary>Seller lấy danh sách return của shop mình</summary>
    [HttpGet("seller")]
    public async Task<IActionResult> GetSellerReturns(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _mediator.Send(new GetSellerReturnsQuery(CurrentUserId, status, page, pageSize));
        return Ok(new { items, total, page, pageSize });
    }

    /// <summary>Seller duyệt hoàn trả</summary>
    [HttpPatch("{returnId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid returnId, [FromBody] ApproveReturnRequestBody? req = null)
    {
        try
        {
            await _mediator.Send(new ApproveReturnCommand(returnId, CurrentUserId, req?.Note));
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>Seller từ chối hoàn trả</summary>
    [HttpPatch("{returnId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid returnId, [FromBody] RejectReturnRequestBody req)
    {
        try
        {
            await _mediator.Send(new RejectReturnCommand(returnId, CurrentUserId, req.Reason));
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}

public record CreateReturnRequest(
    Guid OrderId, Guid SellerId, string Reason, string Description,
    decimal RefundAmount, List<string>? EvidenceImageUrls = null);
public record ApproveReturnRequestBody(string? Note = null);
public record RejectReturnRequestBody(string Reason);
