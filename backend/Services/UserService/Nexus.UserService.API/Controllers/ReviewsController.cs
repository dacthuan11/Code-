using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.UserService.Application.UseCases.CreateReview;
using Nexus.UserService.Application.UseCases.GetReviews;
using System.Security.Claims;

namespace Nexus.UserService.API.Controllers;

[ApiController]
[Route("api/v1/users/{targetUserId:guid}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReviewsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetReviews(Guid targetUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetReviewsQuery(targetUserId, page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview(Guid targetUserId, [FromBody] CreateReviewRequest req)
    {
        var reviewerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var id = await _mediator.Send(new CreateReviewCommand(reviewerId, targetUserId, req.OrderId, req.Rating, req.Comment));
        return CreatedAtAction(nameof(GetReviews), new { targetUserId }, new { id });
    }
}

public record CreateReviewRequest(int Rating, string Comment, Guid? OrderId = null);
