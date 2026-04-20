namespace Modules.Catalog.Domain.StockItems;

public class StockItem
{
    public Guid Id { get; set; }
    public int AvailableQty { get; set; }
    public int ReservedQty { get; set; }
}