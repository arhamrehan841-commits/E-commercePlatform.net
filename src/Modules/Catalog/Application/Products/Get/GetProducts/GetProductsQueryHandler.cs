using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Infrastructure.Data;

namespace Modules.Catalog.Application.Products.Get.GetProducts;

internal sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductResponse>>
{
    private readonly CatalogDbContext _dbContext;

    public GetProductsQueryHandler(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ProductResponse>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Products.AsNoTracking().AsQueryable();

        // 1. Apply Filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(p => p.Name.Contains(request.SearchTerm));

        // (Assuming you add Category to your Product entity later, commented out for now so it compiles)
        // if (!string.IsNullOrWhiteSpace(request.Category))
        //     query = query.Where(p => p.Category == request.Category); 

        if (request.MinPrice.HasValue)
            query = query.Where(p => p.Price.Amount >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            query = query.Where(p => p.Price.Amount <= request.MaxPrice.Value);

        // 2. Apply Sorting
        query = request.SortBy?.ToLower() switch
        {
            "price" => request.SortDescending ? query.OrderByDescending(p => p.Price.Amount) : query.OrderBy(p => p.Price.Amount),
            "name" => request.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            _ => query.OrderBy(p => p.Id) 
        };

        // 3. Count & Paginate
        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Price.Amount, p.Price.Currency))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductResponse>(products, totalCount, request.PageNumber, request.PageSize);
    }
}