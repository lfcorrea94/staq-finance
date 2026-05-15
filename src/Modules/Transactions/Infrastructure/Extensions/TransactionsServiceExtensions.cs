using Microsoft.Extensions.DependencyInjection;
using StaqFinance.Modules.Transactions.Application.Commands;
using StaqFinance.Modules.Transactions.Application.Queries;
using StaqFinance.Modules.Transactions.Infrastructure.Handlers;

namespace StaqFinance.Modules.Transactions.Infrastructure.Extensions;

public static class TransactionsServiceExtensions
{
    public static IServiceCollection AddTransactionsModule(this IServiceCollection services)
    {
        services.AddScoped<ICreateTransactionCommandHandler, CreateTransactionCommandHandler>();
        services.AddScoped<IListTransactionsQueryHandler, ListTransactionsQueryHandler>();

        return services;
    }
}
