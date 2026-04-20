using Microsoft.EntityFrameworkCore;
using SharedKernel.Database;

namespace Modules.Orders.Infrastructure.Data;

internal sealed class OrdersModuleDatabase(OrdersDbContext context) : IModuleDatabase
{
    public async Task MigrateAsync() => await context.Database.MigrateAsync();

    // Since you don't have an Orders seeder yet, just return completed
    public async Task SeedAsync() => await Task.CompletedTask; 
}