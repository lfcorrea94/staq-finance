using Microsoft.AspNetCore.Authorization;
using StaqFinance.Modules.Tenancy.Application.Interfaces;
using StaqFinance.Modules.Tenancy.Domain.Interfaces;
using System.Security.Claims;

namespace StaqFinance.Api.Authorization;

public sealed class MustBelongToTenantHandler : AuthorizationHandler<MustBelongToTenantRequirement>
{
    private readonly ICurrentTenant _currentTenant;
    private readonly ITenantRepository _tenantRepository;

    public MustBelongToTenantHandler(ICurrentTenant currentTenant, ITenantRepository tenantRepository)
    {
        _currentTenant = currentTenant;
        _tenantRepository = tenantRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MustBelongToTenantRequirement requirement)
    {
        if (!_currentTenant.IsResolved)
        {
            context.Fail();
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            context.Fail();
            return;
        }

        var isMember = await _tenantRepository.ExistsMemberAsync(_currentTenant.TenantId, userId);

        if (isMember)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}
