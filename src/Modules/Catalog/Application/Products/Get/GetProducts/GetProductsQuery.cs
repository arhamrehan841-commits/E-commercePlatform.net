using MediatR;

namespace Modules.Catalog.Application.Products.Get.GetProducts;

public record GetProductsQuery(
    int PageNumber = 1, 
    int PageSize = 20,
    string? SearchTerm = null,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? SortBy = null,
    bool SortDescending = false
) : IRequest<PagedResult<ProductResponse>>;