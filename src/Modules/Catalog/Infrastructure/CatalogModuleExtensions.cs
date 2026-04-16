using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Database;
using Modules.Catalog.Infrastructure.Data;
using Modules.Catalog.Application.Products.Create; // Add this

namespace Modules.Catalog.Infrastructure;

public static class CatalogModuleExtensions
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, string connectionString)
    {
        // 1. Database Setup
        services.AddDbContext<CatalogDbContext>(opt => opt.UseSqlServer(connectionString));
        services.AddScoped<IModuleDatabase, CatalogModuleDatabase>();
        
        // 2. MediatR Setup (Moved from Host!)
        services.AddMediatR(config => 
        {
            config.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
        });

        return services;
    }
}