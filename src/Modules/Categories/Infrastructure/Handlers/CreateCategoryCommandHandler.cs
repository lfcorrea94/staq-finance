using Microsoft.EntityFrameworkCore;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Categories.Application.Commands;
using StaqFinance.Modules.Categories.Application.DTOs;
using StaqFinance.Modules.Categories.Domain.Entities;

namespace StaqFinance.Modules.Categories.Infrastructure.Handlers;

internal sealed class CreateCategoryCommandHandler : ICreateCategoryCommandHandler
{
    private readonly DbContext _context;

    public CreateCategoryCommandHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<Result<CategoryResponse>> HandleAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.Set<Category>()
            .AnyAsync(c => c.TenantId == command.TenantId && c.Name == command.Name, cancellationToken);

        if (exists)
            return Result.Failure<CategoryResponse>(
                Error.Conflict("Category.DuplicateName", $"A category named '{command.Name}' already exists in this workspace."));

        var category = Category.Create(command.TenantId, command.Name);

        _context.Set<Category>().Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new CategoryResponse(category.Id, category.Name, category.CreatedAt));
    }
}
