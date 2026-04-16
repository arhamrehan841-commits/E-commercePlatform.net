using Microsoft.EntityFrameworkCore;
using BuildingBlocks.Database; // The new interface we made

namespace Modules.Catalog.Infrastructure.Data;

internal sealed class CatalogModuleDatabase(CatalogDbContext context) : IModuleDatabase
{
    public async Task MigrateAsync() => await context.Database.MigrateAsync();

    public async Task SeedAsync() => await CatalogDataSeeder.SeedAsync(context);
}