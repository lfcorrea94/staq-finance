using Microsoft.Extensions.DependencyInjection;
using StaqFinance.Modules.RecurringTransactions.Application.Commands;
using StaqFinance.Modules.RecurringTransactions.Application.Queries;
using StaqFinance.Modules.RecurringTransactions.Infrastructure.Handlers;

namespace StaqFinance.Modules.RecurringTransactions.Infrastructure.Extensions;

public static class RecurringTransactionsServiceExtensions
{
    public static IServiceCollection AddRecurringTransactionsModule(this IServiceCollection services)
    {
        services.AddScoped<ICreateRecurringTransactionCommandHandler, CreateRecurringTransactionCommandHandler>();
        services.AddScoped<IRunRecurringTransactionsCommandHandler, RunRecurringTransactionsCommandHandler>();
        services.AddScoped<IListRecurringTransactionsQueryHandler, ListRecurringTransactionsQueryHandler>();

        return services;
    }
}
