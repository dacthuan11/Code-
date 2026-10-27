using MediatR;
using Nexus.CatalogService.Domain.Repositories;

namespace Nexus.CatalogService.Application.UseCases.DeleteCategory;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, Unit>
{
    private readonly ICategoryRepository _categories;
    public DeleteCategoryHandler(ICategoryRepository categories) => _categories = categories;

    public async Task<Unit> Handle(DeleteCategoryCommand cmd, CancellationToken ct)
    {
        var cat = await _categories.GetByIdAsync(cmd.CategoryId, ct)
            ?? throw new KeyNotFoundException($"Category {cmd.CategoryId} not found");

        cat.MarkDeleted();
        await _categories.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
