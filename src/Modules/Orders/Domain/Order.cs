using SharedKernel.ValueObjects;

namespace Modules.Orders.Domain;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Status { get; private set; } = "Pending";
    private String? LogMessage;
    
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Create(Guid customerId) // <-- REMOVED reservation list
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer ID cannot be empty");

        return new Order 
        { 
            Id = Guid.NewGuid(), 
            CustomerId = customerId
        };
    }

    public void AddItem(Guid productId, Guid reservationId, string productName, Money unitPrice, int quantity) // <-- ADDED reservationId
    {
        var item = new OrderItem(Id, productId, reservationId, productName, unitPrice, quantity);
        _items.Add(item);
    }

    public Money CalculateTotal()
    {
        if (!_items.Any()) return Money.Zero();
        
        var totalAmount = _items.Sum(i => i.UnitPrice.Amount * i.Quantity);
        var currency = _items.First().UnitPrice.Currency;
        
        return new Money(totalAmount, currency);
    }

    public void MarkAsFailed(string reason)
    {
        Status = "Failed";
        LogMessage = $"Order {Id} marked as Failed. Reason: {reason}";
    }
}