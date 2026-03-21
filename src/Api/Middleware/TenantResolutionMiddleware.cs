using StaqFinance.Modules.Tenancy.Application.Interfaces;
using StaqFinance.Modules.Tenancy.Domain.Interfaces;

namespace StaqFinance.Api.Middleware;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant, ITenantRepository tenantRepository)
    {
        var workspaceSlug = context.GetRouteValue("workspaceSlug")?.ToString();

        if (!string.IsNullOrEmpty(workspaceSlug) && context.User.Identity?.IsAuthenticated == true)
        {
            var tenant = await tenantRepository.GetBySlugAsync(workspaceSlug, context.RequestAborted);

            if (tenant is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                    title = "Workspace not found.",
                    status = 404,
                    detail = $"Workspace '{workspaceSlug}' does not exist."
                });
                return;
            }

            currentTenant.Set(tenant.Id, tenant.Slug);
        }

        await _next(context);
    }
}
