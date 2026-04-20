using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Database;
using SharedKernel.Contracts; // Add this for IStockReservationContract
using Modules.Catalog.Infrastructure.Contracts;
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
        services.AddScoped<IStockReservationContract, CatalogReservationService>();
        
        // 2. MediatR Setup (Moved from Host!)
        services.AddMediatR(config => 
        {
            config.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
        });

        return services;
    }
}