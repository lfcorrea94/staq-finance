using Microsoft.Extensions.DependencyInjection;
using StaqFinance.Modules.Categories.Application.Commands;
using StaqFinance.Modules.Categories.Application.Queries;
using StaqFinance.Modules.Categories.Infrastructure.Handlers;

namespace StaqFinance.Modules.Categories.Infrastructure.Extensions;

public static class CategoriesServiceExtensions
{
    public static IServiceCollection AddCategoriesModule(this IServiceCollection services)
    {
        services.AddScoped<ICreateCategoryCommandHandler, CreateCategoryCommandHandler>();
        services.AddScoped<IListCategoriesQueryHandler, ListCategoriesQueryHandler>();

        return services;
    }
}
