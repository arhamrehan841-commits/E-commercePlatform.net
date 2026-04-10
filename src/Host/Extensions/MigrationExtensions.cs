using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Infrastructure.Data;
using Modules.Orders.Infrastructure.Data;

namespace Host.Extensions;

public static class MigrationExtensions
{
    // Changed to async Task to support database seeding
    public static async Task ApplyMigrationsAsync(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        var catalogContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var ordersContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        // 1. Build the tables
        await catalogContext.Database.MigrateAsync();
        await ordersContext.Database.MigrateAsync();

        // 2. Populate the tables
        await CatalogDataSeeder.SeedAsync(catalogContext);
    }
}