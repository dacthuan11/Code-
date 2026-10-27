using MediatR;

namespace Nexus.CatalogService.Application.UseCases.CreateCategory;

/// <summary>
/// Command tạo category mới.
/// Description có trong command nhưng domain entity Category không có field description
/// → bị bỏ qua trong handler. Nếu cần sau này thì thêm vào entity + migration.
/// </summary>
public record CreateCategoryCommand(
    string Name,
    string? Description,  // không dùng trong domain hiện tại, giữ lại cho backward compat
    Guid? ParentId        // null = tạo root category (level 1)
) : IRequest<CreateCategoryResult>;

/// <summary>Thông tin category vừa tạo - trả slug để FE dùng cho URL ngay.</summary>
public record CreateCategoryResult(Guid CategoryId, string Name, string Slug);
