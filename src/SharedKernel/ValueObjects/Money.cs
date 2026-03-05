namespace SharedKernel.ValueObjects;

// In C# 9+, 'record' is the perfect construct for Value Objects 
// because they have built-in structural equality.
public record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency = "USD") => new(0, currency);
    
    // We can add business logic directly to the value object
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
            
        return new Money(Amount + other.Amount, Currency);
    }
}