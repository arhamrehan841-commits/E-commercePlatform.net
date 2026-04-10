using Bogus;
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Domain.Products;
using SharedKernel.ValueObjects;

namespace Modules.Catalog.Infrastructure.Data;

public static class CatalogDataSeeder
{
    public static async Task SeedAsync(CatalogDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

        var moneyFaker = new Faker<Money>()
            .CustomInstantiator(f => new Money(
                f.Random.Decimal(10m, 1000m), 
                "USD"));

        var productFaker = new Faker<Product>()
            // This is the fix: Tell Bogus HOW to build a Product
            .CustomInstantiator(f => Product.Create(
                f.Commerce.ProductName(),
                f.Commerce.ProductDescription(),
                moneyFaker.Generate() // Generate the Money object first
            ));
            // You can remove the .RuleFor lines now because the constructor handles it all!

        var products = productFaker.Generate(50);

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}