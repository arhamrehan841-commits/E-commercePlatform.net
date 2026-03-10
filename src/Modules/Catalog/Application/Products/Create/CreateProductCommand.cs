using MediatR;

namespace Modules.Catalog.Application.Products.Create;

// The Request (Data payload)
// We return a Guid (the ID of the newly created product)
public record CreateProductCommand(string Name, string Description, decimal PriceAmount, string Currency) : IRequest<Guid>;