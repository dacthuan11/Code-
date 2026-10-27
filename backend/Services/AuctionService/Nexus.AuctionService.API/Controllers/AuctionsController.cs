using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.AuctionService.Application.UseCases.CreateAuction;
using Nexus.AuctionService.Application.UseCases.EndAuction;
using Nexus.AuctionService.Application.UseCases.GetAuction;
using Nexus.AuctionService.Application.UseCases.PlaceBid;
using System.Security.Claims;

namespace Nexus.AuctionService.API.Controllers;

[ApiController]
[Route("api/v1/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuctionsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [Authorize(Policy = "AUCTION.MANAGE")]
    public async Task<IActionResult> Create([FromBody] CreateAuctionCommand cmd, CancellationToken ct)
    {
        var sellerId    = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var sellerEmail = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email) ?? "";
        var result = await _mediator.Send(cmd with { SellerId = sellerId, SellerEmail = sellerEmail }, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.AuctionId }, result);
    }

    [HttpPost("{id:guid}/bids")]
    [Authorize(Policy = "AUCTION.BID")]
    public async Task<IActionResult> PlaceBid(Guid id, [FromBody] PlaceBidRequest request, CancellationToken ct)
    {
        var bidderId    = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var bidderEmail = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email) ?? "";
        var result = await _mediator.Send(new PlaceBidCommand(id, bidderId, bidderEmail, request.Amount), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "AUCTION.MANAGE")]
    public async Task<IActionResult> ActivateAuction(Guid id, CancellationToken ct)
    {
        var repo = HttpContext.RequestServices.GetRequiredService<Nexus.AuctionService.Domain.Repositories.IAuctionRepository>();
        var auction = await repo.GetByIdAsync(id, ct);
        if (auction is null) return NotFound();
        auction.Activate();
        await repo.SaveChangesAsync(ct);
        return Ok(new { id = auction.Id, status = auction.Status.ToString() });
    }

    [HttpPost("{id:guid}/end")]
    [Authorize(Policy = "AUCTION.MANAGE")]
    public async Task<IActionResult> EndAuction(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new EndAuctionCommand(id), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAuctionByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/bids")]
    public async Task<IActionResult> GetBids(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBidsQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuctionsQuery(status, page, pageSize), ct);
        return Ok(result);
    }
}

public record PlaceBidRequest(decimal Amount);
