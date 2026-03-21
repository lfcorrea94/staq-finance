using Microsoft.Extensions.DependencyInjection;
using StaqFinance.Modules.Tenancy.Application.Interfaces;
using StaqFinance.Modules.Tenancy.Domain.Interfaces;
using StaqFinance.Modules.Tenancy.Infrastructure.Repositories;
using StaqFinance.Modules.Tenancy.Infrastructure.Services;

namespace StaqFinance.Modules.Tenancy.Infrastructure.Extensions;

public static class TenancyServiceExtensions
{
    public static IServiceCollection AddTenancyModule(this IServiceCollection services)
    {
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddScoped<ITenantRepository, TenantRepository>();

        return services;
    }
}
