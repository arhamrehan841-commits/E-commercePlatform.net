namespace SharedKernel.Contracts;

// This allows Orders to command Catalog synchronously without a direct project reference
public interface IStockReservationContract
{
    Task<Guid> ReserveStockAsync(Guid itemId, int quantity, CancellationToken cancellationToken = default);
    Task ConfirmReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);
}