using Bogus;
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Domain; 

namespace Modules.Catalog.Infrastructure.Data;

public static class CatalogDataSeeder
{
    public static async Task SeedAsync(CatalogDbContext context)
    {
        // Safety check: Only seed if the database is completely empty
        if (await context.Products.AnyAsync())
        {
            return; 
        }

        // Configure realistic fake data generation
        var faker = new Faker<Product>()
            .RuleFor(p => p.Id, f => Guid.NewGuid())
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.Price.Amount, f => f.Random.Decimal(10m, 500m))
            .RuleFor(p => p.Price.Currency, f => "USD");

        var products = faker.Generate(50);

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}