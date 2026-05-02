using StaqFinance.Modules.Categories.Application.DTOs;
using StaqFinance.BuildingBlocks;

namespace StaqFinance.Modules.Categories.Application.Queries;

public sealed record ListCategoriesQuery(Guid TenantId);

public interface IListCategoriesQueryHandler
{
    Task<Result<IReadOnlyList<CategoryResponse>>> HandleAsync(ListCategoriesQuery query, CancellationToken cancellationToken = default);
}
