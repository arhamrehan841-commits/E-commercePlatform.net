using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Infrastructure.Data;

namespace Modules.Catalog.Application.Products.Get.GetById;

internal sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductResponse?>
{
    private readonly CatalogDbContext _dbContext;

    public GetProductByIdQueryHandler(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductResponse?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        // Notice AsNoTracking() - This is crucial for Read-Only performance in EF Core
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            return null;
        }

        // Map the Domain Entity back to a simple DTO for the API layer
        return new ProductResponse(
            product.Id, 
            product.Name, 
            product.Description, 
            product.Price.Amount, 
            product.Price.Currency);
    }
}