using SharedKernel.ValueObjects;

namespace Modules.Orders.Domain;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Status { get; private set; } = "Pending";
    
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Parameterless constructor required by Entity Framework Core
    private Order() { }

    public static Order Create(Guid customerId)
    {
        // Guard Clause
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID cannot be empty");

        return new Order 
        { 
            Id = Guid.NewGuid(), 
            CustomerId = customerId 
        };
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
        var currency = _items.First().UnitPrice.Currency;
        
        return new Money(totalAmount, currency);
    }
}