using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Database;
using Modules.Catalog.Infrastructure.Data;

namespace Modules.Catalog.Infrastructure;

public static class CatalogModuleExtensions
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, string connectionString)
    {
        // 1. Register the DbContext
        services.AddDbContext<CatalogDbContext>(opt => opt.UseSqlServer(connectionString));
        
        // 2. Register the Module Database for the generic migration runner
        services.AddScoped<IModuleDatabase, CatalogModuleDatabase>();
        
        return services;
    }
}