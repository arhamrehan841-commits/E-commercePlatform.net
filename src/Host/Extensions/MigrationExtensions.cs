using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Infrastructure.Data;
using Modules.Orders.Infrastructure.Data;

namespace Host.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        var catalogContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var ordersContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        // Applies pending .cs migrations to the physical database
        catalogContext.Database.Migrate();
        ordersContext.Database.Migrate();
    }
}