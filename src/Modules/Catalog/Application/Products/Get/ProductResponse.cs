namespace Modules.Catalog.Application.Products.Get;

// Now it lives at the root of "Products" and can be shared!
public record ProductResponse(Guid Id, string Name, string Description, decimal PriceAmount, string Currency);