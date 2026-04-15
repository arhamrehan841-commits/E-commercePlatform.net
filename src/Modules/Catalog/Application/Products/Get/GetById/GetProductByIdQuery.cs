using MediatR;

namespace Modules.Catalog.Application.Products.Get.GetById;

// We request a product by ID, and return a simple DTO (Data Transfer Object)
public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductResponse?>;
