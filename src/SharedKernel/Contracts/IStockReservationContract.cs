using SharedKernel.Exceptions;
namespace SharedKernel.Contracts;

public record BulkReservationRequest(Guid ItemId, int Quantity);

// NEW: Paired result to map products to reservations
public record ReservationResult(Guid ItemId, Guid ReservationId);

public record BulkReservationResponse(
    bool AllReserved, 
    List<ReservationResult> Reservations, // <-- UPDATED
    List<StockRejection> Rejections);

public interface IStockReservationContract
{
    Task<BulkReservationResponse> ReserveStockStrictAsync(
        IEnumerable<BulkReservationRequest> requests, 
        CancellationToken ct = default);

    Task ConfirmReservationsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task ReleaseReservationsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}