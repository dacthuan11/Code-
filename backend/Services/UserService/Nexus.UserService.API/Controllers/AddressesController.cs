using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.UserService.Application.UseCases.ManageAddress;

namespace Nexus.UserService.API.Controllers;

[ApiController]
[Route("api/v1/users/me/addresses")]
[Authorize]
public class AddressesController : ControllerBase
{
    private readonly IMediator _mediator;
    public AddressesController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException());

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetAddressesQuery(CurrentUserId)));

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddAddressCommand cmd)
    {
        var realCmd = cmd with { UserId = CurrentUserId };
        var id = await _mediator.Send(realCmd);
        return Created($"api/v1/users/me/addresses/{id}", new { id });
    }

    [HttpPut("{addressId:guid}")]
    public async Task<IActionResult> Update(Guid addressId, [FromBody] UpdateAddressRequest req)
    {
        try
        {
            await _mediator.Send(new UpdateAddressCommand(CurrentUserId, addressId,
                req.Label, req.RecipientName, req.Phone, req.AddressLine,
                req.Province, req.District, req.Ward, req.ToDistrictId, req.ToWardCode));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{addressId:guid}")]
    public async Task<IActionResult> Delete(Guid addressId)
    {
        try { await _mediator.Send(new DeleteAddressCommand(CurrentUserId, addressId)); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPatch("{addressId:guid}/default")]
    public async Task<IActionResult> SetDefault(Guid addressId)
    {
        await _mediator.Send(new SetDefaultAddressCommand(CurrentUserId, addressId));
        return NoContent();
    }
}

public record UpdateAddressRequest(
    string Label, string RecipientName, string Phone,
    string AddressLine, string Province, string District, string Ward,
    int ToDistrictId, string ToWardCode);
