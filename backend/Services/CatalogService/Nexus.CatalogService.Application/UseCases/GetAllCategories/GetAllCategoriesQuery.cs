using MediatR;

namespace Nexus.CatalogService.Application.UseCases.GetAllCategories;

/// <summary>Query lấy tất cả danh mục - dùng cho dropdown filter ở trang chủ.</summary>
public record GetAllCategoriesQuery : IRequest<List<CategoryDto>>;

/// <summary>DTO category - field names khớp với Category interface ở frontend.</summary>
public record CategoryDto(
    Guid CategoryId,
    string Name,
    string Slug,
    Guid? ParentCategoryId,
    int Level
);
