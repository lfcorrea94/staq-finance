using Microsoft.Extensions.DependencyInjection;
using StaqFinance.Modules.Accounts.Application.Commands;
using StaqFinance.Modules.Accounts.Application.Queries;
using StaqFinance.Modules.Accounts.Infrastructure.Handlers;

namespace StaqFinance.Modules.Accounts.Infrastructure.Extensions;

public static class AccountsServiceExtensions
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services)
    {
        services.AddScoped<ICreateAccountCommandHandler, CreateAccountCommandHandler>();
        services.AddScoped<IListAccountsQueryHandler, ListAccountsQueryHandler>();

        return services;
    }
}
