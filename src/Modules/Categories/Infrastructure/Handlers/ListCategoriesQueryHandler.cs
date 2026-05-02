using Microsoft.EntityFrameworkCore;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Categories.Application.DTOs;
using StaqFinance.Modules.Categories.Application.Queries;
using StaqFinance.Modules.Categories.Domain.Entities;

namespace StaqFinance.Modules.Categories.Infrastructure.Handlers;

internal sealed class ListCategoriesQueryHandler : IListCategoriesQueryHandler
{
    private readonly DbContext _context;

    public ListCategoriesQueryHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<CategoryResponse>>> HandleAsync(
        ListCategoriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var categories = await _context.Set<Category>()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CategoryResponse>>(categories);
    }
}
