using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.CatalogService.Application.UseCases.CreateCategory;
using Nexus.CatalogService.Application.UseCases.DeleteCategory;
using Nexus.CatalogService.Application.UseCases.GetAllCategories;
using Nexus.CatalogService.Application.UseCases.UpdateCategory;

namespace Nexus.CatalogService.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CategoriesController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET /api/v1/categories - lấy tất cả danh mục (công khai)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetAllCategoriesQuery()));

    /// <summary>POST /api/v1/categories - tạo danh mục mới</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateCategoryCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return CreatedAtAction(nameof(GetAll), new { id = result.CategoryId }, result);
    }

    /// <summary>PUT /api/v1/categories/{id} - đổi tên/chỉnh sửa danh mục</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest req)
    {
        await _mediator.Send(new UpdateCategoryCommand(id, req.Name));
        return NoContent();
    }

    /// <summary>DELETE /api/v1/categories/{id} - xóa danh mục</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteCategoryCommand(id));
        return NoContent();
    }
}

public record UpdateCategoryRequest(string Name);
