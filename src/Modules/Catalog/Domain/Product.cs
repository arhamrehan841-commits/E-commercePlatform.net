using SharedKernel.ValueObjects;

namespace Modules.Catalog.Domain.Products;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = Money.Zero();

    // Parameterless constructor required by Entity Framework Core
    private Product() { }

    // Factory method for creating valid products
    public static Product Create(string name, string description, Money price)
    {
        // Guard Clauses to protect the domain invariants
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Product description cannot be empty");

        // Price validation is handled by the Money value object, so we can rely on it to throw if the price is invalid

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price
        };
    }

    public void UpdatePrice(Money newPrice)
    {
        // Price validation is handled by the Money value object, so we can rely on it to throw if the price is invalid
        Price = newPrice;
    }
}