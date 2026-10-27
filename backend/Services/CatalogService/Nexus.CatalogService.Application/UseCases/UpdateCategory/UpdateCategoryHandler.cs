using MediatR;
using Nexus.CatalogService.Domain.Repositories;

namespace Nexus.CatalogService.Application.UseCases.UpdateCategory;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Unit>
{
    private readonly ICategoryRepository _categories;
    public UpdateCategoryHandler(ICategoryRepository categories) => _categories = categories;

    public async Task<Unit> Handle(UpdateCategoryCommand cmd, CancellationToken ct)
    {
        var cat = await _categories.GetByIdAsync(cmd.CategoryId, ct)
            ?? throw new KeyNotFoundException($"Category {cmd.CategoryId} not found");

        cat.Update(cmd.Name);
        await _categories.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
