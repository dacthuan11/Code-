using MediatR;
using Nexus.CatalogService.Domain.Entities;
using Nexus.CatalogService.Domain.Repositories;
using Nexus.Shared.Events;
using Nexus.Shared.Messaging;

namespace Nexus.CatalogService.Application.UseCases.CreateCategory;

/// <summary>
/// Handler tạo category - có logic tính level dựa vào parent.
/// </summary>
public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, CreateCategoryResult>
{
    private readonly ICategoryRepository _categories;
    private readonly IEventBus _eventBus;

    public CreateCategoryHandler(ICategoryRepository categories, IEventBus eventBus)
    {
        _categories = categories;
        _eventBus = eventBus;
    }

    public async Task<CreateCategoryResult> Handle(CreateCategoryCommand cmd, CancellationToken ct)
    {
        // Root category mặc định level 1, có parent thì level = parent.Level + 1
        int level = 1;
        if (cmd.ParentId.HasValue)
        {
            // Parent phải tồn tại mới được tạo con
            var parent = await _categories.GetByIdAsync(cmd.ParentId.Value, ct)
                ?? throw new KeyNotFoundException($"Parent category {cmd.ParentId} not found");

            level = parent.Level + 1;

            // Business rule cứng: tối đa 3 cấp category
            // Level 1 = root, Level 2 = sub, Level 3 = sub-sub
            // Nếu parent đang ở level 3 thì không cho tạo thêm con nữa
            if (level > 3) throw new InvalidOperationException("Category depth cannot exceed 3 levels");
        }

        // Category.Create nhận 3 tham số - không có description trong domain model
        // cmd.Description bị bỏ qua ở đây
        var category = Category.Create(cmd.Name, cmd.ParentId, level);
        await _categories.AddAsync(category, ct);
        await _categories.SaveChangesAsync(ct);

        // Notify các service khác nếu cần (ví dụ: nếu có service cache category tree)
        await _eventBus.PublishAsync(new CategoryCreatedEvent(category.Id, category.Name), ct);

        return new CreateCategoryResult(category.Id, category.Name, category.Slug);
    }
}
