namespace StaqFinance.Modules.Tenancy.Application.Interfaces;

public interface ICurrentTenant
{
    Guid TenantId { get; }
    string WorkspaceSlug { get; }
    bool IsResolved { get; }
    void Set(Guid tenantId, string workspaceSlug);
}
