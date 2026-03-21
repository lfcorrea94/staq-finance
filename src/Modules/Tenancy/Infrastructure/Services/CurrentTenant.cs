using StaqFinance.Modules.Tenancy.Application.Interfaces;

namespace StaqFinance.Modules.Tenancy.Infrastructure.Services;

public sealed class CurrentTenant : ICurrentTenant
{
    private Guid _tenantId;
    private string _workspaceSlug = string.Empty;

    public Guid TenantId => IsResolved
        ? _tenantId
        : throw new InvalidOperationException("Tenant has not been resolved for this request.");

    public string WorkspaceSlug => IsResolved
        ? _workspaceSlug
        : throw new InvalidOperationException("Tenant has not been resolved for this request.");

    public bool IsResolved { get; private set; }

    public void Set(Guid tenantId, string workspaceSlug)
    {
        _tenantId = tenantId;
        _workspaceSlug = workspaceSlug;
        IsResolved = true;
    }
}
