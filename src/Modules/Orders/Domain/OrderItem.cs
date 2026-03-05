using SharedKernel.ValueObjects;

namespace Modules.Orders.Domain;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public Money UnitPrice { get; private set; } = Money.Zero();
    public int Quantity { get; private set; }

    // Parameterless constructor required by Entity Framework Core
    private OrderItem() { }

    internal OrderItem(Guid orderId, Guid productId, string productName, Money unitPrice, int quantity)
    {
        // Guard Clauses
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID cannot be empty");

        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty");

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty");

        // We use < 0 instead of <= 0 here just in case you ever want to support "Free/Promotional" items
        if (unitPrice.Amount < 0)
            throw new ArgumentException("Unit price cannot be negative");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be at least 1");

        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}