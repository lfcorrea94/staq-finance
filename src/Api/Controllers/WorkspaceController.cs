using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaqFinance.Modules.Tenancy.Application.Interfaces;

namespace StaqFinance.Api.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceSlug}")]
[Authorize(Policy = "MustBelongToTenant")]
public sealed class WorkspaceController : ControllerBase
{
    private readonly ICurrentTenant _currentTenant;

    public WorkspaceController(ICurrentTenant currentTenant)
    {
        _currentTenant = currentTenant;
    }

    [HttpGet("_ping")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Ping(string workspaceSlug)
    {
        return Ok(new
        {
            status = "ok",
            workspaceSlug = _currentTenant.WorkspaceSlug,
            tenantId = _currentTenant.TenantId,
            timestamp = DateTime.UtcNow
        });
    }
}
