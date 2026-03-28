using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Identity.Application.DTOs;
using StaqFinance.Modules.Identity.Application.Queries;
using StaqFinance.Modules.Identity.Domain.Entities;
using StaqFinance.Modules.Tenancy.Domain.Entities;

namespace StaqFinance.Modules.Identity.Infrastructure.Handlers;

internal sealed class GetMeQueryHandler : IGetMeQueryHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly DbContext _context;

    public GetMeQueryHandler(UserManager<ApplicationUser> userManager, DbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<Result<MeResponse>> HandleAsync(
        GetMeQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(query.UserId.ToString());

        if (user is null)
            return Result.Failure<MeResponse>(
                Error.NotFound("User.NotFound", "Usuário não encontrado."));

        var tenantId = await _context.Set<TenantUser>()
            .Where(tu => tu.UserId == user.Id)
            .Select(tu => (Guid?)tu.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantId is null)
            return Result.Failure<MeResponse>(
                Error.NotFound("Workspace.NotFound", "Nenhum workspace encontrado para este usuário."));

        var tenant = await _context.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId.Value, cancellationToken);

        if (tenant is null)
            return Result.Failure<MeResponse>(
                Error.NotFound("Workspace.NotFound", "Workspace não encontrado."));

        return Result.Success(new MeResponse(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.CreatedAt,
            new WorkspaceDto(tenant.Name, tenant.Slug, tenant.Currency)));
    }
}
