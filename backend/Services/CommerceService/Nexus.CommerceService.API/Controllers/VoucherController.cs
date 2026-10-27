using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.CommerceService.Application.UseCases.ManageVoucher;

namespace Nexus.CommerceService.API.Controllers;

[ApiController]
[Route("api/v1/vouchers")]
public class VoucherController : ControllerBase
{
    private readonly IMediator _mediator;
    public VoucherController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException());

    [HttpGet]
    public async Task<IActionResult> GetAvailable(
        [FromQuery] decimal orderTotal = 0,
        [FromQuery] Guid? sellerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new GetAvailableVouchersQuery(orderTotal, sellerId, page, pageSize)));

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateVoucherRequest req)
        => Ok(await _mediator.Send(new ValidateVoucherQuery(req.Code, req.OrderTotal, req.SellerId)));

    [HttpPost]
    [Authorize(Policy = "PRODUCT.CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateVoucherCommand cmd)
    {
        try
        {
            var id = await _mediator.Send(cmd);
            return Created($"api/v1/vouchers/{id}", new { id });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{voucherId:guid}")]
    [Authorize(Policy = "PRODUCT.DELETE")]
    public async Task<IActionResult> Deactivate(Guid voucherId)
    {
        try
        {
            await _mediator.Send(new DeactivateVoucherCommand(voucherId));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}

public record ValidateVoucherRequest(string Code, decimal OrderTotal, Guid? SellerId = null);
