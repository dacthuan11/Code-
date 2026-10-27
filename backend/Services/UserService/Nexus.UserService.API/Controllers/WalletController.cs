using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.UserService.Application.UseCases.ManageWallet;

namespace Nexus.UserService.API.Controllers;

[ApiController]
[Route("api/v1/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IMediator _mediator;
    public WalletController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException());

    [HttpGet]
    public async Task<IActionResult> GetWallet()
    {
        try { return Ok(await _mediator.Send(new GetWalletQuery(CurrentUserId))); }
        catch (KeyNotFoundException) { return NotFound(new { message = "Ví chưa được khởi tạo." }); }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _mediator.Send(new GetWalletHistoryQuery(CurrentUserId, page, pageSize));
        return Ok(new { items, total, page, pageSize });
    }

    [HttpPost("credit")]
    [Authorize(Policy = "USER.DELETE")]
    public async Task<IActionResult> Credit([FromBody] CreditRequest req)
    {
        await _mediator.Send(new CreditWalletCommand(req.UserId, req.Amount, req.Description, req.Reference));
        return NoContent();
    }
}

public record CreditRequest(Guid UserId, decimal Amount, string Description, string Reference);
