using StaqFinance.Modules.Categories.Application.DTOs;
using StaqFinance.BuildingBlocks;

namespace StaqFinance.Modules.Categories.Application.Commands;

public sealed record CreateCategoryCommand(Guid TenantId, string Name);

public interface ICreateCategoryCommandHandler
{
    Task<Result<CategoryResponse>> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default);
}
