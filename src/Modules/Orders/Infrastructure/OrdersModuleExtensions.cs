using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Database;
using Modules.Orders.Infrastructure.Data;

namespace Modules.Orders.Infrastructure;

public static class OrdersModuleExtensions
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OrdersDbContext>(opt => opt.UseSqlServer(connectionString));
        services.AddScoped<IModuleDatabase, OrdersModuleDatabase>();
        
        // Tells MediatR to scan this specific module assembly
        services.AddMediatR(config => 
        {
            config.RegisterServicesFromAssembly(typeof(OrdersModuleExtensions).Assembly);
        });

        return services;
    }
}