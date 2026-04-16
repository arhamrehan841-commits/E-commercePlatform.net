using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Database;
using Modules.Orders.Infrastructure.Data;

namespace Modules.Orders.Infrastructure;

public static class OrdersModuleExtensions
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services, string connectionString)
    {
        // 1. Register the DbContext
        services.AddDbContext<OrdersDbContext>(opt => opt.UseSqlServer(connectionString));
        
        // 2. Register the Module Database
        services.AddScoped<IModuleDatabase, OrdersModuleDatabase>();
        
        return services;
    }
}