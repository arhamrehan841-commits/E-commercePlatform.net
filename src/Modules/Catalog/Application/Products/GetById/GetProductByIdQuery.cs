using MediatR;

namespace Modules.Catalog.Application.Products.GetById;

// We request a product by ID, and return a simple DTO (Data Transfer Object)
public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductResponse>;

public record ProductResponse(Guid Id, string Name, string Description, decimal PriceAmount, string Currency);