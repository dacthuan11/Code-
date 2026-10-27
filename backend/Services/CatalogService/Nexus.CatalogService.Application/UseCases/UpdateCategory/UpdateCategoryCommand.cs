using MediatR;

namespace Nexus.CatalogService.Application.UseCases.UpdateCategory;

public record UpdateCategoryCommand(Guid CategoryId, string Name) : IRequest<Unit>;
