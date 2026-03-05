using SharedKernel.ValueObjects;

namespace Modules.Orders.Domain;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Status { get; private set; } = "Pending";
    
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Create(Guid customerId)
    {
        return new Order { Id = Guid.NewGuid(), CustomerId = customerId };
    }

    public void AddItem(Guid productId, string productName, Money unitPrice, int quantity)
    {
        var item = new OrderItem(Id, productId, productName, unitPrice, quantity);
        _items.Add(item);
    }

    public Money CalculateTotal()
    {
        if (!_items.Any()) return Money.Zero();
        
        var totalAmount = _items.Sum(i => i.UnitPrice.Amount * i.Quantity);
        var currency = _items.First().UnitPrice.Currency; // Assuming all items share the same currency
        
        return new Money(totalAmount, currency);
    }
}