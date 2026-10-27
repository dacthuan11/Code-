using MediatR;
using Nexus.CatalogService.Domain.Repositories;

namespace Nexus.CatalogService.Application.UseCases.GetAllCategories;

/// <summary>Handler trả về danh sách tất cả category, sort theo Level rồi Name.</summary>
public class GetAllCategoriesHandler : IRequestHandler<GetAllCategoriesQuery, List<CategoryDto>>
{
    private readonly ICategoryRepository _categories;
    public GetAllCategoriesHandler(ICategoryRepository categories) => _categories = categories;

    public async Task<List<CategoryDto>> Handle(GetAllCategoriesQuery _, CancellationToken ct)
    {
        var cats = await _categories.GetAllAsync(ct);
        return cats.Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.ParentId, c.Level)).ToList();
    }
}
