using MediatR;
using Modules.Catalog.Domain.Products;
using Modules.Catalog.Infrastructure.Data;
using SharedKernel.ValueObjects;

namespace Modules.Catalog.Application.Products.Create;

internal sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly CatalogDbContext _dbContext;

    public CreateProductCommandHandler(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Reconstruct the Value Object
        var price = new Money(request.PriceAmount, request.Currency);

        // 2. Instantiate the Domain Entity (Guard clauses will automatically run here!)
        var product = Product.Create(request.Name, request.Description, price);

        // 3. Add to EF Core Tracking
        _dbContext.Products.Add(product);

        // 4. Commit to the SQL Server Database
        await _dbContext.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}