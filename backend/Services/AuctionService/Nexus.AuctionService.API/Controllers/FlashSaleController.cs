using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.AuctionService.Application.UseCases.ManageFlashSale;

namespace Nexus.AuctionService.API.Controllers;

[ApiController]
[Route("api/v1/flashsales")]
public class FlashSaleController : ControllerBase
{
    private readonly IMediator _mediator;
    public FlashSaleController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException());

    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _mediator.Send(new GetActiveFlashSalesQuery(page, pageSize)));

    [HttpGet("{flashSaleId:guid}")]
    public async Task<IActionResult> GetById(Guid flashSaleId)
    {
        var result = await _mediator.Send(new GetFlashSaleQuery(flashSaleId));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateFlashSaleCommand cmd)
    {
        try
        {
            var realCmd = cmd with { SellerId = CurrentUserId };
            var id = await _mediator.Send(realCmd);
            return Created($"api/v1/flashsales/{id}", new { id });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{flashSaleId:guid}/activate")]
    [Authorize]
    public async Task<IActionResult> Activate(Guid flashSaleId)
    {
        try
        {
            await _mediator.Send(new ActivateFlashSaleCommand(flashSaleId, CurrentUserId));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{flashSaleItemId:guid}/reserve")]
    [Authorize]
    public async Task<IActionResult> Reserve(Guid flashSaleItemId)
    {
        try
        {
            var success = await _mediator.Send(new ReserveFlashSaleSlotCommand(flashSaleItemId));
            return success
                ? Ok(new { message = "Đặt slot thành công." })
                : BadRequest(new { message = "Hết slot." });
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
