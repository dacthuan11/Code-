using MediatR;

namespace Nexus.CatalogService.Application.UseCases.DeleteCategory;

public record DeleteCategoryCommand(Guid CategoryId) : IRequest<Unit>;
