using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.CatalogService.Application.UseCases.CreateProduct;
using Nexus.CatalogService.Application.UseCases.DeleteProduct;
using Nexus.CatalogService.Application.UseCases.GetProductById;
using Nexus.CatalogService.Application.UseCases.SearchProducts;
using Nexus.CatalogService.Application.UseCases.AddProductImage;
using Nexus.CatalogService.Application.UseCases.UpdateProduct;
using Nexus.CatalogService.Application.UseCases.AddSku;
using Nexus.CatalogService.Application.UseCases.UpdateSku;
using Nexus.CatalogService.Application.UseCases.RemoveSku;
using Nexus.CatalogService.Application.UseCases.CreateProductReview;
using Nexus.CatalogService.Application.UseCases.GetProductReviews;
using System.Security.Claims;

namespace Nexus.CatalogService.API.Controllers;

/// <summary>
/// Controller quản lý sản phẩm - thin controller pattern.
/// Controller không chứa business logic, chỉ nhận request → gửi command/query qua MediatR.
/// </summary>
[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// GET /api/v1/products?keyword=phone&amp;minPrice=100&amp;page=1&amp;pageSize=20
    /// Không cần auth - ai cũng search được
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchProductsQuery query)
        => Ok(await _mediator.Send(query));
    // [FromQuery] bind từng field của record SearchProductsQuery từ query string

    /// <summary>
    /// POST /api/v1/products
    /// Cần policy PRODUCT.CREATE - thường là role Seller hoặc Admin
    /// SellerId nên lấy từ JWT claims trong handler, không tin tưởng từ body
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "PRODUCT.CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand cmd)
    {
        var id = await _mediator.Send(cmd);
        // Trả 201 Created với Location header trỏ về GET endpoint
        // new { id } là route values, new { id } thứ hai là response body
        return CreatedAtAction(nameof(Search), new { id }, new { id });
    }

    /// <summary>
    /// GET /api/v1/products/{id} - chi tiết sản phẩm, response khớp Product interface ở FE.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));
        if (result is null) return NotFound();
        return Ok(new {
            productId    = result.Id,
            name         = result.Name,
            description  = result.Description,
            price        = result.Price,
            stockQuantity = result.Stock,
            categoryId   = result.CategoryId,
            categoryName = result.CategoryName,
            sellerId     = result.SellerId,
            imageUrl     = result.ImageUrls.FirstOrDefault(),
            createdAt    = result.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "PRODUCT.UPDATE")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand cmd, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(cmd with { ProductId = id, RequestedByUserId = userId }, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PRODUCT.DELETE")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _mediator.Send(new DeleteProductCommand(id, userId), ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/products/{id}/images
    /// Thêm URL ảnh vào danh sách ImageUrls của sản phẩm (tối đa 5 ảnh).
    /// Thực tế upload file sẽ xử lý qua CDN/object storage trước, rồi gửi URL về đây.
    /// </summary>
    [HttpPost("{id:guid}/images")]
    [Authorize(Policy = "PRODUCT.UPDATE")]
    public async Task<IActionResult> AddImage(Guid id, [FromBody] AddImageRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new AddProductImageCommand(id, request.ImageUrl, userId), ct);
        return result ? Ok(new { message = "Image added." }) : BadRequest(new { message = "Product not found or max images reached." });
    }

    // ── SKU endpoints ────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/skus")]
    [Authorize(Policy = "PRODUCT.UPDATE")]
    public async Task<IActionResult> AddSku(Guid id, [FromBody] AddSkuCommand cmd, CancellationToken ct)
    {
        var skuId = await _mediator.Send(cmd with { ProductId = id }, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { skuId });
    }

    [HttpPut("{id:guid}/skus/{skuId:guid}")]
    [Authorize(Policy = "PRODUCT.UPDATE")]
    public async Task<IActionResult> UpdateSku(Guid id, Guid skuId, [FromBody] UpdateSkuRequest req, CancellationToken ct)
    {
        await _mediator.Send(new UpdateSkuCommand(id, skuId, req.Price), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/skus/{skuId:guid}")]
    [Authorize(Policy = "PRODUCT.UPDATE")]
    public async Task<IActionResult> RemoveSku(Guid id, Guid skuId, CancellationToken ct)
    {
        await _mediator.Send(new RemoveSkuCommand(id, skuId), ct);
        return NoContent();
    }

    // ── Review endpoints ─────────────────────────────────────────────────────

    [HttpGet("{id:guid}/reviews")]
    public async Task<IActionResult> GetReviews(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetProductReviewsQuery(id, page, pageSize), ct));

    [HttpPost("{id:guid}/reviews")]
    [Authorize]
    public async Task<IActionResult> CreateReview(Guid id, [FromBody] CreateProductReviewCommand cmd, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var name   = User.FindFirstValue(ClaimTypes.Name) ?? "Người dùng";
        var reviewId = await _mediator.Send(cmd with { ProductId = id, ReviewerId = userId, ReviewerName = name }, ct);
        return CreatedAtAction(nameof(GetReviews), new { id }, new { reviewId });
    }
}

public record AddImageRequest(string ImageUrl);
public record UpdateSkuRequest(decimal Price);
