using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexus.CatalogService.Application.UseCases.GetShop;

namespace Nexus.CatalogService.API.Controllers;

[ApiController]
[Route("api/v1/shops")]
public class ShopsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ShopsController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET /api/v1/shops/{sellerId} — trang shop của seller</summary>
    [HttpGet("{sellerId:guid}")]
    public async Task<IActionResult> GetShop(Guid sellerId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetShopQuery(sellerId, page, pageSize), ct));
}
