namespace Modules.Catalog.Application.Products.Get;

public record PagedResult<T>(
    IEnumerable<T> Items, 
    int TotalCount, 
    int PageNumber, 
    int PageSize);