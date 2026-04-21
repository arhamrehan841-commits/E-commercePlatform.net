namespace SharedKernel.Exceptions;

public record StockRejection(Guid ProductId, string ProductName, int RequestedQuantity, int AvailableQuantity);