using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.UserService.Application.UseCases.RegisterSeller;

namespace Nexus.UserService.API.Controllers;

[ApiController]
[Route("api/v1/seller")]
[Authorize]
public class SellerController : ControllerBase
{
    private readonly IMediator _mediator;
    public SellerController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException());

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterSellerRequest req)
    {
        try
        {
            var profileId = await _mediator.Send(new RegisterSellerCommand(CurrentUserId, req.ShopName, req.Description));
            return Ok(new { profileId, message = "Đăng ký shop thành công." });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var result = await _mediator.Send(new GetSellerProfileQuery(CurrentUserId));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateSellerProfileRequest req)
    {
        await _mediator.Send(new UpdateSellerProfileCommand(CurrentUserId, req.ShopName, req.Description, req.LogoUrl, req.BannerUrl));
        return NoContent();
    }

    [HttpGet("{sellerId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicProfile(Guid sellerId)
    {
        var result = await _mediator.Send(new GetSellerProfileQuery(sellerId));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{sellerId:guid}/verify")]
    [Authorize(Policy = "USER.DELETE")]
    public async Task<IActionResult> Verify(Guid sellerId)
    {
        await _mediator.Send(new VerifySellerCommand(sellerId));
        return NoContent();
    }
}

public record RegisterSellerRequest(string ShopName, string Description);
public record UpdateSellerProfileRequest(string ShopName, string Description, string? LogoUrl, string? BannerUrl);
