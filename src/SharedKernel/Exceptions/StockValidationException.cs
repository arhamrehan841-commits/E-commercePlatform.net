namespace SharedKernel.Exceptions;

public class StockValidationException : Exception
{
    public IReadOnlyCollection<StockRejection> Rejections { get; }

    public StockValidationException(List<StockRejection> rejections) 
        : base("One or more items in the order are out of stock.")
    {
        Rejections = rejections.AsReadOnly();
    }
}